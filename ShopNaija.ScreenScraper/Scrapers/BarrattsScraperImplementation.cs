using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class BarrattsScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private const double profitRate = 1.2275;
        private const double deliveryRate = 9;
        private const double cardRate = 1.02;
        private const string productType = "Womens Shoes";
        private const string vendor = "Barratts";

        public BarrattsScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            //Debugger.Launch();

            var nodes = document.DocumentNode.SelectNodes("//div[@id='productlister']/ul/li[@class='result']");

            var data = new List<ProductData>();

            var titleAndHandle = new Dictionary<string, string>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
                var title = node.SelectNodes("div[contains(@class, 'product-title')]").First()
                    .InnerText
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

                string price = "";
                var amounts = node.SelectNodes("div[@class = 'productdisplayprice']/span[@class='amount']");
                if (amounts != null)
                {
                    price = ((Convert.ToDouble(
                        amounts.First().InnerText
                            .Replace("Orig.:", "")
                            .Replace("&pound;", string.Empty).Replace("£", string.Empty).Replace("&#163;", string.Empty)
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                  ) * profitRate + deliveryRate) * cardRate).ToString("0.00");
                }
                /*else
                {
                    price = ((Convert.ToDouble(
                        node.SelectNodes("tr//font[@class='price']").First().InnerText
                            .Replace("Now:", "")
                            .Replace("&pound;", string.Empty).Replace("£", string.Empty).Replace("&#163;", string.Empty)
                            .Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
                                  ) * 1.375 + 9) * 1.02).ToString("0.00");
                }*/

                if (Convert.ToDecimal(price) > 39.99m) continue;
                //Debugger.Launch();
                var imgSrc = node.SelectNodes("div[@class='thumbnailholder']/a/img").First().Attributes["src"].Value;
                var image = imgSrc.StartsWith("/") ? BaseAddress + imgSrc : imgSrc;
                var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
                handle = CheckHandle(handle, titleAndHandle);
                titleAndHandle.Add(handle, title);
                var product = new ProductData { Handle = handle, Title = title, Price = price, Image = image };

                //Debugger.Launch();
                var images = DeepHarvestBarrettNode(node, product).ToList();
                if (images.First() == "skip") continue;

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
                        subProduct.Image = images[count - 1];
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

        private IEnumerable<string> DeepHarvestBarrettNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-parisienne-notepad-306043");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-parisienne-notepad-306043");
            ignoreList.Add("http://www.barratts.co.uk/en/floral-patterned-satchel-309455");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-cherry-notepad-306041");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-armelle-notepad-306040");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-mini-coin-purse-306032");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-shoulder-bag-306044");

            var productLink = node.SelectNodes("div[@class='thumbnailholder']/a").First().Attributes["href"].Value;
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
                var imgSrc = doc.SelectNodes("//dd[@class='productimage']/a/div/img").First().Attributes["src"].Value;
                images = new[] {imgSrc.StartsWith("/") ? BaseAddress + imgSrc : imgSrc};

                var body = doc.SelectNodes("//dd/h1")
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

            /*var xsmall = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 0)
            {
                InnerHtml = "3"
            };
            var small = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 1)
            {
                InnerHtml = "4"
            };
            var medium = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 2)
            {
                InnerHtml = "5"
            };
            var large = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 3)
            {
                InnerHtml = "6"
            };
            var xlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 4)
            {
                InnerHtml = "7"
            };
            var xxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 5)
            {
                InnerHtml = "8"
            };
            var xxxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 6)
            {
                InnerHtml = "9"
            };
            var xxxxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 7)
            {
                InnerHtml = "40M"
            };
            var xxxxxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 8)
            {
                InnerHtml = "40L"
            };
            var xl = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 9)
            {
                InnerHtml = "42S"
            };
            var xxl = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 10)
            {
                InnerHtml = "42M"
            };
            var xxxl = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 11)
            {
                InnerHtml = "42L"
            };
            var xl1 = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 12)
            {
                InnerHtml = "44S"
            };
            var xxl1 = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 13)
            {
                InnerHtml = "44M"
            };
            var xxxl1 = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 14)
            {
                InnerHtml = "44L"
            };*/

            // var sizes = new HtmlNode[] { xsmall, small, medium, large, xlarge, xxlarge, xxxlarge, xxxxlarge, xl, /*xxl, xxxl, xl1, xxl1, xxxl1 */};
            var sizes = new List<HtmlNode>();
            IEnumerable<HtmlNode> sizeNodes = null;
            try
            {
                sizeNodes = doc.SelectNodes("//div[@id='sizeSelectorThumbs']/ul/li")
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
                        InnerHtml = innerText
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