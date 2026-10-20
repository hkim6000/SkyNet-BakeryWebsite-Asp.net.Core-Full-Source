using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminLogin : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Staff sign in | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            await Store.EnsureFirstManager();
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_first}", Local.Request() ? FirstBox(Store.InitialPassword()) : string.Empty);
        }

        public async Task<ApiResponse> SignIn()
        {
            ApiResponse response = new ApiResponse();
            string login = Ui.Clean(GetDataValue("login"), 40).ToLowerInvariant();
            string pass = GetDataValue("pass") ?? string.Empty;
            if (login == string.Empty || pass == string.Empty)
            {
                return Fail(response, "Enter your user name and password.");
            }
            if (Auth.Paused(login))
            {
                await Store.Log(login, "sign in", "too many tries", false);
                return Fail(response, "Too many tries. Please wait a few minutes and try again.");
            }
            List<Staff> staff = await Store.LoadStaff();
            Staff? u = staff.FirstOrDefault(x => x.Login.Equals(login, StringComparison.OrdinalIgnoreCase) && x.Active);
            if (u == null || !Access.Verify(pass, u.Hash))
            {
                Auth.Failed(login);
                await Store.Log(login, "sign in", "wrong password", false);
                return Fail(response, "The user name or password is not right.");
            }
            Auth.Cleared(login);
            if (u.MustChange)
            {
                response.SetElementContents("lg-e-form", string.Empty);
                response.ExecuteScript("AdminLoginJs.choose();");
                return response;
            }
            string token = await Auth.Open(u.Id);
            await Store.Log(u.Id, "sign in", u.Name, true);
            response.ExecuteScript("AdminLoginJs.signedIn('" + token + "');");
            return response;
        }

        public async Task<ApiResponse> SetPassword()
        {
            ApiResponse response = new ApiResponse();
            string login = Ui.Clean(GetDataValue("login"), 40).ToLowerInvariant();
            string pass = GetDataValue("pass") ?? string.Empty;
            string new1 = GetDataValue("new1") ?? string.Empty;
            string new2 = GetDataValue("new2") ?? string.Empty;
            if (Auth.Paused(login))
            {
                return Fail(response, "Too many tries. Please wait a few minutes and try again.");
            }
            string token;
            string name;
            string userId;
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? u = staff.FirstOrDefault(x => x.Login.Equals(login, StringComparison.OrdinalIgnoreCase) && x.Active && x.MustChange);
                if (u == null || !Access.Verify(pass, u.Hash))
                {
                    Auth.Failed(login);
                    await Store.Log(login, "choose password", "wrong password", false);
                    return Fail(response, "The user name or current password is not right.");
                }
                string problem = Access.PasswordProblem(new1, pass);
                if (problem == string.Empty && new1 != new2)
                {
                    problem = "The two new passwords are not the same.";
                }
                if (problem != string.Empty)
                {
                    return Fail(response, problem);
                }
                u.Hash = Access.HashPassword(new1);
                u.MustChange = false;
                await Store.SaveStaff(staff);
                name = u.Name;
                userId = u.Id;
            }
            if (File.Exists(Store.InitialFile))
            {
                File.Delete(Store.InitialFile);
            }
            Auth.Cleared(login);
            await Auth.CloseAll(userId, string.Empty);
            token = await Auth.Open(userId);
            await Store.Log(userId, "choose password", name, true);
            response.ExecuteScript("AdminLoginJs.signedIn('" + token + "');");
            return response;
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            string tok = (GetDataValue("tok") ?? string.Empty).Trim();
            Ctx? c = await Auth.Check(tok);
            if (c != null)
            {
                await Auth.Close(tok);
                await Store.Log(c.U.Id, "sign out", c.U.Name, true);
            }
            response.ExecuteScript("var bp = location.pathname; bp = bp.substring(0, bp.lastIndexOf('/') + 1); document.cookie = 'bk_tok=; path=' + bp + '; max-age=0'; location.href = 'AdminLogin';");
            return response;
        }

        private static ApiResponse Fail(ApiResponse response, string message)
        {
            response.SetElementContents("lg-e-form", HtmlEncode(message));
            response.ExecuteScript("AdminLoginJs.failed();");
            return response;
        }

        private static string FirstBox(string password)
        {
            if (password == string.Empty)
            {
                return string.Empty;
            }
            return "<div class=\"lg-first\"><b>First sign-in</b><dl><dt>User name</dt><dd><code>admin</code></dd><dt>Password</dt><dd><code>" + HtmlEncode(password) + "</code></dd></dl><small>Shown only on this computer, until you choose your own password. Also in appConfig/initial-admin.txt.</small></div>";
        }
    }
}
