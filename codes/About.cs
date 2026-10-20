using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class About : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Our story | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Meet the bakers behind Hearthstone Bakery and how we make our bread.");

            SiteData site = await Store.LoadSite();
            StringBuilder team = new StringBuilder();
            foreach (TeamMember m in site.Team)
            {
                team.Append("<article class=\"ab-person\"><img src=\"images/team/" + HtmlEncode(m.Image) + ".webp\" alt=\"Portrait of " + HtmlEncode(m.Name) + "\" loading=\"lazy\"><div><b>" + HtmlEncode(m.Name) + "</b><small>" + HtmlEncode(m.Role) + "</small><p>" + HtmlEncode(m.Text) + "</p></div></article>");
            }
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText.Replace("{plhd_team}", team.ToString()), site);
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
