using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class Forever21ScraperImplementation : ScraperImplementationBase, IScraperImplementation
	{
		public Forever21ScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			//Debugger.Launch();

			var nodes = document.DocumentNode.SelectNodes("//table[@class='dlCategoryList']/tr/td/table");

			var data = new List<ProductData>();

			var titleAndHandle = new Dictionary<string, string>();

			foreach (var node in nodes)
			{
				// /a/img[@src]
				var title = node.SelectNodes("tr//div[@class='DisplayName']").First().InnerText
					.Replace("&eacute;", "e")
					.Replace("&acute;", "e")
					.Replace("w/", "with")
					.Replace("&amp;", "and")
					.Replace("&trade;", "")
					.Replace("&", "and")
					.Replace("3/4", "3-quarter")
					.Replace("\t", " ")
					.Replace("/t", " ")
					.Trim();

				string price = "";
				if (node.SelectNodes("tr//font[@class='oprice']") != null)
				{
					price = ((Convert.ToDouble(
						node.SelectNodes("tr//font[@class='oprice']").First().InnerText
							.Replace("Orig.:", "")
							.Replace("&pound;", string.Empty).Replace("£", string.Empty)
							.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
								  ) * 1.4 + 12) * 1.02).ToString("0.00");
				}
				else
				{
					price = ((Convert.ToDouble(
						node.SelectNodes("tr//font[@class='price']").First().InnerText
							.Replace("Now:", "")
							.Replace("&pound;", string.Empty).Replace("£", string.Empty)
							.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
								  ) * 1.4 + 12) * 1.02).ToString("0.00");
				}

				if (Convert.ToDecimal(price) > 79.99m) continue;
				//Debugger.Launch();
				var image = node.SelectNodes("tr/td/div/a/img").First().Attributes["src"].Value;
				var handle = title.Replace(" ", "-");
				handle = CheckHandle(handle, titleAndHandle);
				titleAndHandle.Add(handle, title);
				var product = new ProductData { Handle = handle, Title = title, Price = price, Image = image };

				//Debugger.Launch();
				var images = DeepHarvestForever21Node(node, product).ToList();

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

		private IEnumerable<string> DeepHarvestForever21Node(HtmlNode node, ProductData product)
		{
			var productLink = node.SelectNodes("tr/td/div/a").First().Attributes["href"].Value;

			var mainProductHtml = new HtmlDocument();
			var doc = HtmlNode.CreateNode("");
			IEnumerable<string> images = new string[0];
			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;
				images = new[] { doc.SelectNodes("//img[@class='ItemImage']").First().Attributes["src"].Value };
				try
				{
					var body = doc.SelectNodes("//td[@class='productdesc']/span/p").First().InnerText.Replace("\"", "'").Replace("- US size - refer to size chart for conversion", "").Replace("See Return Policy", "").Replace("\t", " ").Replace("/t", " ").Replace("&trade;", "").Replace("&amp;", "and").Replace("&", "and").Replace("&nbsp;", " ").Replace("Love 21 - ", "").Replace("Forever 21 ", "").Replace("&eacute", "e").Replace("&acute", "e").Trim();
					var indexOf = body.IndexOf("Product Code :", System.StringComparison.Ordinal);
					body = body.Substring(0, indexOf);
					product.Body = "\"" + body + "\"";
				}
				catch
				{
					var body = doc.SelectNodes("//td[@class='productdesc']/span").First().InnerText.Replace("\"", "'").Replace("- US size - refer to size chart for conversion", "").Replace("See Return Policy", "").Replace("\t", " ").Replace("/t", " ").Replace("&trade;", "").Replace("&amp;", "and").Replace("&", "and").Replace("&nbsp;", " ").Replace("Love 21 - ", "").Replace("Forever 21 ", "").Replace("&eacute", "e").Replace("&acute", "e").Trim();
					var indexOf = body.IndexOf("Product Code :", System.StringComparison.Ordinal);
					body = body.Substring(0, indexOf);
					product.Body = "\"" + body + "\"";
				}
				product.Type = "Womens T-Shirts";
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			var xsmall = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 0)
			{
				InnerHtml = "S"
			};
			var small = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 1)
			{
				InnerHtml = "M"
			};
			var medium = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 2)
			{
				InnerHtml = "L"
			};
			var large = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 3)
			{
				InnerHtml = "XL"
			};
			var xlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 4)
			{
				InnerHtml = ""
			};
			var xxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 5)
			{
				InnerHtml = "38L"
			};
			var xxxlarge = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 6)
			{
				InnerHtml = "40S"
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
			};
			var sizes = new HtmlNode[] { xsmall, small, medium, large/*, xlarge, xxlarge, xxxlarge, xxxxlarge, xl, xxl, xxxl, xl1, xxl1, xxxl1 */};

			product.Option1Name = "Size";
			product.Option1Value = sizes.First().InnerText;



			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Forever21";
			product.InventoryQuantity = "0";
			product.Tags = "Womens T-Shirts";
			product.Sizes = sizes;

			return images;
		}
	}
}