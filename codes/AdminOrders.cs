using System.Globalization;
using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminOrders : WebPage
    {
        private const string AreaKey = "orders";
        private const int Size = 10;
        private static readonly string[] Flow = { "new", "baking", "ready", "done" };

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Orders | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminOrdersJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("ao-menu", await Auth.Menu("ao", "AdminOrders", c, site));
            response.SetElementContents("ao-who", Auth.Who("ao", c));
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                response.SetElementContents("ao-main", Auth.NoAccess("ao", "Orders"));
                return response;
            }
            string status = Ui.Pick(GetDataValue("status"), StatusKeys);
            string day = Ui.Pick(GetDataValue("day"), DayKeys);
            if (status == string.Empty && day == string.Empty)
            {
                status = "active";
            }
            response.SetElementContents("ao-main", ShellHtml(status, day) + "<div id=\"ao-list\">" + await ListHtml(c, string.Empty, status, day, string.Empty, 1) + "</div>");
            return response;
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminOrdersJs");
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Auth.Refuse(response, c, "AdminOrdersJs", "view orders", "list", "You do not have access to orders.");
            }
            string html = await ListHtml(c, Ui.Clean(GetDataValue("term"), 40), Ui.Pick(GetDataValue("status"), StatusKeys), Ui.Pick(GetDataValue("day"), DayKeys), Ui.Pick(GetDataValue("mode"), new[] { "pickup", "delivery" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1));
            response.SetElementContents("ao-list", html);
            return response;
        }

        public async Task<ApiResponse> View()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminOrdersJs");
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Auth.Refuse(response, c, "AdminOrdersJs", "view order", GetDataValue("id"), "You do not have access to orders.");
            }
            string id = Ui.Clean(GetDataValue("id"), 12);
            BakeOrder? o = (await Store.LoadOrders()).FirstOrDefault(x => x.Id == id);
            if (o == null)
            {
                response.ExecuteScript("AdminOrdersJs.toast('That order was not found.');");
                return response;
            }
            response.SetElementContents("ao-mbody", DetailHtml(c, o));
            response.ExecuteScript("AdminOrdersJs.openModal('Order " + Ui.Js(o.Id) + "');");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminOrdersJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 12);
            string want = Ui.Pick(GetDataValue("status"), Flow);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminOrdersJs", "order status", id, "You can look at orders but not change them.");
            }
            string message;
            using (await Store.Hold())
            {
                List<BakeOrder> orders = await Store.LoadOrders();
                BakeOrder? o = orders.FirstOrDefault(x => x.Id == id);
                int at = o == null ? -1 : Array.IndexOf(Flow, o.Status);
                if (o == null || at < 0 || at >= Flow.Length - 1 || Flow[at + 1] != want)
                {
                    message = "That order cannot move to that step.";
                }
                else
                {
                    o.Status = want;
                    await Store.SaveOrders(orders);
                    message = o.Id + " is now " + Ui.StatusLabel(want).ToLowerInvariant() + ".";
                    await Store.Log(c.U.Id, "order " + want, o.Id + " " + o.Name, true);
                }
            }
            response.SetElementContents("ao-list", await ListHtml(c, Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fstatus"), StatusKeys), Ui.Pick(GetDataValue("fday"), DayKeys), Ui.Pick(GetDataValue("fmode"), new[] { "pickup", "delivery" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            response.SetElementContents("ao-menu", await Auth.Menu("ao", "AdminOrders", c, await Store.LoadSite()));
            response.ExecuteScript("AdminOrdersJs.done('" + Ui.Js(message) + "');");
            return response;
        }

        public async Task<ApiResponse> Cancel()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminOrdersJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 12);
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Auth.Refuse(response, c, "AdminOrdersJs", "cancel order", id, "You are not allowed to cancel orders.");
            }
            string why = Ui.Clean(GetDataValue("why"), 200);
            string message;
            using (await Store.Hold())
            {
                List<BakeOrder> orders = await Store.LoadOrders();
                BakeOrder? o = orders.FirstOrDefault(x => x.Id == id);
                if (o == null || o.Status == "done" || o.Status == "cancelled")
                {
                    message = "That order can no longer be cancelled.";
                }
                else
                {
                    o.Status = "cancelled";
                    o.CancelReason = why;
                    await Store.SaveOrders(orders);
                    message = o.Id + " was cancelled.";
                    await Store.Log(c.U.Id, "order cancel", o.Id + " " + o.Name + (why == string.Empty ? string.Empty : ": " + why), true);
                }
            }
            response.SetElementContents("ao-list", await ListHtml(c, Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fstatus"), StatusKeys), Ui.Pick(GetDataValue("fday"), DayKeys), Ui.Pick(GetDataValue("fmode"), new[] { "pickup", "delivery" }), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            response.SetElementContents("ao-menu", await Auth.Menu("ao", "AdminOrders", c, await Store.LoadSite()));
            response.ExecuteScript("AdminOrdersJs.done('" + Ui.Js(message) + "');");
            return response;
        }

        private static readonly string[] StatusKeys = { "active", "new", "baking", "ready", "done", "cancelled" };
        private static readonly string[] DayKeys = { "today", "tomorrow", "upcoming", "past" };

        private static string ShellHtml(string status, string day)
        {
            string StatusOpt(string key, string label)
            {
                return "<option value=\"" + key + "\"" + (status == key ? " selected" : string.Empty) + ">" + label + "</option>";
            }
            string DayOpt(string key, string label)
            {
                return "<option value=\"" + key + "\"" + (day == key ? " selected" : string.Empty) + ">" + label + "</option>";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"ao-top\"><div><h1>Orders</h1><p>Everything customers have ordered online.</p></div></div>");
            sb.Append("<div class=\"ao-tools\"><input type=\"search\" id=\"ao-term\" placeholder=\"Search name, number, email, phone\" maxlength=\"40\" aria-label=\"Search orders\">");
            sb.Append("<select id=\"ao-status\" aria-label=\"Status\" onchange=\"AdminOrdersJs.changed()\"><option value=\"\">Any status</option>" + StatusOpt("active", "Active (not done)") + StatusOpt("new", "New") + StatusOpt("baking", "Baking") + StatusOpt("ready", "Ready") + StatusOpt("done", "Done") + StatusOpt("cancelled", "Cancelled") + "</select>");
            sb.Append("<select id=\"ao-day\" aria-label=\"Day\" onchange=\"AdminOrdersJs.changed()\"><option value=\"\">Any day</option>" + DayOpt("today", "Today") + DayOpt("tomorrow", "Tomorrow") + DayOpt("upcoming", "Today and later") + DayOpt("past", "Before today") + "</select>");
            sb.Append("<select id=\"ao-mode\" aria-label=\"Collect or delivery\" onchange=\"AdminOrdersJs.changed()\"><option value=\"\">Collect and delivery</option><option value=\"pickup\">Collect only</option><option value=\"delivery\">Delivery only</option></select></div>");
            return sb.ToString();
        }

        private static async Task<string> ListHtml(Ctx c, string term, string status, string day, string mode, int page)
        {
            List<BakeOrder> all = await Store.LoadOrders();
            string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            IEnumerable<BakeOrder> q = all;
            if (status == "active")
            {
                q = q.Where(o => o.Status != "done" && o.Status != "cancelled");
            }
            else if (status != string.Empty)
            {
                q = q.Where(o => o.Status == status);
            }
            if (day == "today")
            {
                q = q.Where(o => o.Date == today);
            }
            else if (day == "tomorrow")
            {
                q = q.Where(o => o.Date == tomorrow);
            }
            else if (day == "upcoming")
            {
                q = q.Where(o => string.CompareOrdinal(o.Date, today) >= 0);
            }
            else if (day == "past")
            {
                q = q.Where(o => string.CompareOrdinal(o.Date, today) < 0);
            }
            if (mode != string.Empty)
            {
                q = q.Where(o => o.Mode == mode);
            }
            if (term != string.Empty)
            {
                string t = term.Replace("hb-", string.Empty, StringComparison.OrdinalIgnoreCase);
                q = q.Where(o => o.Id.Contains(term, StringComparison.OrdinalIgnoreCase) || o.Number.ToString(CultureInfo.InvariantCulture).Contains(t) || o.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || o.Email.Contains(term, StringComparison.OrdinalIgnoreCase) || o.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            bool ascending = status == "active" || status == "new" || status == "baking" || status == "ready" || day == "today" || day == "tomorrow" || day == "upcoming";
            List<BakeOrder> rows = (ascending ? q.OrderBy(o => o.Date).ThenBy(o => o.Slot).ThenBy(o => o.Number) : q.OrderByDescending(o => o.Date).ThenByDescending(o => o.Number)).ToList();
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            StringBuilder sb = new StringBuilder();
            if (rows.Count == 0)
            {
                sb.Append("<div class=\"ao-tbl-wrap\"><div class=\"ao-empty\">No orders match.</div></div>");
                return sb.ToString();
            }
            bool edit = Access.Can(c.U, AreaKey, 'E');
            sb.Append("<div class=\"ao-tbl-wrap\"><table class=\"ao-tbl\"><thead><tr><th>When</th><th>Order</th><th>Customer</th><th>How</th><th class=\"ao-num\">Total</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (BakeOrder o in rows.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<tr class=\"ao-click\" onclick=\"AdminOrdersJs.view('" + o.Id + "')\"><td><b>" + Ui.H(Ui.Day(o.Date)) + "</b><small>" + Ui.H(o.Slot.Replace("-", " to ")) + "</small></td>");
                sb.Append("<td>" + Ui.H(o.Id) + "<small>" + Ui.Count(o.Lines.Sum(l => l.Qty), "item", "items") + "</small></td>");
                sb.Append("<td>" + Ui.H(o.Name) + "<small>" + Ui.H(o.Phone) + "</small></td><td>" + (o.Mode == "delivery" ? "Delivery" : "Collect") + "</td><td class=\"ao-num\">" + Ui.Money(o.Total) + "</td>");
                sb.Append("<td><span class=\"ao-pill ao-s-" + o.Status + "\">" + Ui.StatusLabel(o.Status) + "</span></td><td><div class=\"ao-acts\" onclick=\"event.stopPropagation()\">" + (edit ? NextButton(o) : string.Empty) + "</div></td></tr>");
            }
            sb.Append("</tbody></table></div>");
            sb.Append("<div class=\"ao-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "order", "orders") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"ao-btn ao-btn-ghost ao-btn-sm\"" + (page <= 1 ? " disabled" : " onclick=\"AdminOrdersJs.go(" + (page - 1) + ")\"") + ">Previous</button>");
            sb.Append("<button type=\"button\" class=\"ao-btn ao-btn-ghost ao-btn-sm\"" + (page >= pages ? " disabled" : " onclick=\"AdminOrdersJs.go(" + (page + 1) + ")\"") + ">Next</button></div></div>");
            return sb.ToString();
        }

        private static string NextButton(BakeOrder o)
        {
            int at = Array.IndexOf(Flow, o.Status);
            if (at < 0 || at >= Flow.Length - 1)
            {
                return string.Empty;
            }
            return "<button type=\"button\" class=\"ao-btn ao-btn-sm\" onclick=\"" + AdvanceCall(o) + "\">" + NextLabel(o) + "</button>";
        }

        private static string NextLabel(BakeOrder o)
        {
            return o.Status switch
            {
                "new" => "Start baking",
                "baking" => "Mark ready",
                _ => o.Mode == "delivery" ? "Mark delivered" : "Mark collected"
            };
        }

        private static string AdvanceCall(BakeOrder o)
        {
            int at = Array.IndexOf(Flow, o.Status);
            string title = NextLabel(o) + " for " + o.Id + "?";
            string text = o.Status == "ready" ? "This closes the order." : "The customer can see the new status on the Track page.";
            return "AdminOrdersJs.advance('" + o.Id + "', '" + Flow[at + 1] + "', '" + Ui.Js(title) + "', '" + Ui.Js(text) + "')";
        }

        private static string DetailHtml(Ctx c, BakeOrder o)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p><span class=\"ao-pill ao-s-" + o.Status + "\">" + Ui.StatusLabel(o.Status) + "</span></p><br>");
            sb.Append("<dl class=\"ao-detail\"><dt>When</dt><dd>" + Ui.H(Ui.Day(o.Date)) + ", " + Ui.H(o.Slot.Replace("-", " to ")) + "</dd>");
            sb.Append("<dt>How</dt><dd>" + (o.Mode == "delivery" ? "Delivery to " + Ui.H(o.Address) + ", " + Ui.H(o.Zip) : "Collect at the counter") + "</dd>");
            sb.Append("<dt>Customer</dt><dd>" + Ui.H(o.Name) + "</dd><dt>Phone</dt><dd>" + Ui.H(o.Phone) + "</dd><dt>Email</dt><dd>" + Ui.H(o.Email) + "</dd>");
            sb.Append("<dt>Note</dt><dd>" + (o.Note == string.Empty ? "None" : Ui.H(o.Note)) + "</dd>");
            sb.Append("<dt>Placed</dt><dd>" + Ui.H(Ui.Stamp(o.Created)) + "</dd>");
            if (o.Status == "cancelled" && o.CancelReason != string.Empty)
            {
                sb.Append("<dt>Reason</dt><dd>" + Ui.H(o.CancelReason) + "</dd>");
            }
            sb.Append("</dl><ul class=\"ao-lines\">");
            foreach (OrderLine l in o.Lines)
            {
                sb.Append("<li><span>" + l.Qty + " x " + Ui.H(l.Title) + "</span><span>" + Ui.Money(l.Price * l.Qty) + "</span></li>");
            }
            if (o.Delivery > 0)
            {
                sb.Append("<li><span>Delivery</span><span>" + Ui.Money(o.Delivery) + "</span></li>");
            }
            sb.Append("<li class=\"ao-sumrow\"><span>Total, unpaid</span><span>" + Ui.Money(o.Total) + "</span></li></ul>");
            string acts = string.Empty;
            if (Access.Can(c.U, AreaKey, 'E') && Array.IndexOf(Flow, o.Status) is int at && at >= 0 && at < Flow.Length - 1)
            {
                acts += "<button type=\"button\" class=\"ao-btn\" onclick=\"" + AdvanceCall(o) + "\">" + NextLabel(o) + "</button>";
            }
            if (Access.Can(c.U, AreaKey, 'X') && o.Status != "done" && o.Status != "cancelled")
            {
                acts += "<button type=\"button\" class=\"ao-btn ao-btn-ghost ao-btn-red\" onclick=\"AdminOrdersJs.cancel('" + o.Id + "', '" + Ui.H(o.Id) + "')\">Cancel order</button>";
            }
            sb.Append("<div class=\"ao-formacts\">" + acts + "<button type=\"button\" class=\"ao-btn ao-btn-ghost\" onclick=\"AdminOrdersJs.closeModal()\">Close</button></div>");
            return sb.ToString();
        }
    }
}
