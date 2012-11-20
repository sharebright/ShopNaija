using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class MonsoonScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        public MonsoonScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div[contains(@class,'productList_item')]");
            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
                var title = node.SelectNodes("div[@class='productList_info']/div[@class='productList_name']/a")
					.First()
					.InnerText
					.Replace("\n", "")
					.Replace("\t", "")
					.Trim();

                var price = ((Convert.ToDouble(
                    node.SelectNodes("div[@class='productList_info']/div[@class='productList_prices']/div[contains(@class,'price')]/a")
						.First()
						.InnerText.Replace("\n", "")
						.Replace("\t", "")
						.Trim()
                        .Replace("&pound;", string.Empty)
                        .Replace("£", "")
                        .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                ) * 1.125 + 4) * 1.02).ToString("0.00");
                
				var product = new ProductData { Title = title, Price = price };

                DeepHarvestMonsoonNode(node, product);
                var count = 0;

                foreach (var p in product.Colours)
                {
                    if (product.Sizes != null && product.Sizes.Any())
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
                            // Debugger.Launch();
                            if (!s.NextSibling.InnerText.Contains("Out of Stock"))
                            {
                                subProduct.Option2Name = "Size";
                                if (s.Attributes["value"].Value == string.Empty) continue;
                                subProduct.Option2Value = s.Attributes["value"].Value
                                                                .Replace(" Shoe", "")
                                                                .Replace("&frac12;", ".5");
                            }
                            else
                            {
                                continue;
                            }
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

        private void DeepHarvestMonsoonNode(HtmlNode node, ProductData product)
        {
            var productLink = BaseAddress + node.SelectNodes("div[@class='productList_img']/a").First().Attributes["href"].Value;
            var mainProductHtml = new HtmlDocument();
            var doc = HtmlNode.CreateNode("");
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));
                // //div[@id="productright"]/div[@class=product_info]/p
                doc = mainProductHtml.DocumentNode;
                product.Image = doc.SelectNodes("//a[contains(@class,'MagicZoom')]").First().Attributes["href"].Value;

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
                product.Type = "Womens Accessories Bracelets"; DiscernType(product.Body, product.Title);
            }
            catch
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }

            HtmlNodeCollection colours = null;
            IEnumerable<HtmlNode> sizes = null;
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
                //Debugger.Launch();
                HtmlNode htmlNode = doc.SelectNodes("//select[@id=\"attributes'size'\"]").First();
                //int i = 0;
                //while (htmlNode.SelectNodes("option").Skip(i).First().NextSibling.InnerText.Contains("Out"))
                //{
                //    i++;
                //    htmlNode = doc.SelectNodes("//select[@id=\"attributes'size'\"]").Skip(i).First();
                //}
                sizes = htmlNode
                    .SelectNodes("option")
                    .Where(x => !x.NextSibling.InnerText.Contains("Out"))
                    .ToList();
                if (product.Option1Name == "Title")
                {
                    product.Option1Name = "Size";
                    product.Option1Value = sizes.Select(x => x.Attributes["value"].Value)
                        .First()
                        .Replace(" Shoe", "");
                }
                else
                {
                    if (sizes.Count() > 0)
                    {
                        product.Option2Name = "Size";
                        product.Option2Value = sizes.Select(x => x.Attributes["value"].Value)
                            .First()
                            .Replace(" Shoe", "");
                    }
                }
            }

            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = "Accessorize";
            product.InventoryQuantity = "0";
            product.Tags = "Women Accessories Bracelets";
            product.Sizes = sizes;
            product.Colours = colours;
        }
    }
}