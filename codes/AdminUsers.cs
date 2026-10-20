using System.Text;
using System.Text.RegularExpressions;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminUsers : WebPage
    {
        private const string AreaKey = "users";

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Users | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminUsersJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("au-menu", await Auth.Menu("au", "AdminUsers", c, site));
            response.SetElementContents("au-who", Auth.Who("au", c));
            response.SetElementContents("au-main", Access.Can(c.U, AreaKey, 'V') ? MainHtml(c, await Store.LoadStaff()) : Auth.NoAccess("au", "Users"));
            return response;
        }

        public async Task<ApiResponse> Edit()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminUsersJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 40);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminUsersJs", "edit user", id, "Only managers can change staff.");
            }
            Staff? u = (await Store.LoadStaff()).FirstOrDefault(x => x.Id == id);
            if (id != string.Empty && u == null)
            {
                response.ExecuteScript("AdminUsersJs.toast('That person was not found.');");
                return response;
            }
            response.SetElementContents("au-mbody", FormHtml(c, u));
            response.ExecuteScript("AdminUsersJs.openModal('" + (u == null ? "New staff member" : "Edit " + Ui.Js(u.Name)) + "');");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminUsersJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 40);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminUsersJs", "save user", id, "Only managers can change staff.");
            }
            string message;
            bool closeSessions = false;
            string savedId = string.Empty;
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? u = staff.FirstOrDefault(x => x.Id == id);
                string login = Ui.Clean(GetDataValue("login"), 30).ToLowerInvariant();
                string name = Ui.Clean(GetDataValue("name"), 60);
                string title = Ui.Clean(GetDataValue("title"), 40);
                string password = GetDataValue("password") ?? string.Empty;
                bool manager = GetDataValue("manager") == "1";
                bool active = GetDataValue("active") == "1";
                bool self = u != null && u.Id == c.U.Id;
                string eLogin = !Regex.IsMatch(login, "^[a-z0-9._-]{3,30}$") ? "Use 3 to 30 letters, numbers, dots, dashes or underscores." : staff.Any(x => x.Login == login && x.Id != id) ? "That user name is already taken." : string.Empty;
                string eName = name.Length < 2 ? "Enter a name." : string.Empty;
                string ePass = string.Empty;
                if (u == null && password == string.Empty)
                {
                    ePass = "Choose a temporary password.";
                }
                else if (password != string.Empty)
                {
                    ePass = Access.PasswordProblem(password, string.Empty);
                }
                string eForm = string.Empty;
                if (self && (!manager || !active))
                {
                    eForm = "You cannot remove your own manager right or switch yourself off.";
                }
                else if (u != null && u.Admin && u.Active && (!manager || !active) && !staff.Any(x => x.Id != u.Id && x.Admin && x.Active && !x.MustChange))
                {
                    eForm = "At least one active manager must remain.";
                }
                response.SetElementContents("au-e-login", eLogin);
                response.SetElementContents("au-e-name", eName);
                response.SetElementContents("au-e-pass", ePass);
                response.SetElementContents("au-e-form", eForm);
                if (eLogin + eName + ePass + eForm != string.Empty)
                {
                    return response;
                }
                bool isNew = u == null;
                if (u == null)
                {
                    u = new Staff { Id = "u-" + Guid.NewGuid().ToString("N").Substring(0, 8) };
                    staff.Add(u);
                }
                u.Login = login;
                u.Name = name;
                u.Title = title;
                u.Admin = manager;
                u.Active = active;
                u.Perms = ParsePerms(GetDataValue("perms"));
                if (password != string.Empty)
                {
                    u.Hash = Access.HashPassword(password);
                    u.MustChange = true;
                    closeSessions = true;
                }
                if (!active)
                {
                    closeSessions = true;
                }
                await Store.SaveStaff(staff);
                savedId = u.Id;
                message = name + (isNew ? " was added." : " was saved.");
                await Store.Log(c.U.Id, isNew ? "user add" : "user edit", u.Login, true);
                response.SetElementContents("au-main", MainHtml(c, staff));
            }
            if (closeSessions)
            {
                await Auth.CloseAll(savedId, string.Empty);
            }
            response.ExecuteScript("AdminUsersJs.saved('" + Ui.Js(message) + "');");
            return response;
        }

        public async Task<ApiResponse> Toggle()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminUsersJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 40);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminUsersJs", "switch user", id, "Only managers can change staff.");
            }
            string message;
            bool close = false;
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? u = staff.FirstOrDefault(x => x.Id == id);
                if (u == null)
                {
                    message = "That person was not found.";
                }
                else if (u.Id == c.U.Id)
                {
                    message = "You cannot switch yourself off.";
                }
                else if (u.Admin && u.Active && !staff.Any(x => x.Id != u.Id && x.Admin && x.Active && !x.MustChange))
                {
                    message = "At least one active manager must remain.";
                }
                else
                {
                    u.Active = !u.Active;
                    close = !u.Active;
                    await Store.SaveStaff(staff);
                    message = u.Name + (u.Active ? " is switched on." : " is switched off.");
                    await Store.Log(c.U.Id, u.Active ? "user on" : "user off", u.Login, true);
                }
                response.SetElementContents("au-main", MainHtml(c, staff));
            }
            if (close)
            {
                await Auth.CloseAll(id, string.Empty);
            }
            response.ExecuteScript("AdminUsersJs.saved('" + Ui.Js(message) + "');");
            return response;
        }

        private static Dictionary<string, string> ParsePerms(string? raw)
        {
            Dictionary<string, string> perms = new Dictionary<string, string>();
            foreach (string part in (raw ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] bits = part.Split(':');
                Area? area = bits.Length == 2 ? Access.Find(bits[0]) : null;
                if (area == null || area.AdminOnly)
                {
                    continue;
                }
                string letters = Access.Cleaned(bits[1], area);
                if (letters != string.Empty)
                {
                    perms[area.Key] = letters;
                }
            }
            return perms;
        }

        private static string Summary(Staff u)
        {
            if (u.Admin)
            {
                return "Manager: everything";
            }
            List<string> parts = new List<string>();
            foreach (Area a in Access.Areas.Where(x => !x.AdminOnly))
            {
                string level = Access.Level(u, a.Key);
                if (level != string.Empty)
                {
                    parts.Add(a.Name + (level.Contains('X') ? " (full)" : level.Contains('E') ? " (edit)" : " (view)"));
                }
            }
            return parts.Count == 0 ? "No areas yet" : string.Join(", ", parts);
        }

        private static string MainHtml(Ctx c, List<Staff> staff)
        {
            bool edit = Access.Can(c.U, AreaKey, 'E');
            StringBuilder sb = new StringBuilder("<div class=\"au-top\"><div><h1>Users</h1><p>Who can sign in to the staff area, and what each person may do.</p></div>");
            if (edit)
            {
                sb.Append("<button type=\"button\" class=\"au-btn\" onclick=\"AdminUsersJs.edit('')\">Add a person</button>");
            }
            sb.Append("</div><div class=\"au-tbl-wrap\"><table class=\"au-tbl\"><thead><tr><th>Person</th><th>Rights</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (Staff u in staff)
            {
                sb.Append("<tr><td><b>" + Ui.H(u.Name) + "</b><small>" + Ui.H(u.Login) + (u.Title == string.Empty ? string.Empty : " &middot; " + Ui.H(u.Title)) + "</small></td><td class=\"au-rights\">" + Ui.H(Summary(u)) + "</td>");
                sb.Append("<td><span class=\"au-pill " + (u.Active ? "au-s-ok" : "au-s-off") + "\">" + (u.Active ? (u.MustChange ? "Waiting for first sign-in" : "Active") : "Switched off") + "</span></td><td><div class=\"au-acts\">");
                if (edit)
                {
                    sb.Append("<button type=\"button\" class=\"au-btn au-btn-ghost au-btn-sm\" onclick=\"AdminUsersJs.edit('" + Ui.H(u.Id) + "')\">Edit</button>");
                    if (u.Id != c.U.Id)
                    {
                        sb.Append("<button type=\"button\" class=\"au-btn au-btn-ghost au-btn-sm\" onclick=\"AdminUsersJs.toggle('" + Ui.H(u.Id) + "', '" + Ui.H(Ui.Js(u.Name)) + "', " + (u.Active ? "true" : "false") + ")\">" + (u.Active ? "Switch off" : "Switch on") + "</button>");
                    }
                }
                sb.Append("</div></td></tr>");
            }
            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private static string FormHtml(Ctx c, Staff? u)
        {
            Staff v = u ?? new Staff { Active = true };
            bool self = u != null && u.Id == c.U.Id;
            StringBuilder sb = new StringBuilder("<form class=\"au-stack\" onsubmit=\"return AdminUsersJs.save()\" novalidate>");
            sb.Append("<input type=\"hidden\" id=\"au-f-id\" value=\"" + Ui.H(v.Id) + "\">");
            sb.Append("<div class=\"au-grid2\"><label class=\"au-field\"><span>User name</span><input type=\"text\" id=\"au-f-login\" maxlength=\"30\" autocomplete=\"off\" autocapitalize=\"none\" value=\"" + Ui.H(v.Login) + "\"><em class=\"au-err\" id=\"au-e-login\"></em></label>");
            sb.Append("<label class=\"au-field\"><span>Full name</span><input type=\"text\" id=\"au-f-name\" maxlength=\"60\" value=\"" + Ui.H(v.Name) + "\"><em class=\"au-err\" id=\"au-e-name\"></em></label></div>");
            sb.Append("<div class=\"au-grid2\"><label class=\"au-field\"><span>Job title (optional)</span><input type=\"text\" id=\"au-f-title\" maxlength=\"40\" value=\"" + Ui.H(v.Title) + "\"></label>");
            sb.Append("<label class=\"au-field\"><span>" + (u == null ? "Temporary password" : "New temporary password (leave empty to keep)") + "</span><input type=\"password\" id=\"au-f-pass\" maxlength=\"100\" autocomplete=\"new-password\"><em class=\"au-err\" id=\"au-e-pass\"></em></label></div>");
            sb.Append("<p class=\"au-note\">With a temporary password the person must choose their own at the next sign-in.</p>");
            sb.Append("<label class=\"au-check\"><input type=\"checkbox\" id=\"au-f-admin\"" + (v.Admin ? " checked" : string.Empty) + (self ? " disabled" : string.Empty) + " onchange=\"AdminUsersJs.adminToggle()\"> Manager: can do everything, including settings and users</label>");
            sb.Append("<label class=\"au-check\"><input type=\"checkbox\" id=\"au-f-active\"" + (v.Active ? " checked" : string.Empty) + (self ? " disabled" : string.Empty) + "> Can sign in</label>");
            sb.Append("<div class=\"au-boxed\" id=\"au-permbox\"" + (v.Admin ? " hidden" : string.Empty) + "><b>What this person can do</b><div class=\"au-permwrap\"><table class=\"au-grid-perm\"><thead><tr><th>Area</th><th>View</th><th>Change</th><th>Extra</th></tr></thead><tbody>");
            foreach (Area a in Access.Areas.Where(x => !x.AdminOnly))
            {
                string level = v.Admin ? string.Empty : Access.Level(v, a.Key);
                sb.Append("<tr data-area=\"" + a.Key + "\"><td>" + Ui.H(a.Name) + (a.Extra == string.Empty ? string.Empty : "<small>Extra: " + Ui.H(a.Extra) + "</small>") + "</td>");
                sb.Append("<td><input type=\"checkbox\" data-right=\"V\" aria-label=\"View " + Ui.H(a.Name) + "\"" + (level.Contains('V') ? " checked" : string.Empty) + " onchange=\"AdminUsersJs.tick(this)\"></td>");
                sb.Append("<td>" + (a.NoEdit ? "&ndash;" : "<input type=\"checkbox\" data-right=\"E\" aria-label=\"Change " + Ui.H(a.Name) + "\"" + (level.Contains('E') ? " checked" : string.Empty) + " onchange=\"AdminUsersJs.tick(this)\">") + "</td>");
                sb.Append("<td>" + (a.Extra == string.Empty || a.NoEdit ? "&ndash;" : "<input type=\"checkbox\" data-right=\"X\" aria-label=\"" + Ui.H(a.Extra) + "\"" + (level.Contains('X') ? " checked" : string.Empty) + " onchange=\"AdminUsersJs.tick(this)\">") + "</td></tr>");
            }
            sb.Append("</tbody></table></div><p class=\"au-note\">Settings and Users are for managers only. Without the Activity log right, a person sees only their own activity.</p></div>");
            sb.Append("<em class=\"au-err\" id=\"au-e-form\" role=\"alert\"></em>");
            sb.Append("<div class=\"au-formacts\"><button type=\"button\" class=\"au-btn au-btn-ghost\" onclick=\"AdminUsersJs.closeModal()\">Cancel</button><button type=\"submit\" class=\"au-btn\">Save person</button></div></form>");
            return sb.ToString();
        }
    }
}
