using System.Globalization;
using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Order : WebPage
    {
        private const int MaxPerItem = 20;
        private const int MaxLines = 25;

        private sealed class Basket
        {
            public List<OrderLine> Lines { get; } = new List<OrderLine>();
            public List<string> Notes { get; set; } = new List<string>();
            public string Clean { get; set; } = string.Empty;
            public decimal Subtotal { get; set; }
            public int Qty { get; set; }
        }

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Your order | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Choose collection or delivery, pick a time and place your Hearthstone Bakery order.");

            SiteData site = await Store.LoadSite();
            OrderRules r = site.Rules;
            string feeLine = "From " + Ui.Money(r.DeliveryFee) + ", free over " + Ui.Money(r.FreeDeliveryOver) + ".";
            string zones = r.DeliveryZips.Count == 0 ? "Delivery is not available right now." : "We deliver to ZIP codes " + string.Join(", ", r.DeliveryZips) + ". Minimum order " + Ui.Money(r.MinDelivery) + ".";
            string lead = "We need about " + Ui.Count(r.LeadHours, "hour", "hours") + " to bake, so only times we can still make are shown. You can order up to " + Ui.Count(r.DaysAhead, "day", "days") + " ahead.";
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText
                .Replace("{plhd_feeline}", HtmlEncode(feeLine))
                .Replace("{plhd_zones}", HtmlEncode(zones))
                .Replace("{plhd_leadline}", HtmlEncode(lead))
                .Replace("{plhd_dates}", DateSelect(r, string.Empty)), site);
        }

        public async Task<ApiResponse> Quote()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            List<BakeOrder> orders = await Store.LoadOrders();
            Basket basket = BuildBasket(site, GetDataValue("cart"));
            string mode = Ui.Pick(GetDataValue("mode"), new[] { "pickup", "delivery" });
            if (mode == string.Empty)
            {
                mode = "pickup";
            }
            string date = (GetDataValue("date") ?? string.Empty).Trim();
            bool reset = GetDataValue("fresh") == "1";
            if (DateProblem(site.Rules, date) != string.Empty)
            {
                date = string.Empty;
                reset = true;
            }
            string slot = (GetDataValue("slot") ?? string.Empty).Trim();
            response.SetElementContents("od-lines", LinesHtml(basket));
            response.SetElementContents("od-sum", SummaryHtml(basket, mode, site.Rules));
            if (reset)
            {
                response.SetElementContents("od-datebox", DateSelect(site.Rules, date));
            }
            response.SetElementContents("od-slotbox", SlotSelect(site.Rules, orders, date, slot));
            response.ExecuteScript("OrderJs.cartSet('" + Ui.Js(basket.Clean) + "');");
            return response;
        }

        public async Task<ApiResponse> Place()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            OrderRules r = site.Rules;
            Basket basket = BuildBasket(site, GetDataValue("cart"));
            string mode = Ui.Pick(GetDataValue("mode"), new[] { "pickup", "delivery" });
            string date = (GetDataValue("date") ?? string.Empty).Trim();
            string slot = (GetDataValue("slot") ?? string.Empty).Trim();
            string name = Ui.Clean(GetDataValue("name"), 60);
            string email = Ui.Clean(GetDataValue("email"), 80);
            string phone = Ui.Clean(GetDataValue("phone"), 30);
            string address = Ui.Clean(GetDataValue("address"), 120);
            string zip = Ui.Clean(GetDataValue("zip"), 5);
            string note = Ui.Clean(GetDataValue("note"), 200);
            bool delivery = mode == "delivery";

            string eDate = DateProblem(r, date);
            string eName = name.Length < 2 ? "Please enter your name." : string.Empty;
            string eEmail = !Ui.IsEmail(email) ? "Please enter a valid email address." : string.Empty;
            string ePhone = !Ui.IsPhone(phone) ? "Please enter a phone number we can call." : string.Empty;
            string eAddress = delivery && address.Length < 5 ? "Please enter your street address." : string.Empty;
            string eZip = string.Empty;
            if (delivery)
            {
                eZip = zip.Length != 5 || !zip.All(char.IsDigit) ? "Please enter a 5-digit ZIP code." : !r.DeliveryZips.Contains(zip) ? "Sorry, we do not deliver to this ZIP code yet." : string.Empty;
            }
            string eSlot = slot == string.Empty ? "Please choose a time." : !r.Slots.Contains(slot) ? "Please choose one of the times listed." : string.Empty;
            string eForm = string.Empty;
            if (basket.Lines.Count == 0)
            {
                eForm = "Your basket is empty. Add something from the menu first.";
            }
            else if (basket.Notes.Count > 0)
            {
                eForm = string.Join(" ", basket.Notes) + " We updated your basket. Please check it and try again.";
            }
            else if (delivery && basket.Subtotal < r.MinDelivery)
            {
                eForm = "Delivery needs an order of at least " + Ui.Money(r.MinDelivery) + ". Add more or choose collection.";
            }
            if (mode == string.Empty)
            {
                eForm = "Please choose collect or delivery.";
            }

            response.SetElementContents("od-e-date", eDate);
            response.SetElementContents("od-e-slot", eSlot);
            response.SetElementContents("od-e-name", eName);
            response.SetElementContents("od-e-email", eEmail);
            response.SetElementContents("od-e-phone", ePhone);
            response.SetElementContents("od-e-address", eAddress);
            response.SetElementContents("od-e-zip", eZip);
            response.SetElementContents("od-e-note", string.Empty);
            if (eDate + eSlot + eName + eEmail + ePhone + eAddress + eZip + eForm != string.Empty)
            {
                response.SetElementContents("od-e-form", eForm == string.Empty ? "Please fix the fields marked above." : eForm);
                if (basket.Notes.Count > 0)
                {
                    response.SetElementContents("od-lines", LinesHtml(basket));
                    response.SetElementContents("od-sum", SummaryHtml(basket, mode == string.Empty ? "pickup" : mode, r));
                    response.ExecuteScript("OrderJs.cartSet('" + Ui.Js(basket.Clean) + "');");
                }
                response.ExecuteScript("OrderJs.failed();");
                return response;
            }

            BakeOrder order;
            string slotProblem;
            using (await Store.Hold())
            {
                List<BakeOrder> orders = await Store.LoadOrders();
                slotProblem = SlotProblem(r, orders, date, slot);
                if (slotProblem != string.Empty)
                {
                    order = new BakeOrder();
                }
                else
                {
                    int number = Store.NextOrderNumber(orders);
                    decimal fee = Fee(r, mode, basket.Subtotal);
                    order = new BakeOrder
                    {
                        Number = number,
                        Id = "HB-" + number.ToString(CultureInfo.InvariantCulture),
                        Created = DateTime.UtcNow,
                        Status = "new",
                        Mode = mode,
                        Date = date,
                        Slot = slot,
                        Name = name,
                        Email = email,
                        Phone = phone,
                        Address = delivery ? address : string.Empty,
                        Zip = delivery ? zip : string.Empty,
                        Note = note,
                        Lines = basket.Lines,
                        Subtotal = basket.Subtotal,
                        Delivery = fee,
                        Total = basket.Subtotal + fee
                    };
                    orders.Add(order);
                    await Store.SaveOrders(orders);
                }
            }
            if (slotProblem != string.Empty)
            {
                response.SetElementContents("od-e-slot", slotProblem);
                response.SetElementContents("od-e-form", "That time window is no longer available. Please choose another.");
                List<BakeOrder> fresh = await Store.LoadOrders();
                response.SetElementContents("od-slotbox", SlotSelect(r, fresh, date, string.Empty));
                response.ExecuteScript("OrderJs.failed();");
                return response;
            }

            response.SetElementContents("od-e-form", string.Empty);
            response.SetElementContents("od-done", DoneHtml(order, site));
            response.ExecuteScript("OrderJs.placed();");
            return response;
        }

        private static Basket BuildBasket(SiteData site, string? raw)
        {
            Basket b = new Basket();
            Dictionary<string, int> want = new Dictionary<string, int>();
            foreach (string part in (raw ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] bits = part.Split(':');
                if (bits.Length != 2 || !int.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty < 1)
                {
                    continue;
                }
                string key = bits[0].Trim();
                want[key] = Math.Min(MaxPerItem, (want.TryGetValue(key, out int have) ? have : 0) + qty);
            }
            List<string> clean = new List<string>();
            foreach (KeyValuePair<string, int> w in want.Take(MaxLines))
            {
                Product? p = site.Products.FirstOrDefault(x => x.Key == w.Key);
                if (p == null)
                {
                    b.Notes.Add("An item in your basket is no longer on the menu and was removed.");
                    continue;
                }
                if (!p.Available)
                {
                    b.Notes.Add(p.Title + " is sold out today and was removed.");
                    continue;
                }
                b.Lines.Add(new OrderLine { Key = p.Key, Title = p.Title, Qty = w.Value, Price = p.Price });
                clean.Add(p.Key + ":" + w.Value.ToString(CultureInfo.InvariantCulture));
                b.Subtotal += p.Price * w.Value;
                b.Qty += w.Value;
            }
            b.Notes = b.Notes.Distinct().ToList();
            b.Clean = string.Join(";", clean);
            return b;
        }

        private static decimal Fee(OrderRules r, string mode, decimal subtotal)
        {
            return mode == "delivery" && subtotal < r.FreeDeliveryOver ? r.DeliveryFee : 0m;
        }

        private static DateTime SlotStart(string date, string slot)
        {
            string start = slot.Split('-')[0];
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) && TimeSpan.TryParse(start, CultureInfo.InvariantCulture, out TimeSpan t))
            {
                return d.Date + t;
            }
            return DateTime.MaxValue;
        }

        private static string DateProblem(OrderRules r, string date)
        {
            if (date == string.Empty)
            {
                return "Please choose a day.";
            }
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            {
                return "Please choose a day from the list.";
            }
            if (d.Date < DateTime.Today || d.Date > DateTime.Today.AddDays(r.DaysAhead) || !r.OpenDays.Contains((int)d.DayOfWeek) || r.ClosedDates.Contains(date))
            {
                return "We are not taking orders for that day.";
            }
            return string.Empty;
        }

        private static string SlotProblem(OrderRules r, List<BakeOrder> orders, string date, string slot)
        {
            if (!r.Slots.Contains(slot))
            {
                return "Please choose one of the times listed.";
            }
            if (SlotStart(date, slot) < DateTime.Now.AddHours(r.LeadHours))
            {
                return "That time is too soon for us to bake.";
            }
            if (orders.Count(o => o.Date == date && o.Slot == slot && o.Status != "cancelled") >= r.SlotCapacity)
            {
                return "That time window is fully booked.";
            }
            return string.Empty;
        }

        private static string DateSelect(OrderRules r, string selected)
        {
            StringBuilder sb = new StringBuilder("<select id=\"od-date\" onchange=\"OrderJs.requote()\"><option value=\"\">Choose a day</option>");
            for (int i = 0; i <= r.DaysAhead; i++)
            {
                DateTime day = DateTime.Today.AddDays(i);
                string iso = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (DateProblem(r, iso) != string.Empty || !r.Slots.Any(s => SlotStart(iso, s) >= DateTime.Now.AddHours(r.LeadHours)))
                {
                    continue;
                }
                string label = day.ToString("dddd d MMM", CultureInfo.InvariantCulture) + (i == 0 ? " (today)" : i == 1 ? " (tomorrow)" : string.Empty);
                sb.Append("<option value=\"" + iso + "\"" + (iso == selected ? " selected" : string.Empty) + ">" + HtmlEncode(label) + "</option>");
            }
            return sb.Append("</select>").ToString();
        }

        private static string SlotSelect(OrderRules r, List<BakeOrder> orders, string date, string selected)
        {
            if (date == string.Empty)
            {
                return "<select id=\"od-slot\"><option value=\"\">Choose a day first</option></select>";
            }
            StringBuilder sb = new StringBuilder("<select id=\"od-slot\"><option value=\"\">Choose a time</option>");
            foreach (string slot in r.Slots)
            {
                string problem = SlotProblem(r, orders, date, slot);
                int left = r.SlotCapacity - orders.Count(o => o.Date == date && o.Slot == slot && o.Status != "cancelled");
                string label = slot.Replace("-", " to ");
                if (problem.Contains("soon"))
                {
                    label += " (too soon)";
                }
                else if (problem.Contains("booked"))
                {
                    label += " (fully booked)";
                }
                else if (left <= 2)
                {
                    label += " (" + left + " left)";
                }
                sb.Append("<option value=\"" + HtmlEncode(slot) + "\"" + (problem != string.Empty ? " disabled" : string.Empty) + (slot == selected && problem == string.Empty ? " selected" : string.Empty) + ">" + HtmlEncode(label) + "</option>");
            }
            return sb.Append("</select>").ToString();
        }

        private static string LinesHtml(Basket b)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string n in b.Notes)
            {
                sb.Append("<p class=\"od-warn\">" + HtmlEncode(n) + "</p>");
            }
            if (b.Lines.Count == 0)
            {
                sb.Append("<div class=\"od-empty\"><p>Your basket is empty.</p><a class=\"od-btn\" href=\"Menu\">Browse the menu</a></div>");
                return sb.ToString();
            }
            foreach (OrderLine l in b.Lines)
            {
                sb.Append("<div class=\"od-line\"><img src=\"images/products/" + HtmlEncode(l.Key) + ".webp\" alt=\"\"><div class=\"od-line-t\"><b>" + HtmlEncode(l.Title) + "</b><small>" + Ui.Money(l.Price) + " each</small></div>");
                sb.Append("<div class=\"od-qty\"><button type=\"button\" aria-label=\"One fewer " + HtmlEncode(l.Title) + "\" onclick=\"OrderJs.step('" + l.Key + "', -1)\">&minus;</button><span>" + l.Qty + "</span><button type=\"button\" aria-label=\"One more " + HtmlEncode(l.Title) + "\" onclick=\"OrderJs.step('" + l.Key + "', 1)\">+</button></div>");
                sb.Append("<em>" + Ui.Money(l.Price * l.Qty) + "</em><button type=\"button\" class=\"od-x\" aria-label=\"Remove " + HtmlEncode(l.Title) + "\" onclick=\"OrderJs.drop('" + l.Key + "')\">&times;</button></div>");
            }
            return sb.ToString();
        }

        private static string SummaryHtml(Basket b, string mode, OrderRules r)
        {
            if (b.Lines.Count == 0)
            {
                return "<p class=\"od-muted\">Add something from the menu to see your total.</p>";
            }
            decimal fee = Fee(r, mode, b.Subtotal);
            StringBuilder sb = new StringBuilder("<div class=\"od-rows\">");
            sb.Append("<div class=\"od-row\"><span>" + Ui.Count(b.Qty, "item", "items") + "</span><span>" + Ui.Money(b.Subtotal) + "</span></div>");
            sb.Append("<div class=\"od-row\"><span>" + (mode == "delivery" ? "Delivery" : "Collection") + "</span><span>" + (fee == 0 ? "Free" : Ui.Money(fee)) + "</span></div>");
            sb.Append("<div class=\"od-row od-total\"><span>Total</span><span>" + Ui.Money(b.Subtotal + fee) + "</span></div></div>");
            if (mode == "delivery" && b.Subtotal < r.MinDelivery)
            {
                sb.Append("<p class=\"od-hint\">Delivery needs at least " + Ui.Money(r.MinDelivery) + ". Add " + Ui.Money(r.MinDelivery - b.Subtotal) + " more, or choose collection.</p>");
            }
            else if (mode == "delivery" && fee > 0)
            {
                sb.Append("<p class=\"od-hint\">Add " + Ui.Money(r.FreeDeliveryOver - b.Subtotal) + " more for free delivery.</p>");
            }
            sb.Append("<p class=\"od-muted\">Prices include tax.</p>");
            return sb.ToString();
        }

        private static string DoneHtml(BakeOrder o, SiteData site)
        {
            string how = o.Mode == "delivery" ? "Delivery to " + HtmlEncode(o.Address) + ", " + HtmlEncode(o.Zip) : "Collect from " + HtmlEncode(site.Address);
            StringBuilder sb = new StringBuilder("<div class=\"od-done-in\">");
            sb.Append("<h2>Thank you, " + HtmlEncode(o.Name) + "!</h2>");
            sb.Append("<p>Your order <span class=\"od-code\">" + HtmlEncode(o.Id) + "</span> is in. We will start baking when your day comes around. Keep the number and the email you used to check on it any time.</p>");
            sb.Append("<dl><dt>When</dt><dd>" + HtmlEncode(Ui.Day(o.Date)) + ", " + HtmlEncode(o.Slot.Replace("-", " to ")) + "</dd>");
            sb.Append("<dt>How</dt><dd>" + how + "</dd>");
            sb.Append("<dt>Items</dt><dd>" + HtmlEncode(string.Join(", ", o.Lines.Select(l => l.Qty + " x " + l.Title))) + "</dd>");
            sb.Append("<dt>Total</dt><dd><b>" + Ui.Money(o.Total) + "</b>, payable when you " + (o.Mode == "delivery" ? "receive it" : "collect") + "</dd></dl>");
            sb.Append("<div class=\"od-done-acts\"><a class=\"od-btn\" href=\"Track?o=" + o.Number + "\">Track this order</a><a class=\"od-btn od-btn-ghost\" href=\"Menu\">Back to the menu</a></div></div>");
            return sb.ToString();
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
