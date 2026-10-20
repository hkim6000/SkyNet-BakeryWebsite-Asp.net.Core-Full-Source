using System.Globalization;
using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Admin : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Dashboard | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("db-menu", await Auth.Menu("db", "Admin", c, site));
            response.SetElementContents("db-who", Auth.Who("db", c));
            response.SetElementContents("db-main", await MainHtml(c));
            return response;
        }

        private static async Task<string> MainHtml(Ctx c)
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"db-top\"><div><h1>Hello, " + Ui.H(c.U.Name) + "</h1><p>" + DateTime.Today.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture) + "</p></div></div>");
            bool seeOrders = Access.Can(c.U, "orders", 'V');
            bool seeMessages = Access.Can(c.U, "messages", 'V');
            List<BakeOrder> orders = seeOrders ? await Store.LoadOrders() : new List<BakeOrder>();
            List<BakeOrder> open = orders.Where(o => o.Status != "done" && o.Status != "cancelled").ToList();
            sb.Append("<div class=\"db-stats\">");
            if (seeOrders)
            {
                sb.Append("<a class=\"db-stat\" href=\"AdminOrders?f=new\"><b>" + orders.Count(o => o.Status == "new") + "</b><span>New orders</span><small>Waiting to be started</small></a>");
                sb.Append("<a class=\"db-stat\" href=\"AdminOrders?f=today\"><b>" + open.Count(o => o.Date == today) + "</b><span>Due today</span><small>Still to bake or hand over</small></a>");
                sb.Append("<a class=\"db-stat\" href=\"AdminOrders?f=tomorrow\"><b>" + open.Count(o => o.Date == tomorrow) + "</b><span>Due tomorrow</span><small>Plan the bake</small></a>");
            }
            if (seeMessages)
            {
                int unread = (await Store.LoadMessages()).Count(m => !m.Read);
                sb.Append("<a class=\"db-stat\" href=\"AdminMessages\"><b>" + unread + "</b><span>Unread messages</span><small>From the contact form</small></a>");
            }
            sb.Append("</div>");
            if (seeOrders)
            {
                List<BakeOrder> list = orders.Where(o => o.Date == today && o.Status != "cancelled").OrderBy(o => o.Slot).ThenBy(o => o.Number).Take(10).ToList();
                sb.Append("<div class=\"db-card\"><h2>Today&rsquo;s orders</h2>");
                if (list.Count == 0)
                {
                    sb.Append("<p class=\"db-muted\">No orders are due today.</p>");
                }
                else
                {
                    sb.Append("<div class=\"db-tbl-wrap\"><table class=\"db-tbl\"><thead><tr><th>Time</th><th>Order</th><th>Customer</th><th>How</th><th class=\"db-num\">Total</th><th>Status</th></tr></thead><tbody>");
                    foreach (BakeOrder o in list)
                    {
                        sb.Append("<tr><td>" + Ui.H(o.Slot.Replace("-", " to ")) + "</td><td><b>" + Ui.H(o.Id) + "</b></td><td>" + Ui.H(o.Name) + "</td><td>" + (o.Mode == "delivery" ? "Delivery" : "Collect") + "</td><td class=\"db-num\">" + Ui.Money(o.Total) + "</td><td><span class=\"db-pill db-s-" + o.Status + "\">" + Ui.StatusLabel(o.Status) + "</span></td></tr>");
                    }
                    sb.Append("</tbody></table></div>");
                }
                sb.Append("</div>");
            }
            sb.Append("<div class=\"db-links\">");
            foreach (Area a in Access.Areas)
            {
                if (Access.Level(c.U, a.Key) != string.Empty)
                {
                    sb.Append("<a class=\"db-link\" href=\"" + a.Page + "\">" + Ui.H(a.Name) + "<small>" + Ui.H(Describe(a.Key)) + "</small></a>");
                }
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Describe(string key)
        {
            return key switch
            {
                "orders" => "See, update and cancel orders",
                "products" => "Prices and what is sold out",
                "messages" => "Read what customers wrote",
                "activity" => "Who changed what",
                "settings" => "Opening days, times and fees",
                "users" => "Staff and their rights",
                _ => string.Empty
            };
        }
    }
}
