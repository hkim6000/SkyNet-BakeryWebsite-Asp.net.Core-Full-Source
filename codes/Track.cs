using System.Globalization;
using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Track : WebPage
    {
        private static readonly string[] Flow = { "new", "baking", "ready", "done" };

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Track your order | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Check the status of your Hearthstone Bakery order.");

            SiteData site = await Store.LoadSite();
            string num = Ui.Clean(QueryValue("o"), 8);
            string shown = num.Length > 0 && num.All(char.IsDigit) ? "HB-" + num : string.Empty;
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText.Replace("{plhd_num}", HtmlEncode(shown)), site);
        }

        public async Task<ApiResponse> Find()
        {
            ApiResponse response = new ApiResponse();
            string raw = Ui.Clean(GetDataValue("num"), 20).ToUpperInvariant().Replace("HB-", string.Empty).Replace("#", string.Empty).Trim();
            string email = Ui.Clean(GetDataValue("mail"), 80);
            bool numOk = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) && number > 0;
            string eNum = numOk ? string.Empty : "Enter the order number, for example HB-1001.";
            string eEmail = Ui.IsEmail(email) ? string.Empty : "Enter the email you used to order.";
            response.SetElementContents("tr-e-num", eNum);
            response.SetElementContents("tr-e-email", eEmail);
            if (eNum + eEmail != string.Empty)
            {
                response.SetElementContents("tr-result", string.Empty);
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<BakeOrder> orders = await Store.LoadOrders();
            BakeOrder? o = orders.FirstOrDefault(x => x.Number == number && x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (o == null)
            {
                response.SetElementContents("tr-result", "<div class=\"tr-card-o\"><h2>We could not find that order</h2><p class=\"tr-stat\">Check the number and the email, or call us on " + HtmlEncode(site.Phone) + ".</p></div>");
                return response;
            }
            response.SetElementContents("tr-result", OrderHtml(o, site));
            return response;
        }

        private static string OrderHtml(BakeOrder o, SiteData site)
        {
            StringBuilder sb = new StringBuilder("<div class=\"tr-card-o\">");
            sb.Append("<h2>Order " + HtmlEncode(o.Id) + "</h2><p class=\"tr-stat\">" + HtmlEncode(StatusText(o)) + "</p>");
            if (o.Status == "cancelled")
            {
                sb.Append("<div class=\"tr-cancelled\">This order was cancelled." + (o.CancelReason == string.Empty ? string.Empty : " Reason: " + HtmlEncode(o.CancelReason)) + " Call us on " + HtmlEncode(site.Phone) + " if you have questions.</div>");
            }
            else
            {
                int at = Array.IndexOf(Flow, o.Status);
                sb.Append("<ol class=\"tr-flow\">");
                for (int i = 0; i < Flow.Length; i++)
                {
                    string label = i == 2 ? (o.Mode == "delivery" ? "Out for delivery" : "Ready to collect") : i == 0 ? "Received" : Ui.StatusLabel(Flow[i]);
                    sb.Append("<li class=\"" + (i < at ? "tr-did" : i == at ? "tr-now" : string.Empty) + "\">" + HtmlEncode(label) + "</li>");
                }
                sb.Append("</ol>");
            }
            sb.Append("<dl class=\"tr-info\"><dt>When</dt><dd>" + HtmlEncode(Ui.Day(o.Date)) + ", " + HtmlEncode(o.Slot.Replace("-", " to ")) + "</dd>");
            sb.Append("<dt>How</dt><dd>" + (o.Mode == "delivery" ? "Delivery to " + HtmlEncode(o.Address) + ", " + HtmlEncode(o.Zip) : "Collect from " + HtmlEncode(site.Address)) + "</dd>");
            sb.Append("<dt>Name</dt><dd>" + HtmlEncode(o.Name) + "</dd></dl><ul class=\"tr-items\">");
            foreach (OrderLine l in o.Lines)
            {
                sb.Append("<li><span>" + l.Qty + " x " + HtmlEncode(l.Title) + "</span><span>" + Ui.Money(l.Price * l.Qty) + "</span></li>");
            }
            if (o.Delivery > 0)
            {
                sb.Append("<li><span>Delivery</span><span>" + Ui.Money(o.Delivery) + "</span></li>");
            }
            sb.Append("<li class=\"tr-sum\"><span>Total, payable on " + (o.Mode == "delivery" ? "delivery" : "collection") + "</span><span>" + Ui.Money(o.Total) + "</span></li></ul></div>");
            return sb.ToString();
        }

        private static string StatusText(BakeOrder o)
        {
            return o.Status switch
            {
                "new" => "We have your order and will start baking on the day.",
                "baking" => "Your order is in the oven.",
                "ready" => o.Mode == "delivery" ? "Your order is on its way." : "Your order is ready at the counter.",
                "done" => "Completed. Thank you for ordering.",
                _ => "This order was cancelled."
            };
        }

        private static string HoursHtml(SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            foreach (HourLine h in site.Hours)
            {
                sb.Append("<span>" + HtmlEncode(h.Days) + ": " + HtmlEncode(h.Time) + "</span>");
            }
            return sb.ToString();
        }

        private static string WithFooter(string html, SiteData site)
        {
            return html
                .Replace("{plhd_f_tag}", HtmlEncode(site.Tagline))
                .Replace("{plhd_f_address}", HtmlEncode(site.Address))
                .Replace("{plhd_f_phone}", HtmlEncode(site.Phone) + " &middot; " + HtmlEncode(site.Email))
                .Replace("{plhd_f_hours}", HoursHtml(site));
        }
    }
}
