using System.Text;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class Contact : WebPage
    {
        private static readonly string[] Topics = { "order", "large", "feedback", "other" };

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Contact | Hearthstone Bakery");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Contact Hearthstone Bakery about an order, a large cake or anything else.");

            SiteData site = await Store.LoadSite();
            StringBuilder faq = new StringBuilder();
            foreach (Faq f in site.Faqs)
            {
                faq.Append("<details class=\"ct-faq\"><summary>" + HtmlEncode(f.Q) + "</summary><p>" + HtmlEncode(f.A) + "</p></details>");
            }
            string tel = new string(site.Phone.Where(ch => char.IsDigit(ch) || ch == '+').ToArray());
            HtmlDoc.HtmlBodyText = WithFooter(HtmlDoc.HtmlBodyText
                .Replace("{plhd_address}", HtmlEncode(site.Address))
                .Replace("{plhd_tel}", HtmlEncode(tel))
                .Replace("{plhd_phone}", HtmlEncode(site.Phone))
                .Replace("{plhd_mail}", HtmlEncode(site.Email))
                .Replace("{plhd_hours}", HoursHtml(site))
                .Replace("{plhd_faq}", faq.ToString()), site);
        }

        public async Task<ApiResponse> Send()
        {
            ApiResponse response = new ApiResponse();
            string name = Ui.Clean(GetDataValue("name"), 60);
            string email = Ui.Clean(GetDataValue("mail"), 80);
            string topic = Ui.Pick(GetDataValue("topic"), Topics);
            string text = Ui.Clean(GetDataValue("text"), 1001);
            string eName = name.Length < 2 ? "Please enter your name." : string.Empty;
            string eEmail = !Ui.IsEmail(email) ? "Please enter a valid email address." : string.Empty;
            string eTopic = topic == string.Empty ? "Please choose a topic." : string.Empty;
            string eMsg = text.Length < 10 ? "Please write at least 10 characters." : text.Length > 1000 ? "Please keep it under 1,000 characters." : string.Empty;
            response.SetElementContents("ct-e-name", eName);
            response.SetElementContents("ct-e-email", eEmail);
            response.SetElementContents("ct-e-topic", eTopic);
            response.SetElementContents("ct-e-msg", eMsg);
            if (eName + eEmail + eTopic + eMsg != string.Empty)
            {
                response.SetElementContents("ct-sent", string.Empty);
                return response;
            }
            using (await Store.Hold())
            {
                List<Enquiry> list = await Store.LoadMessages();
                list.Add(new Enquiry { Id = "m-" + Guid.NewGuid().ToString("N").Substring(0, 8), Name = name, Email = email, Topic = topic, Text = text, When = DateTime.UtcNow });
                await Store.SaveMessages(list);
            }
            response.SetElementContents("ct-sent", "<b>Thank you, " + HtmlEncode(name) + ".</b> We have your message and will reply by email.");
            response.ExecuteScript("ContactJs.sent();");
            return response;
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
