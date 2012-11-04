using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Diagnostics;

namespace ShopNaija.ScreenScraper
{
    public class Scraper
    {
        private string rootUrlToGetDataFrom;
        private string filter;
        private string result;
        private string baseAddress = "http://www.henryjamesshoes.com";

        public Scraper(string rootUrlToGetDataFrom, string filter, string baseAddress = "http://www.henryjamesshoes.com")
        {
            this.rootUrlToGetDataFrom = rootUrlToGetDataFrom;
            this.filter = filter;
            this.baseAddress = baseAddress;
        }

        public ScrapedData Scrape()
        {
            result = GetHtmlString();
            var data = ApplyFilter();
            return data;
        }

        private ScrapedData ApplyFilter()
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);
            ScrapedData scrapedData = new ScrapedData();
            if (baseAddress == "http://www.henryjamesshoes.com")
            {
                var shoesNodes = document.DocumentNode.SelectNodes("//div[@id='Product_List']/div[@class='product_box']");
                scrapedData.Data = RecurseNode(shoesNodes);
                return scrapedData;
            }

            if (baseAddress == "http://uk.monsoon.co.uk")
            {
                // HtmlNodeCollection nodes = null;
                var monsoonNodes = document.DocumentNode.SelectNodes("//div[contains(@class,'productList_item')]");

                scrapedData.Data = RecurseMonsoonDressesNode(monsoonNodes);
                return scrapedData;
            }

            if (baseAddress == "http://www.zara.com") { }
            var zaraNodes = document.DocumentNode.SelectNodes("//li[contains(@class,'filteredItem')]");

            scrapedData.Data = RecurseZaraNode(zaraNodes);
            return scrapedData;
        }

        private IEnumerable<ProductData> RecurseZaraNode(HtmlNodeCollection nodes)
        {
            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                var img = node.SelectNodes("a/img").First().Attributes["data-src"].Value.Split(new[] { "?" }, StringSplitOptions.None)[0];
                var title = node.SelectNodes("div[@class='infoProd']/a").First().InnerText.Replace("\n", "").Replace("\t", "").Trim();

                var price = (
                        Convert.ToDouble(
                            node.SelectNodes("div[@class='infoProd']/p[@class='price']").First().InnerText
                            .Replace("\n", "")
                            .Replace("\t", "")
                            .Replace("\r", "")
                            .Replace("GBP", "")
                            .Replace("&pound;", string.Empty)
                            .Replace("£", "")
                            .Trim()
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                        ) * 1.5 * 270).ToString();

                var product = new ProductData { Image = img, Title = title, Price = price };

                DeepHarvestZaraNode(node, product);
                int count = 0;
                bool added = false;
                foreach (var p in product.Colours)
                {
                    if (product.Sizes != null && product.Sizes.Count > 0)
                    {
                        foreach (var s in product.Sizes)
                        {
                            if (count == 0)
                            {
                                added = true;
                                data.Add(product);
                                count++;
                                continue;
                            }
                            var subProduct = ProductData.Clone(product);

                            var splits = p.SelectSingleNode("a").Attributes["title"].Value.Split(new string[] { " " }, StringSplitOptions.None);
                            var l = splits[1];
                            if (splits.Length > 2)
                            {
                                l = splits[1] + " " + splits[2];
                            }

                            if (!l.Contains("not"))
                            {

                                subProduct.Option1Name = "Colour";
                                subProduct.Option1Value = l;

                                subProduct.Option2Name = "Size";
                                subProduct.Option2Value = s.InnerText == string.Empty
                                    ? s.Attributes["value"].Value.Replace("&frac12;", ".5")
                                    : s.InnerText
                                        .Replace("\r", "")
                                        .Replace("\r", "")
                                        .Replace("&nbsp;", "")
                                        .Trim();
                            }
                            else
                            {
                                subProduct.Option1Name = "Size";
                                subProduct.Option1Value = s.InnerText == string.Empty
                                    ? s.Attributes["value"].Value.Replace("&frac12;", ".5")
                                    : s.InnerText
                                        .Replace("\r", "")
                                        .Replace("\r", "")
                                        .Replace("&nbsp;", "")
                                        .Trim();

                            }
                            data.Add(subProduct);
                            added = true;
                        }
                    }
                    else
                    {
                        if (count == 0)
                        {
                            data.Add(product);
                            added = true;
                            count++;
                            continue;
                        }
                        var subProduct = ProductData.Clone(product);
                        var splits = p.SelectSingleNode("a").Attributes["title"].Value.Split(new string[] { " " }, StringSplitOptions.None);
                        var l = splits[1];
                        if (splits.Length > 2)
                        {
                            l = splits[1] + " " + splits[2];
                        }
                        if (!l.Contains("not"))
                        {
                            subProduct.Option1Name = "Colour";
                            subProduct.Option1Value = l;
                            data.Add(subProduct);
                            added = true;
                        }
                    }
                }
                if (!added)
                {
                    data.Add(product);
                }
            }
            return data;
        }

        private void DeepHarvestZaraNode(HtmlNode node, ProductData product)
        {
            var productLink = node.SelectNodes("div[@class='infoProd']/a").First().Attributes["href"].Value;

            var mainProductHtml = new HtmlDocument();
            HtmlNode doc = HtmlNode.CreateNode("");
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));
                // //div[@id="productright"]/div[@class=product_info]/p
                doc = mainProductHtml.DocumentNode;

                product.Handle = doc.SelectNodes("//div[@class='prodInfoDesc']/h2")
                    .First()
                    .InnerText
                    .Replace(" ", "-")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Replace("&amp;-", "")
                    .Trim();

                product.Body = "\"" + doc.SelectNodes("//div[@class='prodInfoDesc']/p[@class='description']").First().InnerText.Replace("\"", "'")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "") + "\"";
                product.Type = DiscernType(product.Body, product.Title);
            }
            catch
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }

            HtmlNodeCollection colours = null;
            HtmlNodeCollection sizes = null;
            var productForm = doc.SelectNodes("//div[@class='formProduct']").First();
            //Debugger.Launch();
            product.Option1Name = "Title";
            product.Option1Value = "Title";
            colours = doc.SelectNodes("//ul[contains(@class,'colorImage')]/li");
            if (colours != null)
            {
                product.Option1Name = "Colour";
                var splits = colours.First().SelectSingleNode("a").Attributes["title"].Value.Split(new[] { " " }, StringSplitOptions.None);
                var t = splits[1];
                if (t != "not")
                {
                    if (splits.Length > 2)
                    {
                        product.Option1Value = splits[1] + " " + splits[2];
                    }
                    else
                    {
                        product.Option1Value = splits[1];
                    }
                }
                else
                {
                    product.Option1Name = "Title";
                    product.Option1Value = "Title";
                }
            }

            var notAvailable = doc.SelectNodes("//div[@class='tableOptions']/table/tr");
            foreach (var check in notAvailable)
            {
                if (notAvailable != null)
                {
                    sizes = notAvailable.First().SelectNodes("//b[@class='sizeDetail1']");
                    break;
                }
            }

            if (productForm.InnerHtml.Contains("Size"))
            {
                if (sizes != null)
                {
                    if (product.Option1Name == "Title")
                    {
                        product.Option1Name = "Size";
                        product.Option1Value = sizes.First().SelectSingleNode("//b")
                            .InnerText
                                    .Replace("\r", "")
                                    .Replace("\r", "")
                                    .Replace("&nbsp;", "")
                                    .Trim();
                    }
                    else
                    {
                        product.Option2Name = "Size";
                        product.Option2Value = sizes.First().SelectSingleNode("//b")
                            .InnerText
                                    .Replace("\r", "")
                                    .Replace("\r", "")
                                    .Replace("&nbsp;", "")
                                    .Trim();
                    }
                }
            }

            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = "Zara";
            product.InventoryQuantity = "0";
            product.Tags = "Jeans denim";
            product.Sizes = sizes ?? new HtmlNodeCollection(null);
            product.Colours = colours ?? new HtmlNodeCollection(null);
        }



        private IEnumerable<ProductData> RecurseMonsoonDressesNode(HtmlNodeCollection nodes)
        {
            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
                var img = node.SelectNodes("div/a/img").First().Attributes["src"].Value;
                var title = node.SelectNodes("div[@class='productList_info']/div[@class='productList_name']/a").First().InnerText.Replace("\n", "").Replace("\t", "").Trim();

                var price = (Convert.ToDouble(
                        node.SelectNodes("div[@class='productList_info']/div[@class='productList_prices']/div[contains(@class,'price')]/a").First().InnerText.Replace("\n", "").Replace("\t", "").Trim()
                    .Replace("&pound;", string.Empty)
                    .Replace("£", "")
                    .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                        ) * 1.5 * 270).ToString();
                var product = new ProductData { Image = img, Title = title, Price = price };

                DeepHarvestDressNode(node, product);
                int count = 0;

                foreach (var p in product.Colours)
                {
                    if (product.Sizes != null && product.Sizes.Count > 0)
                    {
                        foreach (var s in product.Sizes)
                        {
                            if (count == 0)
                            {
                                data.Add(product);
                                count++;
                                continue;
                            }
                            var subProduct = ProductData.Clone(product);
                            subProduct.Option1Name = "Colour";
                            subProduct.Option1Value = p.InnerText == string.Empty ? p.Attributes["value"].Value : p.InnerText;
                            subProduct.Option2Name = "Size";
                            subProduct.Option2Value = s.InnerText == string.Empty ? s.Attributes["value"].Value.Replace("&frac12;", ".5") : s.InnerText;

                            data.Add(subProduct);
                        }
                    }
                    else
                    {
                        if (count == 0)
                        {
                            data.Add(product);
                            count++;
                            continue;
                        }
                        var subProduct = ProductData.Clone(product);
                        subProduct.Option1Name = "Colour";
                        subProduct.Option1Value = p.InnerText == string.Empty ? p.Attributes["value"].Value : p.InnerText;
                        data.Add(subProduct);
                    }
                }
            }
            return data;
        }

        private IEnumerable<ProductData> RecurseNode(HtmlNodeCollection nodes)
        {
            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
                var img = baseAddress + node.SelectNodes("a/img").First().Attributes["src"].Value;
                var title = node.SelectNodes("p[@class='title']/a").First().InnerText;
                var price = (Convert.ToDouble(
                        node.SelectNodes("p[@class='price']/a").First().InnerText
                            .Replace("&pound;", string.Empty)
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                        ) * 1.5 * 270).ToString();
                var product = new ProductData { Image = img, Title = title, Price = price };

                DeepHarvestNode(node, product);
                int count = 0;

                foreach (var p in product.Colours)
                {
                    if (product.Sizes != null && product.Sizes.Count > 0)
                    {
                        foreach (var s in product.Sizes)
                        {
                            if (count == 0)
                            {
                                data.Add(product);
                                count++;
                                continue;
                            }
                            var subProduct = ProductData.Clone(product);
                            subProduct.Option1Name = "Colour";
                            subProduct.Option1Value = p.InnerText;
                            subProduct.Option2Name = "Size";
                            subProduct.Option2Value = s.InnerText.Replace("&frac12;", ".5");

                            data.Add(subProduct);
                        }
                    }
                    else
                    {
                        if (count == 0)
                        {
                            data.Add(product);
                            count++;
                            continue;
                        }
                        var subProduct = ProductData.Clone(product);
                        subProduct.Option1Name = "Colour";
                        subProduct.Option1Value = p.InnerText;
                        data.Add(subProduct);
                    }
                }


            }

            return data;
        }

        private void DeepHarvestDressNode(HtmlNode node, ProductData product)
        {
            var productLink = baseAddress + node.SelectNodes("div[@class='productList_img']/a").First().Attributes["href"].Value;

            var mainProductHtml = new HtmlDocument();
            HtmlNode doc = HtmlNode.CreateNode("");
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));
                // //div[@id="productright"]/div[@class=product_info]/p
                doc = mainProductHtml.DocumentNode;

                product.Handle = doc.SelectNodes("//div[@class='productDetail_name_and_description']/h1")
                    .First()
                    .InnerText
                    .Replace(" ", "-")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Replace("&amp;-", "")
                    .Trim();

                product.Body = "\"" + doc.SelectNodes("//div[@class='productDescriptionText']").First().InnerText.Replace("\"", "'")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "") + "\"";
                product.Type = "Dresses"; DiscernType(product.Body, product.Title);
            }
            catch
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }

            HtmlNodeCollection colours = null;
            HtmlNodeCollection sizes = null;
            var productForm = doc.SelectNodes("//div[@class='clearBoth variant_matrix']").First();

            product.Option1Name = "Title";
            product.Option1Value = "Title";

            if (productForm.InnerHtml.Contains("Colour"))
            {
                colours = doc.SelectNodes("//select [@id=\"attributes'colour'\"]").First().SelectNodes("option");
                product.Option1Name = "Colour";
                product.Option1Value = colours.Select(x => x.Attributes["value"].Value).First();
            }

            if (productForm.InnerHtml.Contains("Size"))
            {
                sizes = doc.SelectNodes("//select [@id=\"attributes'size'\"]").First().SelectNodes("option");
                if (product.Option1Name == "Title")
                {
                    product.Option1Name = "Size";
                    product.Option1Value = sizes.Select(x => x.Attributes["value"].Value).First();
                }
                else
                {
                    product.Option2Name = "Size";
                    product.Option2Value = sizes.Select(x => x.Attributes["value"].Value).First();
                }
            }

            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = "Monsoon";
            product.InventoryQuantity = "0";
            product.Tags = "Monsoon Dress Dresses Maxi Dress Casual Style";
            product.Sizes = sizes;
            product.Colours = colours;
        }

        private void DeepHarvestNode(HtmlNode node, ProductData product)
        {
            var productLink = baseAddress + node.SelectNodes("p[@class='view_buy']/a").First().Attributes["href"].Value;

            var mainProductHtml = new HtmlDocument();
            HtmlNode doc = HtmlNode.CreateNode("");
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));
                // //div[@id="productright"]/div[@class=product_info]/p
                doc = mainProductHtml.DocumentNode;

                product.Handle = new Uri(productLink).AbsolutePath.Replace("/products/", string.Empty).Replace("/", "-");
                product.Body = "\"" + doc.SelectNodes("//div[@id='productright']/div[@class='product_info']/p").First().InnerText.Replace("\"", "'") + "\"";
                product.Type = DiscernType(product.Body, product.Title);
            }
            catch
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }

            HtmlNodeCollection colours = null;
            HtmlNodeCollection sizes = null;
            var productForm = doc.SelectNodes("//div[@id='product_form']").First();

            product.Option1Name = "Title";
            product.Option1Value = "Title";

            if (productForm.InnerHtml.Contains("Colour"))
            {
                colours = doc.SelectNodes("//div[@id='colours_js_box']").First().SelectNodes("//a[contains(@class,'product_colour')]");
                product.Option1Name = "Colour";
                product.Option1Value = colours.First().InnerText;
            }

            if (productForm.InnerHtml.Contains("Size"))
            {
                sizes = doc.SelectNodes("//div[@id='sizes_js_box']").First().SelectNodes("//a[contains(@class,'product_sizes')]");
                if (product.Option1Name == "Title")
                {
                    product.Option1Name = "Colour";
                    product.Option1Value = sizes.First().InnerText;
                }
                else
                {
                    product.Option2Name = "Size";
                    product.Option2Value = sizes.First().InnerText;
                }
            }

            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = "Henry James";
            product.InventoryQuantity = "0";
            product.Tags = "Henry James Shoes Belts Boots Loafers Leather Suede";
            product.Sizes = sizes;
            product.Colours = colours;
        }

        private string DiscernType(string body, string title)
        {
            var val = string.Empty;
            if (body.ToLower().Contains("shoe") || body.ToLower().Contains("sneaker") || body.ToLower().Contains("boot") || body.ToLower().Contains("slipper") || body.ToLower().Contains("loafer"))
            {
                val = "Shoes";
            }

            if (body.ToLower().Contains("maxi") || body.ToLower().Contains("dress"))
            {
                val = "Dresses";
            }

            if (body.ToLower().Contains("belt"))
            {
                val = "Belt";
            }
            if (title.ToLower().Contains("jean") || body.ToLower().Contains("jean"))
            {
                val = "Jeans";
            }

            return val;
        }

        private string GetHtmlString(string urlToGetDataFrom = "")
        {
            var responseHtml = "";
            var source = string.IsNullOrEmpty(urlToGetDataFrom) ? this.rootUrlToGetDataFrom : urlToGetDataFrom;
            WebRequest request = WebRequest.Create(source);

            using (var response = request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    responseHtml = sr.ReadToEnd();
                }
            }

            return responseHtml;
        }
    }
}
