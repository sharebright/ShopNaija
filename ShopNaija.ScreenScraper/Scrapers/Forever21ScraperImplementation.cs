using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using ShopifyHandle;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class Forever21ScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private readonly double profitRate = 1.3;
        private readonly double deliveryRate = 0;
        private readonly double cardRate = 1.031;
        private const string ProductType = "Womens Dresses";
        private const string Vendor = "Forever21";

        public Forever21ScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public Forever21ScraperImplementation(string rootUrlToGetDataFrom, string baseAddress, double profitRate, double deliveryRate, double cardRate)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
            this.profitRate = profitRate;
            this.deliveryRate = deliveryRate;
            this.cardRate = cardRate;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//table[@class='dlCategoryList']/tr/td/table");

            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
                var title = node.SelectNodes("tr//div[@class='DisplayName']").First().InnerText
                    .Replace("&eacute;", "e")
                    .Replace("&acute;", "e")
                    .Replace("w/", "with")
                    .Replace("&reg;", "")
                    .Replace("&amp;", "and")
                    .Replace("&trade;", "")
                    .Replace("&", "and")
                    .Replace("3/4", "3-quarter")
                    .Replace("\t", " ")
                    .Replace("/t", " ")
                    .Replace("/t", " ")
                    .Replace("'", " ")
                    .Trim();

                var price = "";
                var amounts = node.SelectNodes("tr//font[@class='price']");
                //Console.ReadKey();
                if (amounts != null)
                {
                    price = ((Convert.ToDouble(
                        amounts.First().InnerText
                            .Replace("Orig.:", "")
                            .Replace("&pound;", string.Empty).Replace("£", string.Empty)
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                  ) * profitRate + deliveryRate) * cardRate).ToString("0.00");
                }
                else
                {
                    price = ((Convert.ToDouble(
                        amounts.First().InnerText
                            .Replace("Now:", "")
                            .Replace("&pound;", string.Empty).Replace("£", string.Empty)
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                  ) * profitRate + deliveryRate) * cardRate).ToString("0.00");
                }

                if (Convert.ToDecimal(price) > 89.99m) continue;
                var imgSrc = node.SelectNodes("tr/td/div/a/img").First().Attributes["src"].Value.Replace(" ", "%20");
                var image = "\"" + (imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc) + "\"";

                var product = new ProductData { Title = title, Price = price, Image = image };

                var images = DeepHarvestForever21Node(node, product).ToList();
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

        private IEnumerable<string> DeepHarvestForever21Node(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            var productLink = node.SelectNodes("tr/td/div/a").First().Attributes["href"].Value;
            
            if (ignoreList.Contains(productLink))
            {
                return new[] { "skip" };
            }

            var mainProductHtml = new HtmlDocument();
            var doc = HtmlNode.CreateNode("");
            IEnumerable<string> images = new string[0];
            
            ParseMainProductNodes(product, mainProductHtml, productLink, ref doc, ref images);

            var sizes = new List<HtmlNode>();

            if (TryGenerateStockSizesWithoutFailure(doc, productLink, ref sizes, mainProductHtml)) return new[] { "skip" };

            if (!sizes.Any()) return new[] { "skip" };

            FinaliseProductAttributes(product, sizes, productLink);

            return images;
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

        private static bool TryGenerateStockSizesWithoutFailure(HtmlNode doc, string productLink, ref List<HtmlNode> sizes, HtmlDocument mainProductHtml)
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

            GenerateSizes(sizeNodes, ref sizes, mainProductHtml);
            return false;
        }

        private static IEnumerable<HtmlNode> GetInStockSizeNodes(HtmlNode doc)
        {
            return doc.SelectNodes("//select[@id='ctl00_MainContent_ddlSize']/option");
        }

        private static void GenerateSizes(IEnumerable<HtmlNode> sizeNodes, ref List<HtmlNode> sizes, HtmlDocument mainProductHtml)
        {
            var temp = new List<HtmlNode>();
            foreach (var sizeNode in sizeNodes)
            {
                var count = temp.Count;
                var innerText = sizeNode.NextSibling
                    .InnerText
                    .Replace("\t", string.Empty)
                    .Replace("/r", string.Empty)
                    .Replace("/n", string.Empty)
                    .Trim();
                if (innerText == "Size  (US*)") continue;
                var htmlNode = new HtmlNode(HtmlNodeType.Element, mainProductHtml, count)
                {
                    InnerHtml = "\"" + innerText + "\""
                };
                temp.Add(htmlNode);
            }
            sizes = temp; //.ToList();
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

        private static string GetProductBody(HtmlNode doc)
        {
            try
            {
                var body = doc.SelectNodes("//td[@class='productdesc']")
                              .First()
                              .InnerText
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
                              .Replace(",", "")
                              .Replace(".  ", ". ")
                              .Replace("\r", string.Empty)
                              .Replace("\n", string.Empty)
                              .Trim(new[] { ' ' }).TrimStart(new[] { ' ' });

                var indexOf = body.IndexOf("Product Code :", StringComparison.Ordinal);
                body = body.Substring(0, indexOf);
                return @"" + body.Replace("\"", "&quot;").Replace("”", "&quot;") + "";
            }
            catch
            {
                var body = doc.SelectNodes("//td[@class='productdesc']")
                              .First()
                              .InnerText
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
                              .Trim();

                var indexOf = body.IndexOf("Product Code :", StringComparison.Ordinal);

                body = body.Substring(0, indexOf);

                return @"" + body.Replace("\"", "&quot;").Replace("”", "&quot;") + "";
            }
        }

        private static string GetImageSrc(HtmlNode doc)
        {
            return doc.SelectNodes("//img[@class='ItemImage']").First().Attributes["src"].Value;
        }
    }
}