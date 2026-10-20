using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminMessages : WebPage
    {
        private const string AreaKey = "messages";
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Messages | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminMessagesJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("am-menu", await Auth.Menu("am", "AdminMessages", c, site));
            response.SetElementContents("am-who", Auth.Who("am", c));
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                response.SetElementContents("am-main", Auth.NoAccess("am", "Messages"));
                return response;
            }
            response.SetElementContents("am-main", ShellHtml() + "<div id=\"am-list\">" + ListHtml(await Store.LoadMessages(), string.Empty, string.Empty, 1) + "</div>");
            return response;
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminMessagesJs");
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Auth.Refuse(response, c, "AdminMessagesJs", "view messages", "list", "You do not have access to messages.");
            }
            response.SetElementContents("am-list", ListHtml(await Store.LoadMessages(), Ui.Clean(GetDataValue("term"), 40), Ui.Pick(GetDataValue("show"), new[] { "unread", "read" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            return response;
        }

        public async Task<ApiResponse> View()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminMessagesJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 20);
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Auth.Refuse(response, c, "AdminMessagesJs", "view message", id, "You do not have access to messages.");
            }
            Enquiry? m = (await Store.LoadMessages()).FirstOrDefault(x => x.Id == id);
            if (m == null)
            {
                response.ExecuteScript("AdminMessagesJs.toast('That message was not found.');");
                return response;
            }
            response.SetElementContents("am-mbody", DetailHtml(c, m));
            response.ExecuteScript("AdminMessagesJs.openModal('Message from " + Ui.Js(m.Name) + "');");
            return response;
        }

        public async Task<ApiResponse> Mark()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminMessagesJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 20);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminMessagesJs", "mark message", id, "You can read messages but not change them.");
            }
            bool read = GetDataValue("read") == "1";
            string message;
            using (await Store.Hold())
            {
                List<Enquiry> all = await Store.LoadMessages();
                Enquiry? m = all.FirstOrDefault(x => x.Id == id);
                if (m == null)
                {
                    message = "That message was not found.";
                }
                else
                {
                    m.Read = read;
                    await Store.SaveMessages(all);
                    message = "Marked as " + (read ? "read." : "unread.");
                    await Store.Log(c.U.Id, read ? "message read" : "message unread", m.Name, true);
                }
            }
            await Redraw(response, c);
            response.ExecuteScript("AdminMessagesJs.done('" + Ui.Js(message) + "');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminMessagesJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 20);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminMessagesJs", "delete message", id, "You can read messages but not change them.");
            }
            string message;
            using (await Store.Hold())
            {
                List<Enquiry> all = await Store.LoadMessages();
                Enquiry? m = all.FirstOrDefault(x => x.Id == id);
                if (m == null)
                {
                    message = "That message was not found.";
                }
                else
                {
                    all.Remove(m);
                    await Store.SaveMessages(all);
                    message = "Message deleted.";
                    await Store.Log(c.U.Id, "message delete", m.Name, true);
                }
            }
            await Redraw(response, c);
            response.ExecuteScript("AdminMessagesJs.done('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Redraw(ApiResponse response, Ctx c)
        {
            response.SetElementContents("am-list", ListHtml(await Store.LoadMessages(), Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fshow"), new[] { "unread", "read" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            response.SetElementContents("am-menu", await Auth.Menu("am", "AdminMessages", c, await Store.LoadSite()));
        }

        private static string ShellHtml()
        {
            return "<div class=\"am-top\"><div><h1>Messages</h1><p>What customers wrote through the contact form.</p></div></div><div class=\"am-tools\"><input type=\"search\" id=\"am-term\" placeholder=\"Search messages\" maxlength=\"40\" aria-label=\"Search messages\"><select id=\"am-show\" aria-label=\"Show\" onchange=\"AdminMessagesJs.changed()\"><option value=\"\">All messages</option><option value=\"unread\">Unread</option><option value=\"read\">Read</option></select></div>";
        }

        private static string TopicLabel(string topic)
        {
            return topic switch
            {
                "order" => "An order",
                "large" => "Large order",
                "feedback" => "Feedback",
                _ => "Other"
            };
        }

        private static string ListHtml(List<Enquiry> all, string term, string show, int page)
        {
            IEnumerable<Enquiry> q = all;
            if (show == "unread")
            {
                q = q.Where(m => !m.Read);
            }
            else if (show == "read")
            {
                q = q.Where(m => m.Read);
            }
            if (term != string.Empty)
            {
                q = q.Where(m => m.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || m.Email.Contains(term, StringComparison.OrdinalIgnoreCase) || m.Text.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<Enquiry> rows = q.OrderByDescending(m => m.When).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"am-tbl-wrap\"><div class=\"am-empty\">No messages match.</div></div>";
            }
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            StringBuilder sb = new StringBuilder("<div class=\"am-tbl-wrap\"><table class=\"am-tbl\"><thead><tr><th>From</th><th>Topic</th><th>Message</th><th>Received</th><th>Status</th></tr></thead><tbody>");
            foreach (Enquiry m in rows.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<tr class=\"am-click" + (m.Read ? string.Empty : " am-unread") + "\" onclick=\"AdminMessagesJs.view('" + Ui.H(m.Id) + "')\"><td><b>" + Ui.H(m.Name) + "</b><small>" + Ui.H(m.Email) + "</small></td><td>" + Ui.H(TopicLabel(m.Topic)) + "</td><td><span class=\"am-snip\">" + Ui.H(m.Text) + "</span></td><td>" + Ui.H(Ui.Stamp(m.When)) + "</td><td><span class=\"am-pill " + (m.Read ? "am-s-off" : "am-s-new") + "\">" + (m.Read ? "Read" : "Unread") + "</span></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"am-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "message", "messages") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"am-btn am-btn-ghost am-btn-sm\"" + (page <= 1 ? " disabled" : " onclick=\"AdminMessagesJs.go(" + (page - 1) + ")\"") + ">Previous</button>");
            sb.Append("<button type=\"button\" class=\"am-btn am-btn-ghost am-btn-sm\"" + (page >= pages ? " disabled" : " onclick=\"AdminMessagesJs.go(" + (page + 1) + ")\"") + ">Next</button></div></div>");
            return sb.ToString();
        }

        private static string DetailHtml(Ctx c, Enquiry m)
        {
            StringBuilder sb = new StringBuilder("<dl class=\"am-meta\"><dt>From</dt><dd>" + Ui.H(m.Name) + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.H(m.Email) + "\">" + Ui.H(m.Email) + "</a></dd><dt>Topic</dt><dd>" + Ui.H(TopicLabel(m.Topic)) + "</dd><dt>Received</dt><dd>" + Ui.H(Ui.Stamp(m.When)) + "</dd></dl>");
            sb.Append("<div class=\"am-msg\">" + Ui.H(m.Text) + "</div><div class=\"am-formacts\">");
            if (Access.Can(c.U, AreaKey, 'E'))
            {
                sb.Append("<button type=\"button\" class=\"am-btn am-btn-ghost am-btn-red\" onclick=\"AdminMessagesJs.remove('" + Ui.H(m.Id) + "', '" + Ui.H(Ui.Js(m.Name)) + "')\">Delete</button>");
                sb.Append("<button type=\"button\" class=\"am-btn am-btn-ghost\" onclick=\"AdminMessagesJs.mark('" + Ui.H(m.Id) + "', " + (m.Read ? "false" : "true") + ")\">" + (m.Read ? "Mark as unread" : "Mark as read") + "</button>");
            }
            sb.Append("<a class=\"am-btn\" href=\"mailto:" + Ui.H(m.Email) + "\">Reply by email</a></div>");
            return sb.ToString();
        }
    }
}
