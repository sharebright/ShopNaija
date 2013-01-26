using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
    class MatalanScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private const double profitRate = 1.225;
        private const double deliveryRate = 3;
        private const double cardRate = 1.02;
        private const string productType = "Womens Shoes";
        private const string vendor = "Matalan";

        public MatalanScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div[contains(@class,'productCont')]");

            var data = new List<ProductData>();

            var titleAndHandle = new Dictionary<string, string>();

            foreach (var node in nodes)
            {
                var img = node.SelectNodes("div[contains(@class,'productImageCont')]/a/img[starts-with(@id, 'product_')]")
                              .First()
                              .Attributes["src"]
                              .Value
                              .TrimStart(new[] { '.' })
                              .Split(new[] { "?" }, StringSplitOptions.None)[0];

                var title =
                    node.SelectNodes("div[@class='productTitle']/h2/a")
                    .First()
                    .InnerText
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Replace("É", "E")
                    .Replace("É".ToLower(), "e")
                    .Trim();

                var price = (
                                (Convert.ToDouble(
                                    node.SelectNodes("div[@class='productPrice']/p/span").First().InnerText
                                        .Replace("\n", "")
                                        .Replace("\t", "")
                                        .Replace("\r", "")
                                        .Replace("GBP", "")
                                        .Replace("&pound;", string.Empty)
                                        .Replace("from", string.Empty)
                                        .Replace("From", string.Empty)
                                        .Replace("now", string.Empty)
                                        .Replace("&nbsp;", string.Empty)
                                        .Replace("£", "")
                                        .Trim()
                                        .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                ) * profitRate + deliveryRate) * cardRate).ToString("0.00");

                var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
                handle = CheckHandle(handle, titleAndHandle);
                titleAndHandle.Add(handle, title);


                var product = new ProductData { Image = img, Title = title, Price = price, Handle = handle };

                var images = DeepHarvestMatalanNode(node, product).ToList();
                if (images.First() == "skip") continue;

                //data.Add(product);
                var count = 0;
                foreach (var size in product.Sizes)
                {
                    if (count == 0)
                    {
                        //if (data.Contains())
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

        private static string CheckHandle(string handle, IDictionary<string, string> titleAndHandle, int count = 0)
        {
            count++;
            var newHandle = handle;
            if (titleAndHandle.ContainsKey(handle))
            {
                newHandle = CheckHandle(string.Format(handle + "-{0}", count), titleAndHandle, count);
            }
            return newHandle;
        }

        private IEnumerable<string> DeepHarvestMatalanNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            // ignoreList.Add("http://www.hm.com/gb/product/07049?article=07049-B");

            var productLink = node.SelectNodes("div/a").First().Attributes["href"].Value.Replace("&amp;", "&");
            if (ignoreList.Contains(productLink))
            {
                return new[] { "skip" };
            }

            var mainProductHtml = new HtmlDocument();
            var doc = HtmlNode.CreateNode("");
            IEnumerable<string> images = new string[0];
            try
            {
                mainProductHtml.LoadHtml(GetHtmlString(productLink));
                // //div[@id="productright"]/div[@class=product_info]/p
                doc = mainProductHtml.DocumentNode;
                var imgSrc = doc.SelectNodes("//img[@id='mainProductImage']").First().Attributes["src"].Value.Replace(" ", "%20");
                images = new[] { imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc };

                var htmlNode = doc.SelectNodes("//div[contains(@class,'tab_content')]").First().SelectNodes("//div[contains(@class, 'info')]").First().SelectNodes("//ul/li/p").First();
                var body = htmlNode
                        .InnerText
                        .Replace("\"", "'")
                        .Replace("Free Delivery with All Suits Over £50!", "'")
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

                product.Body = "\"" + body + "\"";

                product.Type = productType;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
            }

            var sizes = new List<HtmlNode>();
            IEnumerable<HtmlNode> sizeNodes = null;
            try
            {
                sizeNodes = doc.SelectNodes("//ul[@id='alternativeSizes']/li/label")
                               .Where(x => x.ParentNode.Attributes["class"].Value == "in_stock");
            }
            catch
            {
                Console.WriteLine("Failed to get sizes for node: {0}", productLink);
                return new[] { "skip" };
            }
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

            if (!sizes.Any()) return new[] { "skip" };

            product.Option1Name = "Size";
            product.Option1Value = sizes.First().InnerText;


            product.Sku = "\"" + productLink + "\"";
            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = vendor;
            product.InventoryQuantity = "0";
            product.Tags = productType;
            product.Sizes = sizes;

            return images;
        }
    }
}