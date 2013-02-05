using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using ShopifyHandle;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class BarrattsScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
        private readonly double profitRate = 1.1465;
        private readonly double deliveryRate = 9;
        private readonly double cardRate = 1.031;
        private const string productType = "Womens Shoes";
        private const string vendor = "Barratts";

        public BarrattsScraperImplementation(string rootUrlToGetDataFrom, string baseAddress, double profitRate, double deliveryRate, double cardRate)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
            this.profitRate = profitRate;
            this.deliveryRate = deliveryRate;
            this.cardRate = cardRate;
        }

        public BarrattsScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            var nodes = document.DocumentNode.SelectNodes("//div[@id='productlister']/ul/li[@class='result']");

            var data = new List<ProductData>();

            foreach (var node in nodes)
            {
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

                var price = "";
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

                if (Convert.ToDecimal(price) > 149.99m) continue;

                var imgSrc = node.SelectNodes("div[@class='thumbnailholder']/a/img").First().Attributes["src"].Value;
                var image = "\"" + (imgSrc.StartsWith("/") ? BaseAddress + imgSrc : imgSrc) + "\"";
                
                var product = new ProductData { Title = title, Price = price, Image = image };

                var images = DeepHarvestBarrattsNode(node, product).ToList();
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

        private IEnumerable<string> DeepHarvestBarrattsNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            SetUpIgnoreList(ignoreList);

            var productLink = node.SelectNodes("div[@class='thumbnailholder']/a").First().Attributes["href"].Value;
            if (ignoreList.Contains(productLink))
            {
                return new[] { "skip" };
            }

            var mainProductHtml = new HtmlDocument();
            var doc = HtmlNode.CreateNode("");
            IEnumerable<string> images = new string[0];
            
            ParseMainProductNodes(product, mainProductHtml, productLink, ref doc, ref images);

            var sizes = new List<HtmlNode>();

            IEnumerable<HtmlNode> sizeNodes = null;

            if (TryGenerateStockSizesWithoutFailure(doc, productLink, sizes, mainProductHtml)) return new[] { "skip" };


            if (!sizes.Any()) return new[] { "skip" };

            FinalizeProductAttributes(product, sizes, productLink);

            return images;
        }

        private static void FinalizeProductAttributes(ProductData product, List<HtmlNode> sizes, string productLink)
        {
            product.Option1Name = "Size";
            product.Option1Value = sizes.First().InnerText;
            product.Handle = "\"" + HandleManager.Encrypt(productLink) + "\"";
            product.Sku = productLink;
            product.Taxable = "FALSE";
            product.RequiresShipping = "TRUE";
            product.FulfillmentService = "manual";
            product.InventoryPolicy = "continue";
            product.Vendor = vendor;
            product.InventoryQuantity = "0";
            product.Tags = "Womens Ballerina Shoes";
            product.Sizes = sizes;
        }

        private static bool TryGenerateStockSizesWithoutFailure(HtmlNode doc, string productLink, List<HtmlNode> sizes,
                                                                HtmlDocument mainProductHtml)
        {
            IEnumerable<HtmlNode> sizeNodes;
            try
            {
                sizeNodes = doc.SelectNodes("//div[@id='sizeSelectorThumbs']/ul/li")
                               .Where(x => x.Attributes["class"].Value != "outOfStock");
            }
            catch
            {
                Console.WriteLine("Failed to get sizes for node: {0}", productLink);
                return true;
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
            return false;
        }

        private void ParseMainProductNodes(ProductData product, HtmlDocument mainProductHtml, string productLink, ref HtmlNode doc,
                                      ref IEnumerable<string> images)
        {
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
        }

        private static void SetUpIgnoreList(List<string> ignoreList)
        {
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-parisienne-notepad-306043");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-parisienne-notepad-306043");
            ignoreList.Add("http://www.barratts.co.uk/en/floral-patterned-satchel-309455");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-cherry-notepad-306041");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-armelle-notepad-306040");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-mini-coin-purse-306032");
            ignoreList.Add("http://www.barratts.co.uk/en/decodelire-patterned-shoulder-bag-306044");
        }
    }
}