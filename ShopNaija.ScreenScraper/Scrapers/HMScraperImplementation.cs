using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class HMScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private const double profitRate = 1.375;
        private const double deliveryRate = 9;
        private const double cardRate = 1.02;
        private const string productType = "Womens Shoes";
        private const string vendor = "HM";

        public HMScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div/ul[@id='list-products']/li[@class = not('getTheLook')]");

            var data = new List<ProductData>();

            var titleAndHandle = new Dictionary<string, string>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
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
                    .Replace("'", " ").Split(new string[] { "£" }, StringSplitOptions.RemoveEmptyEntries)[0]
                    .Trim();

                string price = "";
                var amounts = node.SelectNodes("div/a/span/span[@class= 'price']");
                if (amounts != null)
                {
                    var priceArray = new string[] { };
                    try
                    {
                        priceArray =
                            amounts.First()
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
                    catch (Exception e)
                    {
                        Debugger.Launch();
                        var t = e;
                    }
                }

                if (Convert.ToDecimal(price) > 89.99m) continue;
                //Debugger.Launch();
                var imgSrc = node.SelectNodes("div/div[@class='image']/img").First().Attributes["src"].Value.Replace(" ", "%20");
                var image = "\"" + (imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc) + "\"";
                var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
                handle = CheckHandle(handle, titleAndHandle);
                titleAndHandle.Add(handle, title);
 
                var product = new ProductData { Handle = handle, Title = title, Price = price, Image = image };

                var images = DeepHarvestHMNode(node, product).ToList();
                if (images.First() == "skip") continue;

                data.Add(product);
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

        private IEnumerable<string> DeepHarvestHMNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            // ignoreList.Add("http://www.hm.com/gb/product/07049?article=07049-B");

            var productLink = node.SelectNodes("div/a").First().Attributes["href"].Value;
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
                var imgSrc = doc.SelectNodes("//img[@id='product-image']").First().Attributes["src"].Value.Replace(" ", "%20");
                images = new[] { imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc };

                var body = doc.SelectNodes("//div[@class='description']/p")
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
                sizeNodes = doc.SelectNodes("//ul[@id='options-variants']/li")
                               .Where(x => x.Attributes["class"].Value != "outOfStock");
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


            product.Sku = productLink;
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