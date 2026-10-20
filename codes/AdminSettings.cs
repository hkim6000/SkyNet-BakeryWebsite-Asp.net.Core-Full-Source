using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminSettings : WebPage
    {
        private const string AreaKey = "settings";
        private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Settings | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminSettingsJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("as-menu", await Auth.Menu("as", "AdminSettings", c, site));
            response.SetElementContents("as-who", Auth.Who("as", c));
            response.SetElementContents("as-main", Access.Can(c.U, AreaKey, 'V') ? MainHtml(site, Access.Can(c.U, AreaKey, 'E')) : Auth.NoAccess("as", "Settings"));
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminSettingsJs");
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminSettingsJs", "save settings", "settings", "Only managers can change settings.");
            }
            string phone = Ui.Clean(GetDataValue("phone"), 30);
            string email = Ui.Clean(GetDataValue("email"), 80);
            string address = Ui.Clean(GetDataValue("address"), 120);
            List<HourLine> hours = new List<HourLine>();
            string eHours = string.Empty;
            foreach (string line in (GetDataValue("hours") ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x != string.Empty))
            {
                string[] bits = line.Split('|');
                if (bits.Length != 2 || bits[0].Trim().Length == 0 || bits[1].Trim().Length == 0 || bits[0].Length > 40 || bits[1].Length > 40)
                {
                    eHours = "Write each line as days | times, for example Saturday | 7:00 to 15:00.";
                    break;
                }
                hours.Add(new HourLine { Days = bits[0].Trim(), Time = bits[1].Trim() });
            }
            if (eHours == string.Empty && (hours.Count == 0 || hours.Count > 7))
            {
                eHours = "Enter between 1 and 7 lines.";
            }
            bool feeOk = Ui.Decimal(GetDataValue("fee"), out decimal fee) && fee >= 0 && fee <= 100;
            bool freeOk = Ui.Decimal(GetDataValue("free"), out decimal free) && free >= 0 && free <= 1000;
            bool minOk = Ui.Decimal(GetDataValue("min"), out decimal min) && min >= 0 && min <= 1000;
            int lead = Ui.Bound(GetDataValue("lead"), -1, 72, -1);
            int ahead = Ui.Bound(GetDataValue("ahead"), 0, 60, 0);
            int cap = Ui.Bound(GetDataValue("cap"), 0, 100, 0);
            List<int> days = (GetDataValue("days") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out int n) ? n : -1).Where(n => n >= 0 && n <= 6).Distinct().OrderBy(n => n).ToList();
            List<string> slots = new List<string>();
            string eSlots = string.Empty;
            foreach (string line in (GetDataValue("slots") ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x != string.Empty))
            {
                Match m = Regex.Match(line, "^([01][0-9]|2[0-3]):([0-5][0-9])-([01][0-9]|2[0-3]):([0-5][0-9])$");
                if (!m.Success || string.CompareOrdinal(line.Substring(0, 5), line.Substring(6, 5)) >= 0)
                {
                    eSlots = "Write each window as 08:00-10:00, start before end.";
                    break;
                }
                slots.Add(line);
            }
            if (eSlots == string.Empty && (slots.Count == 0 || slots.Count > 8))
            {
                eSlots = "Enter between 1 and 8 time windows.";
            }
            List<string> zips = (GetDataValue("zips") ?? string.Empty).Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            string eZips = zips.Any(z => z.Length != 5 || !z.All(char.IsDigit)) ? "Use 5-digit ZIP codes separated by commas." : string.Empty;
            List<string> closed = (GetDataValue("closed") ?? string.Empty).Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            string eClosed = closed.Any(d => !DateTime.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) ? "Use dates like 2026-12-25, one per line." : string.Empty;
            string ePhone = phone.Length < 5 ? "Enter a phone number." : string.Empty;
            string eEmail = Ui.IsEmail(email) ? string.Empty : "Enter a valid email address.";
            string eAddress = address.Length < 5 ? "Enter the bakery address." : string.Empty;
            string eFee = feeOk ? string.Empty : "Enter an amount from 0 to 100.";
            string eFree = freeOk ? string.Empty : "Enter an amount from 0 to 1,000.";
            string eMin = minOk ? string.Empty : "Enter an amount from 0 to 1,000.";
            string eLead = lead < 0 ? "Enter hours from 0 to 72." : string.Empty;
            string eAhead = ahead < 1 ? "Enter days from 1 to 60." : string.Empty;
            string eCap = cap < 1 ? "Enter a number from 1 to 100." : string.Empty;
            string eDays = days.Count == 0 ? "Choose at least one open day." : string.Empty;
            string[] ids = { "phone", "email", "address", "hours", "fee", "free", "min", "lead", "ahead", "cap", "days", "slots", "zips", "closed" };
            string[] errs = { ePhone, eEmail, eAddress, eHours, eFee, eFree, eMin, eLead, eAhead, eCap, eDays, eSlots, eZips, eClosed };
            for (int i = 0; i < ids.Length; i++)
            {
                response.SetElementContents("as-e-" + ids[i], errs[i]);
            }
            if (string.Concat(errs) != string.Empty)
            {
                response.ExecuteScript("AdminSettingsJs.failed();");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Phone = phone;
                site.Email = email;
                site.Address = address;
                site.Hours = hours;
                site.Rules.DeliveryFee = fee;
                site.Rules.FreeDeliveryOver = free;
                site.Rules.MinDelivery = min;
                site.Rules.LeadHours = lead;
                site.Rules.DaysAhead = ahead;
                site.Rules.SlotCapacity = cap;
                site.Rules.OpenDays = days;
                site.Rules.Slots = slots;
                site.Rules.DeliveryZips = zips;
                site.Rules.ClosedDates = closed;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "settings save", "order rules and contact details", true);
            response.ExecuteScript("AdminSettingsJs.saved('Settings saved.');");
            return response;
        }

        private static string Field(string id, string label, string value, string hint = "", int max = 80)
        {
            return "<label class=\"as-field\"><span>" + Ui.H(label) + "</span><input type=\"text\" id=\"as-f-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.H(value) + "\">" + (hint == string.Empty ? string.Empty : "<small class=\"as-note\">" + Ui.H(hint) + "</small>") + "<em class=\"as-err\" id=\"as-e-" + id + "\"></em></label>";
        }

        private static string TextBox(string id, string label, string value, string hint, int rows)
        {
            return "<label class=\"as-field\"><span>" + Ui.H(label) + "</span><textarea id=\"as-f-" + id + "\" rows=\"" + rows + "\" maxlength=\"500\">" + Ui.H(value) + "</textarea><small class=\"as-note\">" + Ui.H(hint) + "</small><em class=\"as-err\" id=\"as-e-" + id + "\"></em></label>";
        }

        private static string MainHtml(SiteData site, bool edit)
        {
            OrderRules r = site.Rules;
            StringBuilder sb = new StringBuilder("<div class=\"as-top\"><div><h1>Settings</h1><p>Contact details, opening times and the rules for online orders.</p></div></div>");
            sb.Append("<form onsubmit=\"return AdminSettingsJs.save()\" novalidate><fieldset class=\"as-fs\"" + (edit ? string.Empty : " disabled") + "><div class=\"as-sections\">");
            sb.Append("<div class=\"as-card\"><h2>Contact and hours</h2><div class=\"as-stack\"><div class=\"as-grid2\">" + Field("phone", "Phone", site.Phone, string.Empty, 30) + Field("email", "Email", site.Email) + "</div>");
            sb.Append(Field("address", "Address", site.Address, string.Empty, 120));
            sb.Append(TextBox("hours", "Opening hours", string.Join("\n", site.Hours.Select(h => h.Days + " | " + h.Time)), "One line per row: days | times. Shown on the site; the order rules below decide when people can order.", 4) + "</div></div>");
            sb.Append("<div class=\"as-card\"><h2>Order rules</h2><div class=\"as-stack\"><div class=\"as-grid3\">" + Field("fee", "Delivery fee ($)", r.DeliveryFee.ToString("0.00", CultureInfo.InvariantCulture), string.Empty, 7) + Field("free", "Free delivery from ($)", r.FreeDeliveryOver.ToString("0.00", CultureInfo.InvariantCulture), string.Empty, 7) + Field("min", "Minimum delivery order ($)", r.MinDelivery.ToString("0.00", CultureInfo.InvariantCulture), string.Empty, 7) + "</div>");
            sb.Append("<div class=\"as-grid3\">" + Field("lead", "Baking notice (hours)", r.LeadHours.ToString(CultureInfo.InvariantCulture), "How far ahead orders must be placed.", 2) + Field("ahead", "Order up to (days ahead)", r.DaysAhead.ToString(CultureInfo.InvariantCulture), string.Empty, 2) + Field("cap", "Orders per time window", r.SlotCapacity.ToString(CultureInfo.InvariantCulture), "Full windows can no longer be chosen.", 3) + "</div>");
            sb.Append("<div class=\"as-field\"><span>Days we take orders for</span><div class=\"as-days\">");
            for (int d = 1; d <= 7; d++)
            {
                int n = d % 7;
                sb.Append("<label class=\"as-check\"><input type=\"checkbox\" data-day=\"" + n + "\"" + (r.OpenDays.Contains(n) ? " checked" : string.Empty) + "> " + DayNames[n] + "</label>");
            }
            sb.Append("</div><em class=\"as-err\" id=\"as-e-days\"></em></div>");
            sb.Append(TextBox("slots", "Time windows", string.Join("\n", r.Slots), "One per line, like 08:00-10:00.", 4));
            sb.Append(TextBox("zips", "Delivery ZIP codes", string.Join(", ", r.DeliveryZips), "5-digit codes separated by commas.", 2));
            sb.Append(TextBox("closed", "Closed dates", string.Join("\n", r.ClosedDates), "Holidays and days off, like 2026-12-25, one per line.", 3) + "</div></div>");
            sb.Append(edit ? "<div class=\"as-savebar\"><button type=\"submit\" class=\"as-btn\" id=\"as-save\">Save settings</button></div>" : "<p class=\"as-note\">You can view settings but not change them.</p>");
            sb.Append("</div></fieldset></form>");
            return sb.ToString();
        }
    }
}
