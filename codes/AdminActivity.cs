using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminActivity : WebPage
    {
        private const string AreaKey = "activity";
        private const int Size = 15;

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Activity | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminActivityJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("ay-menu", await Auth.Menu("ay", "AdminActivity", c, site));
            response.SetElementContents("ay-who", Auth.Who("ay", c));
            bool all = Access.Can(c.U, AreaKey, 'V');
            response.SetElementContents("ay-main", ShellHtml(all) + "<div id=\"ay-list\">" + await ListHtml(c, string.Empty, string.Empty, 1) + "</div>");
            return response;
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminActivityJs");
            }
            response.SetElementContents("ay-list", await ListHtml(c, Ui.Clean(GetDataValue("term"), 40), Ui.Pick(GetDataValue("result"), new[] { "ok", "refused" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            return response;
        }

        private static string ShellHtml(bool all)
        {
            return "<div class=\"ay-top\"><div><h1>" + (all ? "Activity log" : "My activity") + "</h1><p>" + (all ? "Every change and every refused request, newest first." : "What you have done in the staff area.") + "</p></div></div><div class=\"ay-tools\"><input type=\"search\" id=\"ay-term\" placeholder=\"Search the log\" maxlength=\"40\" aria-label=\"Search the log\"><select id=\"ay-result\" aria-label=\"Result\" onchange=\"AdminActivityJs.changed()\"><option value=\"\">Done and refused</option><option value=\"ok\">Done</option><option value=\"refused\">Refused</option></select></div>";
        }

        private static async Task<string> ListHtml(Ctx c, string term, string result, int page)
        {
            bool all = Access.Can(c.U, AreaKey, 'V');
            List<Staff> staff = await Store.LoadStaff();
            IEnumerable<ActivityEntry> q = (await Store.LoadLog()).AsEnumerable().Reverse();
            if (!all)
            {
                q = q.Where(e => e.UserId == c.U.Id);
            }
            if (result == "ok")
            {
                q = q.Where(e => e.Ok);
            }
            else if (result == "refused")
            {
                q = q.Where(e => !e.Ok);
            }
            string Who(string id)
            {
                return staff.FirstOrDefault(s => s.Id == id)?.Name ?? id;
            }
            List<ActivityEntry> rows = (term == string.Empty ? q : q.Where(e => e.Action.Contains(term, StringComparison.OrdinalIgnoreCase) || e.Target.Contains(term, StringComparison.OrdinalIgnoreCase) || Who(e.UserId).Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"ay-tbl-wrap\"><div class=\"ay-empty\">Nothing in the log matches.</div></div>";
            }
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            StringBuilder sb = new StringBuilder("<div class=\"ay-tbl-wrap\"><table class=\"ay-tbl\"><thead><tr><th>When</th><th>Who</th><th>Action</th><th>Details</th><th>Result</th></tr></thead><tbody>");
            foreach (ActivityEntry e in rows.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<tr><td>" + Ui.H(Ui.Stamp(e.When)) + "</td><td>" + Ui.H(Who(e.UserId)) + "</td><td>" + Ui.H(e.Action) + "</td><td>" + Ui.H(e.Target) + "</td><td><span class=\"ay-pill " + (e.Ok ? "ay-s-ok" : "ay-s-refused") + "\">" + (e.Ok ? "Done" : "Refused") + "</span></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"ay-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "entry", "entries") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"ay-btn ay-btn-ghost ay-btn-sm\"" + (page <= 1 ? " disabled" : " onclick=\"AdminActivityJs.go(" + (page - 1) + ")\"") + ">Previous</button>");
            sb.Append("<button type=\"button\" class=\"ay-btn ay-btn-ghost ay-btn-sm\"" + (page >= pages ? " disabled" : " onclick=\"AdminActivityJs.go(" + (page + 1) + ")\"") + ">Next</button></div></div>");
            return sb.ToString();
        }
    }
}
