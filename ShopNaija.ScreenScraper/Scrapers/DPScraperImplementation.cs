using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using SHDocVw;
using WebBrowser = System.Windows.Forms.WebBrowser;

namespace ShopNaija.ScreenScraper.Scrapers
{
    public class DPScraperImplementation : ScraperImplementationBase, IScraperImplementation
    {
    	private WebBrowser webBrowser;
    	private const double profitRate = 1.235;
        private const double deliveryRate = 6;
        private const double cardRate = 1.02;
        private const string productType = "Womens Shoes";
        private const string vendor = "HM";

        public DPScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
        {
            RootUrlToGetDataFrom = rootUrlToGetDataFrom;
            BaseAddress = baseAddress;
        }

        public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
        {
            //Debugger.Launch();
            var nodes = document.DocumentNode.SelectNodes("//ul[@class = 'product']");
            //var nodes = document.DocumentNode.SelectNodes("//div/ul[@id='list-products']/li").Where(x => x.Attributes["class"].Value != "getTheLook");

            var data = new List<ProductData>();

            var titleAndHandle = new Dictionary<string, string>();

            foreach (var node in nodes)
            {
                // /a/img[@src]
                var title = node.SelectNodes("li[@class='product_description']/a").First().InnerText
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
                var amounts = node.SelectNodes("li[@class='product_price']");
                if (amounts != null)
                {
                    var priceArray = new string[] { };
                    try
                    {
                    	var innerText = amounts.First().InnerText;
                    	priceArray =
                            innerText
                            .Replace("Orig.:", "")
                            .Replace("From ", "")
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Replace("&pound;", string.Empty)
                            .Replace("&#163;", string.Empty)
                            .Split(new[] { "£" }, StringSplitOptions.RemoveEmptyEntries);

                        price = ((
                            (Convert.ToDouble((priceArray[0]).Trim()) * profitRate + deliveryRate) * cardRate)
                            .ToString("0.00"));
                    }
                    catch (Exception e)
                    {
                        Debugger.Launch();
                        var t = e;
                    }
                }
                else
                {
                	
                }

                if (Convert.ToDecimal(price) > 39.99m) continue;
                //Debugger.Launch();
                var imgSrc = node.SelectNodes("li[@class='product_image']/a/img").First().Attributes["src"].Value.Replace(" ", "%20");
                var image = "\"" + (imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc) + "\"";
                var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
                handle = CheckHandle(handle, titleAndHandle);
                titleAndHandle.Add(handle, title);
                var product = new ProductData { Handle = handle, Title = title, Price = price, Image = image };

                //Debugger.Launch();
                var images = DeepHarvestDPNode(node, product).ToList();
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

        private IEnumerable<string> DeepHarvestDPNode(HtmlNode node, ProductData product)
        {
            var ignoreList = new List<string>();

            // ignoreList.Add("http://www.hm.com/gb/product/07049?article=07049-B");

            var productLink = node.SelectNodes("li[@class='product_description']/a").First().Attributes["href"].Value;
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

            	GetValue(productLink);

            	//var s = b.Document.Body.InnerHtml;
            	//doc = HtmlNode.CreateNode(s);

            	var t = Doc;
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

    	private void GetValue(string productLink)
    	{
    		webBrowser = new System.Windows.Forms.WebBrowser {AllowNavigation = true};
    		webBrowser.DocumentCompleted += b_DocumentCompleted;
    		webBrowser.Navigate(productLink);
    	}

		private string Doc{get { return webBrowser.Document.Body.InnerHtml; }}

    	void b_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
		{
			var b = sender as System.Windows.Forms.WebBrowser;
		}
    }
}