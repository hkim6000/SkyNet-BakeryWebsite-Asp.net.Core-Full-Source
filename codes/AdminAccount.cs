using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminAccount : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("My account | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminAccountJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("aa-menu", await Auth.Menu("aa", "AdminAccount", c, site));
            response.SetElementContents("aa-who", Auth.Who("aa", c));
            response.SetElementContents("aa-main", MainHtml(c));
            return response;
        }

        public async Task<ApiResponse> Change()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminAccountJs");
            }
            string current = GetDataValue("current") ?? string.Empty;
            string new1 = GetDataValue("new1") ?? string.Empty;
            string new2 = GetDataValue("new2") ?? string.Empty;
            string eCur = string.Empty;
            string eNew = string.Empty;
            string eNew2 = string.Empty;
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? u = staff.FirstOrDefault(x => x.Id == c.U.Id);
                if (u == null || !Access.Verify(current, u.Hash))
                {
                    eCur = "That is not your current password.";
                }
                else
                {
                    eNew = Access.PasswordProblem(new1, current);
                    eNew2 = eNew == string.Empty && new1 != new2 ? "The two new passwords are not the same." : string.Empty;
                }
                if (eCur + eNew + eNew2 == string.Empty && u != null)
                {
                    u.Hash = Access.HashPassword(new1);
                    await Store.SaveStaff(staff);
                }
            }
            response.SetElementContents("aa-e-cur", eCur);
            response.SetElementContents("aa-e-new1", eNew);
            response.SetElementContents("aa-e-new2", eNew2);
            if (eCur + eNew + eNew2 != string.Empty)
            {
                await Store.Log(c.U.Id, "change password", c.U.Login, eCur == string.Empty);
                return response;
            }
            await Auth.CloseAll(c.U.Id, c.Token);
            await Store.Log(c.U.Id, "change password", c.U.Login, true);
            response.ExecuteScript("AdminAccountJs.changed();");
            return response;
        }

        private static string MainHtml(Ctx c)
        {
            Staff u = c.U;
            StringBuilder sb = new StringBuilder("<div class=\"aa-top\"><div><h1>My account</h1><p>Your details and your password.</p></div></div><div class=\"aa-cols\">");
            sb.Append("<div class=\"aa-card\"><h2>You</h2><dl class=\"aa-facts\"><dt>Name</dt><dd>" + Ui.H(u.Name) + "</dd><dt>User name</dt><dd>" + Ui.H(u.Login) + "</dd><dt>Role</dt><dd>" + Ui.H(u.Admin ? "Manager" : u.Title == string.Empty ? "Staff" : u.Title) + "</dd><dt>You can</dt><dd>");
            List<string> parts = new List<string>();
            foreach (Area a in Access.Areas)
            {
                string level = Access.Level(u, a.Key);
                if (level != string.Empty)
                {
                    parts.Add(a.Name + " (" + (level.Contains('X') ? "full" : level.Contains('E') ? "view and change" : "view") + ")");
                }
            }
            sb.Append(Ui.H(parts.Count == 0 ? "Nothing yet" : string.Join(", ", parts)) + "</dd></dl></div>");
            sb.Append("<form class=\"aa-card\" onsubmit=\"return AdminAccountJs.change()\" novalidate><h2>Change password</h2><div class=\"aa-stack\">");
            sb.Append("<label class=\"aa-field\"><span>Current password</span><input type=\"password\" id=\"aa-cur\" maxlength=\"100\" autocomplete=\"current-password\"><em class=\"aa-err\" id=\"aa-e-cur\"></em></label>");
            sb.Append("<label class=\"aa-field\"><span>New password (8 characters or more)</span><input type=\"password\" id=\"aa-new1\" maxlength=\"100\" autocomplete=\"new-password\"><em class=\"aa-err\" id=\"aa-e-new1\"></em></label>");
            sb.Append("<label class=\"aa-field\"><span>New password again</span><input type=\"password\" id=\"aa-new2\" maxlength=\"100\" autocomplete=\"new-password\"><em class=\"aa-err\" id=\"aa-e-new2\"></em></label>");
            sb.Append("<p class=\"aa-note\">Changing it signs you out everywhere else.</p><div class=\"aa-formacts\"><button type=\"submit\" class=\"aa-btn\">Change password</button></div></div></form></div>");
            return sb.ToString();
        }
    }
}
