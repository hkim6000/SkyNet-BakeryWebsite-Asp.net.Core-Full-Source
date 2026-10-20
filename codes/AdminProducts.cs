using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Bakery.Core;
using Bakery.Models;
using SkyNet;

namespace Bakery.codes
{
    public class AdminProducts : WebPage
    {
        private const string AreaKey = "products";
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Products | Hearthstone Bakery staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("ap-menu", await Auth.Menu("ap", "AdminProducts", c, site));
            response.SetElementContents("ap-who", Auth.Who("ap", c));
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                response.SetElementContents("ap-main", Auth.NoAccess("ap", "Products"));
                return response;
            }
            response.SetElementContents("ap-main", ShellHtml(c, site) + "<div id=\"ap-list\">" + ListHtml(c, site, string.Empty, string.Empty, 1) + "</div>");
            return response;
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Auth.Refuse(response, c, "AdminProductsJs", "view products", "list", "You do not have access to products.");
            }
            SiteData site = await Store.LoadSite();
            response.SetElementContents("ap-list", ListHtml(c, site, Ui.Clean(GetDataValue("term"), 40), Ui.Pick(GetDataValue("cat"), site.Categories.Select(x => x.Key)), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            return response;
        }

        public async Task<ApiResponse> Edit()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 60);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminProductsJs", "edit product", id, "You can look at products but not change them.");
            }
            SiteData site = await Store.LoadSite();
            Product? p = site.Products.FirstOrDefault(x => x.Key == id);
            if (id != string.Empty && p == null)
            {
                response.ExecuteScript("AdminProductsJs.toast('That product was not found.');");
                return response;
            }
            response.SetElementContents("ap-mbody", FormHtml(site, p));
            response.ExecuteScript("AdminProductsJs.openModal('" + (p == null ? "New product" : "Edit " + Ui.Js(p.Title)) + "');");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 60);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminProductsJs", "save product", id, "You can look at products but not change them.");
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                string title = Ui.Clean(GetDataValue("title"), 60);
                string category = Ui.Pick(GetDataValue("category"), site.Categories.Select(x => x.Key));
                string unit = Ui.Clean(GetDataValue("unit"), 40);
                string text = Ui.Clean(GetDataValue("text"), 200);
                string tag = Ui.Clean(GetDataValue("tag"), 20);
                string image = Ui.Pick(GetDataValue("image"), ImageNames());
                bool priceOk = Ui.Decimal(GetDataValue("price"), out decimal price) && price > 0 && price <= 999;
                string eTitle = title.Length < 2 ? "Enter a name of at least 2 letters." : string.Empty;
                string eCat = category == string.Empty ? "Choose a kind." : string.Empty;
                string ePrice = priceOk ? string.Empty : "Enter a price between 0.01 and 999.";
                string eUnit = unit.Length < 2 ? "Say how it is sold, for example loaf, 800 g." : string.Empty;
                string eText = text.Length < 5 ? "Write a short description of at least 5 characters." : string.Empty;
                string eImage = image == string.Empty ? "Choose a picture." : string.Empty;
                response.SetElementContents("ap-e-title", eTitle);
                response.SetElementContents("ap-e-cat", eCat);
                response.SetElementContents("ap-e-price", ePrice);
                response.SetElementContents("ap-e-unit", eUnit);
                response.SetElementContents("ap-e-text", eText);
                response.SetElementContents("ap-e-image", eImage);
                if (eTitle + eCat + ePrice + eUnit + eText + eImage != string.Empty)
                {
                    return response;
                }
                Product? p = site.Products.FirstOrDefault(x => x.Key == id);
                bool isNew = p == null;
                if (id != string.Empty && p == null)
                {
                    response.ExecuteScript("AdminProductsJs.saved('That product no longer exists.');");
                    return response;
                }
                if (p == null)
                {
                    p = new Product { Key = NewKey(site, title) };
                    site.Products.Add(p);
                }
                p.Title = title;
                p.Category = category;
                p.Price = Math.Round(price, 2);
                p.Unit = unit;
                p.Text = text;
                p.Tag = tag;
                p.Image = image;
                p.Available = GetDataValue("available") == "1";
                p.Featured = GetDataValue("featured") == "1";
                await Store.SaveSite(site);
                await Store.Log(c.U.Id, isNew ? "product add" : "product edit", p.Title, true);
                response.SetElementContents("ap-list", ListHtml(c, site, Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fcat"), site.Categories.Select(x => x.Key)), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
                response.ExecuteScript("AdminProductsJs.saved('" + Ui.Js(p.Title) + " was saved.');");
            }
            return response;
        }

        public async Task<ApiResponse> Toggle()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 60);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminProductsJs", "toggle product", id, "You can look at products but not change them.");
            }
            string message;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Product? p = site.Products.FirstOrDefault(x => x.Key == id);
                if (p == null)
                {
                    message = "That product was not found.";
                }
                else
                {
                    p.Available = !p.Available;
                    await Store.SaveSite(site);
                    await Store.Log(c.U.Id, p.Available ? "product on sale" : "product sold out", p.Title, true);
                    message = p.Title + (p.Available ? " is back on sale." : " is marked sold out.");
                }
                response.SetElementContents("ap-list", ListHtml(c, site, Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fcat"), site.Categories.Select(x => x.Key)), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            }
            response.ExecuteScript("AdminProductsJs.saved('" + Ui.Js(message) + "');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                return Auth.Expired(response, "AdminProductsJs");
            }
            string id = Ui.Clean(GetDataValue("id"), 60);
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Auth.Refuse(response, c, "AdminProductsJs", "delete product", id, "You can look at products but not change them.");
            }
            string message;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Product? p = site.Products.FirstOrDefault(x => x.Key == id);
                if (p == null)
                {
                    message = "That product was not found.";
                }
                else
                {
                    site.Products.Remove(p);
                    await Store.SaveSite(site);
                    await Store.Log(c.U.Id, "product delete", p.Title, true);
                    message = p.Title + " was deleted.";
                }
                response.SetElementContents("ap-list", ListHtml(c, site, Ui.Clean(GetDataValue("fterm"), 40), Ui.Pick(GetDataValue("fcat"), site.Categories.Select(x => x.Key)), Ui.Bound(GetDataValue("page"), 1, 9999, 1)));
            }
            response.ExecuteScript("AdminProductsJs.saved('" + Ui.Js(message) + "');");
            return response;
        }

        private static IEnumerable<string> ImageNames()
        {
            string dir = Path.Combine(Store.Root, "images", "products");
            if (!Directory.Exists(dir))
            {
                return Array.Empty<string>();
            }
            return Directory.GetFiles(dir, "*.webp").Select(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant()).OrderBy(n => n).ToList();
        }

        private static string NewKey(SiteData site, string title)
        {
            string slug = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            if (slug == string.Empty)
            {
                slug = "product";
            }
            string key = slug;
            int n = 2;
            while (site.Products.Any(x => x.Key == key))
            {
                key = slug + "-" + n.ToString(CultureInfo.InvariantCulture);
                n++;
            }
            return key;
        }

        private static string ShellHtml(Ctx c, SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"ap-top\"><div><h1>Products</h1><p>What is on the menu, what it costs and what is sold out today.</p></div>");
            if (Access.Can(c.U, AreaKey, 'E'))
            {
                sb.Append("<button type=\"button\" class=\"ap-btn\" onclick=\"AdminProductsJs.edit('')\">Add a product</button>");
            }
            sb.Append("</div><div class=\"ap-tools\"><input type=\"search\" id=\"ap-term\" placeholder=\"Search products\" maxlength=\"40\" aria-label=\"Search products\"><select id=\"ap-cat\" aria-label=\"Kind\" onchange=\"AdminProductsJs.changed()\"><option value=\"\">All kinds</option>");
            foreach (ProductGroup g in site.Categories)
            {
                sb.Append("<option value=\"" + Ui.H(g.Key) + "\">" + Ui.H(g.Label) + "</option>");
            }
            sb.Append("</select></div>");
            return sb.ToString();
        }

        private static string ListHtml(Ctx c, SiteData site, string term, string cat, int page)
        {
            List<Product> rows = site.Products
                .Where(p => (cat == string.Empty || p.Category == cat) && (term == string.Empty || p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"ap-tbl-wrap\"><div class=\"ap-empty\">No products match.</div></div>";
            }
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            bool edit = Access.Can(c.U, AreaKey, 'E');
            StringBuilder sb = new StringBuilder("<div class=\"ap-tbl-wrap\"><table class=\"ap-tbl\"><thead><tr><th></th><th>Product</th><th>Kind</th><th class=\"ap-num\">Price</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (Product p in rows.Skip((page - 1) * Size).Take(Size))
            {
                string kind = site.Categories.FirstOrDefault(g => g.Key == p.Category)?.Label ?? p.Category;
                sb.Append("<tr><td><img class=\"ap-thumb\" src=\"images/products/" + Ui.H(p.Image) + ".webp\" alt=\"\"></td><td><b>" + Ui.H(p.Title) + "</b><small>" + Ui.H(p.Unit) + "</small></td><td>" + Ui.H(kind) + "</td><td class=\"ap-num\">" + Ui.Money(p.Price) + "</td>");
                sb.Append("<td><div class=\"ap-flags\"><span class=\"ap-pill " + (p.Available ? "ap-s-ok" : "ap-s-off") + "\">" + (p.Available ? "On sale" : "Sold out") + "</span>" + (p.Featured ? "<span class=\"ap-pill\">Favourite</span>" : string.Empty) + "</div></td><td><div class=\"ap-acts\">");
                if (edit)
                {
                    sb.Append("<button type=\"button\" class=\"ap-btn ap-btn-ghost ap-btn-sm\" onclick=\"AdminProductsJs.edit('" + Ui.H(p.Key) + "')\">Edit</button>");
                    sb.Append("<button type=\"button\" class=\"ap-btn ap-btn-ghost ap-btn-sm\" onclick=\"AdminProductsJs.toggle('" + Ui.H(p.Key) + "', '" + Ui.H(Ui.Js(p.Title)) + "', " + (p.Available ? "true" : "false") + ")\">" + (p.Available ? "Sold out" : "Back on sale") + "</button>");
                    sb.Append("<button type=\"button\" class=\"ap-btn ap-btn-ghost ap-btn-red ap-btn-sm\" onclick=\"AdminProductsJs.remove('" + Ui.H(p.Key) + "', '" + Ui.H(Ui.Js(p.Title)) + "')\">Delete</button>");
                }
                sb.Append("</div></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"ap-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "product", "products") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"ap-btn ap-btn-ghost ap-btn-sm\"" + (page <= 1 ? " disabled" : " onclick=\"AdminProductsJs.go(" + (page - 1) + ")\"") + ">Previous</button>");
            sb.Append("<button type=\"button\" class=\"ap-btn ap-btn-ghost ap-btn-sm\"" + (page >= pages ? " disabled" : " onclick=\"AdminProductsJs.go(" + (page + 1) + ")\"") + ">Next</button></div></div>");
            return sb.ToString();
        }

        private static string FormHtml(SiteData site, Product? p)
        {
            Product v = p ?? new Product { Available = true, Image = ImageNames().FirstOrDefault() ?? string.Empty };
            StringBuilder cats = new StringBuilder("<option value=\"\">Choose a kind</option>");
            foreach (ProductGroup g in site.Categories)
            {
                cats.Append("<option value=\"" + Ui.H(g.Key) + "\"" + (g.Key == v.Category ? " selected" : string.Empty) + ">" + Ui.H(g.Label) + "</option>");
            }
            StringBuilder imgs = new StringBuilder();
            foreach (string n in ImageNames())
            {
                imgs.Append("<option value=\"" + Ui.H(n) + "\"" + (n == v.Image ? " selected" : string.Empty) + ">" + Ui.H(n) + "</option>");
            }
            StringBuilder sb = new StringBuilder("<form class=\"ap-stack\" onsubmit=\"return AdminProductsJs.save()\" novalidate>");
            sb.Append("<input type=\"hidden\" id=\"ap-f-id\" value=\"" + Ui.H(v.Key) + "\">");
            sb.Append("<div class=\"ap-grid2\"><label class=\"ap-field\"><span>Name</span><input type=\"text\" id=\"ap-f-title\" maxlength=\"60\" value=\"" + Ui.H(v.Title) + "\"><em class=\"ap-err\" id=\"ap-e-title\"></em></label>");
            sb.Append("<label class=\"ap-field\"><span>Kind</span><select id=\"ap-f-cat\">" + cats + "</select><em class=\"ap-err\" id=\"ap-e-cat\"></em></label></div>");
            sb.Append("<div class=\"ap-grid2\"><label class=\"ap-field\"><span>Price (tax included)</span><input type=\"text\" inputmode=\"decimal\" id=\"ap-f-price\" maxlength=\"7\" value=\"" + (p == null ? string.Empty : v.Price.ToString("0.00", CultureInfo.InvariantCulture)) + "\"><em class=\"ap-err\" id=\"ap-e-price\"></em></label>");
            sb.Append("<label class=\"ap-field\"><span>Sold as</span><input type=\"text\" id=\"ap-f-unit\" maxlength=\"40\" placeholder=\"loaf, 800 g\" value=\"" + Ui.H(v.Unit) + "\"><em class=\"ap-err\" id=\"ap-e-unit\"></em></label></div>");
            sb.Append("<label class=\"ap-field\"><span>Description</span><textarea id=\"ap-f-text\" maxlength=\"200\" rows=\"3\">" + Ui.H(v.Text) + "</textarea><em class=\"ap-err\" id=\"ap-e-text\"></em></label>");
            sb.Append("<div class=\"ap-previewrow\"><label class=\"ap-field\"><span>Picture (files in images/products)</span><select id=\"ap-f-image\" onchange=\"AdminProductsJs.pic(this)\">" + imgs + "</select><em class=\"ap-err\" id=\"ap-e-image\"></em></label>");
            sb.Append("<img class=\"ap-pp\" id=\"ap-pp\" src=\"images/products/" + Ui.H(v.Image) + ".webp\" alt=\"Picture preview\"></div>");
            sb.Append("<label class=\"ap-field\"><span>Label on the card (optional)</span><input type=\"text\" id=\"ap-f-tag\" maxlength=\"20\" placeholder=\"New, Seasonal\" value=\"" + Ui.H(v.Tag) + "\"></label>");
            sb.Append("<label class=\"ap-check\"><input type=\"checkbox\" id=\"ap-f-avail\"" + (v.Available ? " checked" : string.Empty) + "> On sale today</label>");
            sb.Append("<label class=\"ap-check\"><input type=\"checkbox\" id=\"ap-f-feat\"" + (v.Featured ? " checked" : string.Empty) + "> Show on the home page as a favourite</label>");
            sb.Append("<div class=\"ap-formacts\"><button type=\"button\" class=\"ap-btn ap-btn-ghost\" onclick=\"AdminProductsJs.closeModal()\">Cancel</button><button type=\"submit\" class=\"ap-btn\">Save product</button></div></form>");
            return sb.ToString();
        }
    }
}
