using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ZaraScraperImplementation : ScraperImplementationBase, IScraperImplementation
	{
		private const double profitRate = 1.205;
		private const double deliveryRate = 9;
		private const double cardRate = 1.02;
		private const string productType = "Womens Shoes";
		private const string vendor = "Zara";
		public ZaraScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			var nodes = document.DocumentNode.SelectNodes("//li[contains(@class,'filteredItem')]");

			var data = new List<ProductData>();

			var titleAndHandle = new Dictionary<string, string>();
			foreach (var node in nodes)
			{
				var img =
					node.SelectNodes("a/img").First().Attributes["data-src"].Value.Split(new[] { "?" }, StringSplitOptions.None)[0];
				var title =
					node.SelectNodes("div[@class='infoProd']/a")
					.First()
					.InnerText
					.Replace("\n", "")
					.Replace("\t", "")
					.Replace("É", "E")
					.Replace("É".ToLower(), "e")
					.Trim();

				var price = (
								(Convert.ToDouble(
									node.SelectNodes("div[@class='infoProd']/p[@class='price']").First().InnerText
										.Replace("\n", "")
										.Replace("\t", "")
										.Replace("\r", "")
										.Replace("GBP", "")
										.Replace("&pound;", string.Empty)
										.Replace("£", "")
										.Trim()
										.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)[0]
								) * profitRate + deliveryRate) * cardRate).ToString("0.00");

				if (Convert.ToDecimal(price) > 49.99m) continue;

				var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
				handle = CheckHandle(handle, titleAndHandle);
				titleAndHandle.Add(handle, title);
				var product = new ProductData { Handle = handle, Title = title, Price = price, Image = img };

				var images = DeepHarvestZaraNode(node, product).ToList();
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

		private IEnumerable<string> DeepHarvestZaraNode(HtmlNode node, ProductData product)
		{
			var productLink = node.SelectNodes("div[@class='infoProd']/a").First().Attributes["href"].Value;

			var mainProductHtml = new HtmlDocument();
			HtmlNode doc = HtmlNode.CreateNode("");

			IEnumerable<string> images = new string[0];

			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;
				images = new[] { doc.SelectNodes("//img[@id='bigImage']").First().Attributes["src"].Value };

				//product.Handle = doc.SelectNodes("//div[@class='prodInfoDesc']/h2")
				//    .First()
				//    .InnerText
				//    .Replace("É", "E")
				//    .Replace("É".ToLower(), "e")
				//    .Replace(" ", "-")
				//    .Replace("\r", "")
				//    .Replace("\n", "")
				//    .Replace("\t", "")
				//    .Replace("&amp;-", "")
				//    .Replace("É", "E")
				//    .Replace("É".ToLower(), "e")
				//    .Trim();

				product.Body = "\"" +
							   doc.SelectNodes("//div[@class='prodInfoDesc']/p[@class='description']").First().InnerText.Replace(
								"\"", "'")
								.Replace("\r", "")
								.Replace("\n", "")
								.Replace("\t", "") + "\"";
				product.Type = productType; //"Mens Knitwear"; //DiscernType(product.Body, product.Title);
			}
			catch
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			//HtmlNodeCollection sizes = null;
			var productForm = doc.SelectNodes("//div[@class='formProduct']").First();
			//Debugger.Launch();
			product.Option1Name = "Title";
			product.Option1Value = "Title";
			HtmlNodeCollection colours = doc.SelectNodes("//ul[contains(@class,'colorImage')]/li");
			if (colours != null)
			{
				product.Option1Name = "Colour";
				var splits = colours.First().SelectSingleNode("a").Attributes["title"].Value.Split(new[] { " " },
																								   StringSplitOptions.None);
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

			//var notAvailable = doc.SelectNodes("//div[@class='tableOptions']/table/tr");
			//if (notAvailable.Any())
			//{
			//    sizes = notAvailable.First().SelectNodes("//b[@class='sizeDetail1']");
			//}

			//if (productForm.InnerHtml.Contains("Size"))
			//{
			//    if (sizes != null)
			//    {
			//        if (product.Option1Name == "Title")
			//        {
			//            product.Option1Name = "Size";
			//            product.Option1Value = sizes.First().SelectSingleNode("//b")
			//                .InnerText
			//                .Replace("\r", "")
			//                .Replace("\r", "")
			//                .Replace("&nbsp;", "")
			//                .Trim();
			//        }
			//        else
			//        {
			//            product.Option2Name = "Size";
			//            product.Option2Value = sizes.First().SelectSingleNode("//b")
			//                .InnerText
			//                .Replace("\r", "")
			//                .Replace("\r", "")
			//                .Replace("&nbsp;", "")
			//                .Trim();
			//        }
			//    }
			//}

			var xsmall = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 0)
			{
				InnerHtml = "3"
			};
			var small = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 1)
			{
				InnerHtml = "S"
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

			var sizes = new HtmlNode[] { xsmall, small, medium, large, xlarge, xxlarge, /*xxxlarge, xxxxlarge, xl, xxl, xxxl, xl1, xxl1, xxxl1 */};

			product.Sku = productLink;
			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = vendor;
			product.InventoryQuantity = "0";
			product.Tags = productType;
			product.Sizes = sizes;
			//product.Colours = colours ?? new HtmlNodeCollection(null);

			return images;
		}
	}
}