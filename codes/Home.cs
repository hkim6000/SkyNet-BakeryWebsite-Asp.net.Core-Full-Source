using System.Globalization;
using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Home : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Hearthstone Bakery | Fresh bread and pastries, ordered online");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Slow-fermented bread, pastries and cakes. Order online for collection or delivery.");

            SiteData site = await Store.LoadSite();
            StringBuilder cats = new StringBuilder();
            foreach (ProductGroup g in site.Categories)
            {
                int n = site.Products.Count(x => x.Category == g.Key);
                cats.Append("<a class=\"hm-cat\" href=\"Menu?c=" + g.Key + "\"><b>" + HtmlEncode(g.Label) + "</b><small>" + HtmlEncode(g.Note) + "</small><span>" + Ui.Count(n, "item", "items") + "</span></a>");
            }
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText
                .Replace("{plhd_tagline}", HtmlEncode(site.Tagline))
                .Replace("{plhd_next}", HtmlEncode(NextLine(site.Rules)))
                .Replace("{plhd_fav}", Cards(site.Products.Where(x => x.Featured && x.Available).Take(4)))
                .Replace("{plhd_cats}", cats.ToString())
                .Replace("{plhd_address}", HtmlEncode(site.Address))
                .Replace("{plhd_hours}", HoursHtml(site, true)), site);
        }

        private static string NextLine(OrderRules rules)
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i <= 14; i++)
            {
                DateTime day = now.Date.AddDays(i);
                if (!rules.OpenDays.Contains((int)day.DayOfWeek) || rules.ClosedDates.Contains(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
                {
                    continue;
                }
                if (rules.Slots.Any(s => SlotStart(day, s) >= now.AddHours(rules.LeadHours)))
                {
                    string when = i == 0 ? "today" : i == 1 ? "tomorrow" : day.ToString("dddd", CultureInfo.InvariantCulture);
                    return "Next available: " + when;
                }
            }
            return "Fresh from the oven";
        }

        private static DateTime SlotStart(DateTime day, string slot)
        {
            string start = slot.Split('-')[0];
            return TimeSpan.TryParse(start, CultureInfo.InvariantCulture, out TimeSpan t) ? day.Date + t : day.Date.AddDays(1);
        }

        private static string Cards(IEnumerable<Product> items)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Product i in items)
            {
                sb.Append("<article class=\"hm-card\"><a class=\"hm-card-img\" href=\"Menu?c=" + i.Category + "\"><img src=\"images/products/" + i.Image + ".webp\" alt=\"" + HtmlEncode(i.Title) + "\" loading=\"lazy\">" + (i.Tag != string.Empty ? "<i class=\"hm-tag\">" + HtmlEncode(i.Tag) + "</i>" : string.Empty) + "</a>");
                sb.Append("<div class=\"hm-card-b\"><b>" + HtmlEncode(i.Title) + "</b><small>" + HtmlEncode(i.Unit) + "</small><p>" + HtmlEncode(i.Text) + "</p><div class=\"hm-card-f\"><em class=\"hm-price\">" + Ui.Money(i.Price) + "</em><a class=\"hm-btn hm-btn-ghost\" href=\"Menu?c=" + i.Category + "\">Order</a></div></div></article>");
            }
            return sb.Length == 0 ? "<p>Check back soon for today&rsquo;s bakes.</p>" : sb.ToString();
        }

        private static string HoursHtml(SiteData site, bool bold)
        {
            StringBuilder sb = new StringBuilder();
            foreach (HourLine h in site.Hours)
            {
                sb.Append("<span>" + (bold ? "<b>" + HtmlEncode(h.Days) + "</b>: " : HtmlEncode(h.Days) + ": ") + HtmlEncode(h.Time) + "</span>");
            }
            return sb.ToString();
        }

        private static string WithFooter(string html, SiteData site)
        {
            return html
                .Replace("{plhd_f_tag}", HtmlEncode(site.Tagline))
                .Replace("{plhd_f_address}", HtmlEncode(site.Address))
                .Replace("{plhd_f_phone}", HtmlEncode(site.Phone) + " &middot; " + HtmlEncode(site.Email))
                .Replace("{plhd_f_hours}", HoursHtml(site, false));
        }
    }
}
