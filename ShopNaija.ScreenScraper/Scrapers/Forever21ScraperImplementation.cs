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
			var nodes = document.DocumentNode.SelectNodes("//table[@class='dlCategoryList']/tr/td/table");

			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				// /a/img[@src]
				var title = node.SelectNodes("tr//div[@class='DisplayName']").First().InnerText
					.Replace("&eacute;", "e")
					.Replace("&acute;", "e")
					.Replace("w/", "with")
					.Replace("&amp;", "and")
					.Replace("&", "and")
					.Replace("3/4", "3-quarter")
					.Trim();

				var price = ((Convert.ToDouble(
					node.SelectNodes("tr//font[@class='price']").First().InnerText
						.Replace("&pound;", string.Empty).Replace("£", string.Empty)
						.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
								  ) * 1.3 + 15) * 1.02).ToString("0.00");

				if (Convert.ToDecimal(price) > 60m) continue;
				//Debugger.Launch();
				var image = node.SelectNodes("tr/td/div[@class='ItemImage']/a/img").First().Attributes["src"].Value;
				var product = new ProductData { Handle = title.Replace(" ", "-"), Title = title, Price = price, Image = image };

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

		private IEnumerable<string> DeepHarvestForever21Node(HtmlNode node, ProductData product)
		{
			var productLink = node.SelectNodes("tr/td/div[@class='ItemImage']/a").First().Attributes["href"].Value;

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
					var body = doc.SelectNodes("//td[@class='productdesc']/span/p").First().InnerText.Replace("\"", "'").Replace("&nbsp;", " ").Replace("Love 21 - ", "").Replace("Forever 21 ", "").Replace("&eacute", "e").Replace("&acute", "e").Trim();
					var indexOf = body.IndexOf("Product Code :", System.StringComparison.Ordinal);
					body = body.Substring(0, indexOf);
					product.Body = "\"" + body + "\"";
				}
				catch
				{
					var body = doc.SelectNodes("//td[@class='productdesc']/span").First().InnerText.Replace("\"", "'").Replace("&nbsp;", " ").Replace("Love 21 - ", "").Replace("Forever 21 ", "").Replace("&eacute", "e").Replace("&acute", "e").Trim();
					var indexOf = body.IndexOf("Product Code :", System.StringComparison.Ordinal);
					body = body.Substring(0, indexOf);
					product.Body = "\"" + body + "\"";
				}
				product.Type = "Womens Dresses";
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			var medium = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 1)
			{
				InnerHtml = "Medium"
			};
			var large = new HtmlNode(HtmlNodeType.Element, mainProductHtml, 2)
			{
				InnerHtml = "Large"
			};
			var sizes = new HtmlNode[] { medium, large };

			product.Option1Name = "Size";
			product.Option1Value = "Small";



			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Forever21";
			product.InventoryQuantity = "0";
			product.Tags = "Womens Dresses";
			product.Sizes = sizes;

			return images;
		}
	}
}