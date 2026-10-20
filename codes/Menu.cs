using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Menu : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Menu | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Bread, pastries, cakes and cookies from Hearthstone Bakery. Add to your basket and order online.");

            SiteData site = await Store.LoadSite();
            string cat = Ui.Pick(QueryValue("c"), site.Categories.Select(c => c.Key));
            string term = Ui.Clean(QueryValue("s"), 40);
            List<Product> found = Find(site, cat, term);
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText
                .Replace("{plhd_cat}", cat)
                .Replace("{plhd_chips}", ChipsHtml(site, cat))
                .Replace("{plhd_term}", HtmlEncode(term))
                .Replace("{plhd_countline}", CountLine(found.Count, cat, term, site))
                .Replace("{plhd_grid}", GridHtml(found, term)), site);
        }

        public async Task<ApiResponse> Filter()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            string cat = Ui.Pick(GetDataValue("cat"), site.Categories.Select(c => c.Key));
            string term = Ui.Clean(GetDataValue("term"), 40);
            List<Product> found = Find(site, cat, term);
            response.SetElementContents("mn-chips", ChipsHtml(site, cat));
            response.SetElementContents("mn-grid", GridHtml(found, term));
            response.SetElementContents("mn-countline", CountLine(found.Count, cat, term, site));
            response.ExecuteScript("MenuJs.filtered('" + cat + "', '" + Ui.Js(term) + "');");
            return response;
        }

        private static List<Product> Find(SiteData site, string cat, string term)
        {
            return site.Products
                .Where(p => (cat == string.Empty || p.Category == cat)
                    && (term == string.Empty || p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static string ChipsHtml(SiteData site, string cat)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<button type=\"button\" class=\"mn-chip" + (cat == string.Empty ? " mn-act" : string.Empty) + "\" onclick=\"MenuJs.filter('')\">All</button>");
            foreach (ProductGroup g in site.Categories)
            {
                sb.Append("<button type=\"button\" class=\"mn-chip" + (cat == g.Key ? " mn-act" : string.Empty) + "\" onclick=\"MenuJs.filter('" + g.Key + "')\">" + HtmlEncode(g.Label) + "</button>");
            }
            return sb.ToString();
        }

        private static string CountLine(int n, string cat, string term, SiteData site)
        {
            string label = site.Categories.FirstOrDefault(c => c.Key == cat)?.Label ?? "all kinds";
            string text = Ui.Count(n, "item", "items") + " &middot; " + HtmlEncode(label);
            return term == string.Empty ? text : text + " &middot; matching &ldquo;" + HtmlEncode(term) + "&rdquo;";
        }

        private static string GridHtml(List<Product> items, string term)
        {
            if (items.Count == 0)
            {
                return "<div class=\"mn-empty\">Nothing on the menu matches" + (term == string.Empty ? string.Empty : " &ldquo;" + HtmlEncode(term) + "&rdquo;") + ". Try another word or kind.</div>";
            }
            StringBuilder sb = new StringBuilder();
            foreach (Product i in items)
            {
                string action = i.Available
                    ? "<button type=\"button\" class=\"mn-btn mn-add\" data-key=\"" + HtmlEncode(i.Key) + "\" data-title=\"" + HtmlEncode(i.Title) + "\" onclick=\"MenuJs.add(this)\">Add</button>"
                    : "<span class=\"mn-sold\">Sold out today</span>";
                sb.Append("<article class=\"mn-card\"><span class=\"mn-card-img\"><img src=\"images/products/" + HtmlEncode(i.Image) + ".webp\" alt=\"" + HtmlEncode(i.Title) + "\" loading=\"lazy\">" + (i.Tag != string.Empty ? "<i class=\"mn-tag\">" + HtmlEncode(i.Tag) + "</i>" : string.Empty) + "</span>");
                sb.Append("<div class=\"mn-card-b\"><b>" + HtmlEncode(i.Title) + "</b><small>" + HtmlEncode(i.Unit) + "</small><p>" + HtmlEncode(i.Text) + "</p><div class=\"mn-card-f\"><em class=\"mn-price\">" + Ui.Money(i.Price) + "</em>" + action + "</div></div></article>");
            }
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
