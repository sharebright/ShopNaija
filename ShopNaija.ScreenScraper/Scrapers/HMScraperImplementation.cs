using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;
using ShopifyHandle;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class HmScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private readonly double profitRate = 1.215;
        private readonly double deliveryRate = 4.5;
        private readonly double cardRate = 1.031;
        private const string ProductType = "Womens Trousers";
        private const string Vendor = "HM";

        public HmScraperImplementation(string rootUrlToGetDataFrom, string baseAddress, double profitRate, double deliveryRate, double cardRate)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
            this.profitRate = profitRate;
            this.deliveryRate = deliveryRate;
            this.cardRate = cardRate;
        }

        public HmScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div/ul[@id='list-products']/li[@class = not('getTheLook')]");

            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                var title = node.SelectNodes("div/a/span[@class= 'details']").First()
                    .InnerText
                    .Replace("&eacute;", "e")
                    .Replace("&acute;", "e")
                    .Replace("w/", "with")
                    .Replace("&reg;", "")
                    .Replace("From", "")
                    .Replace("&amp;", "and")
                    .Replace("&trade;", "")
                    .Replace("&", "and")
                    .Replace("3/4", "3-quarter")
                    .Replace("\t", " ")
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("/t", " ")
                    .Replace("'", " ").Split(new[] { "£" }, StringSplitOptions.RemoveEmptyEntries)[0]
                    .Trim();

                var price = "";
                var amounts = node.SelectNodes("div/a/span/span[@class= 'price']");
                if (amounts != null)
                {
                    try
                    {
                        var priceArray = amounts.First()
                            .InnerText
                            .Replace("Orig.:", "")
                            .Replace("From ", "")
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Replace("&pound;", string.Empty)
                            .Replace("&#163;", string.Empty)
                            .Split(new[] { "£" }, StringSplitOptions.RemoveEmptyEntries);

                        price = ((
                            (Convert.ToDouble((priceArray[1]).Trim()) * profitRate + deliveryRate) * cardRate)
                            .ToString("0.00"));
                    }
                    catch (Exception)
                    {
                        Debugger.Launch();
                    }
                }

                if (Convert.ToDecimal(price) > 89.99m) continue;
                var imgSrc = node.SelectNodes("div/div[@class='image']/img").First().Attributes["src"].Value.Replace(" ", "%20");
                var image = "\"" + (imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc) + "\"";
 
                var product = new ProductData { Title = title, Price = price, Image = image };

                var images = DeepHarvestHmNode(node, product).ToList();
                if (images.First() == "skip") continue;

                var count = 0;
                foreach (var size in product.Sizes)
                {
                    if (count == 0)
                    {
                        data.Add(product);
                        count++;
                        continue;
                    }
                    var subProduct = ProductData.Clone(product);
                    subProduct.Option1Name = "Size";
                    subProduct.Option1Value = size.InnerText;
                    if (images.Count >= count)
                    {
                        subProduct.Image = "\"" + images[count - 1] + "\"";
                    }
                    count++;
                    data.Add(subProduct);
                }
            }
            return data;
        }

        private IEnumerable<string> DeepHarvestHmNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            var productLink = node.SelectNodes("div/a").First().Attributes["href"].Value;

            if (ignoreList.Contains(productLink))
            {
                return new[] { "skip" };
            }

            var mainProductHtml = new HtmlDocument();
            var doc = HtmlNode.CreateNode("");
            IEnumerable<string> images = new string[0];

            ParseMainProductNodes(product, mainProductHtml, productLink, ref doc, ref images);
            
            var sizes = new List<HtmlNode>();

            if (TryGenerateStockSizesWithoutFailure(doc, productLink, sizes, mainProductHtml)) return new[] { "skip" };

            if (!sizes.Any()) return new[] { "skip" };

            FinaliseProductAttributes(product, sizes, productLink);

            return images;
        }

        private void ParseMainProductNodes(ProductData product, HtmlDocument mainProductHtml, string productLink, ref HtmlNode doc, ref IEnumerable<string> images)
        {
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));

                doc = mainProductHtml.DocumentNode;

                images = new[] {GetImageSrc(doc)};

                product.Body = GetProductBody(doc);

                product.Type = ProductType;
            }
            catch (Exception)
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }
        }

        private static void FinaliseProductAttributes(ProductData product, List<HtmlNode> sizes, string productLink)
        {
            product.Option1Name = "Size";
            product.Option1Value = sizes.First().InnerText;
            product.Handle = "\"" + HandleManager.Encrypt(productLink) + "\"";
            product.Sku = productLink;
            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = Vendor;
            product.InventoryQuantity = "0";
            product.Tags = ProductType;
            product.Sizes = sizes;
        }

        private static bool TryGenerateStockSizesWithoutFailure(HtmlNode doc, string productLink, List<HtmlNode> sizes, HtmlDocument mainProductHtml)
        {
            IEnumerable<HtmlNode> sizeNodes;

            try
            {
                sizeNodes = GetInStockSizeNodes(doc);
            }
            catch
            {
                Console.WriteLine("Failed to get sizes for node: {0}", productLink);
                return true;
            }

            GenerateSizes(sizeNodes, sizes, mainProductHtml);
            return false;
        }

        private static string GetImageSrc(HtmlNode doc)
        {
            var imgSrc = doc.SelectNodes("//img[@id='product-image']").First().Attributes["src"].Value.Replace(" ", "%20");

            return imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc;
        }

        private static string GetProductBody(HtmlNode doc)
        {
            return "\"" + doc.SelectNodes("//div[@class='description']/p")
                      .First()
                      .InnerText
                      .Replace("\"", "'")
                      .Replace("- US size - refer to size chart for conversion", "")
                      .Replace("See Return Policy", "")
                      .Replace("\t", " ")
                      .Replace("/t", " ")
                      .Replace("&trade;", "")
                      .Replace("&amp;", "and")
                      .Replace("&", "and")
                      .Replace("&nbsp;", " ")
                      .Replace("&eacute", "e")
                      .Replace("&acute", "e")
                      .Trim() + "\"";
        }

        private static void GenerateSizes(IEnumerable<HtmlNode> sizeNodes, List<HtmlNode> sizes, HtmlDocument mainProductHtml)
        {
            foreach (var sizeNode in sizeNodes)
            {
                var count = sizes.Count;
                var innerText = sizeNode
                    .InnerText
                    .Replace("\t", string.Empty)
                    .Replace("/r", string.Empty)
                    .Replace("/n", string.Empty)
                    .Trim();

                var htmlNode = new HtmlNode(HtmlNodeType.Element, mainProductHtml, count)
                {
                    InnerHtml = "\"" + innerText + "\""
                };
                sizes.Add(htmlNode);
            }
        }

        private static IEnumerable<HtmlNode> GetInStockSizeNodes(HtmlNode doc)
        {
            return doc.SelectNodes("//ul[@id='options-variants']/li")
                      .Where(x => x.Attributes["class"].Value != "outOfStock");
        }
    }
}