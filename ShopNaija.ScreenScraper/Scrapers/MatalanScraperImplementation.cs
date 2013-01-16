using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	class MatalanScraperImplementation : ScraperImplementationBase, IScraperImplementation
	{
		public MatalanScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			var nodes = document.DocumentNode.SelectNodes("//div[contains(@class,'productCont')]");

			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				var img = BaseAddress +
						  node.SelectNodes("div[contains(@class,'productImageCont')]/a/img[starts-with(@id, 'product_')]")
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

				var handle = title.Replace(" ", "-");

				var price = (
								(Convert.ToDouble(
									node.SelectNodes("div[@class='productPrice']/p/span[contains(@class,'onePrice')]").First().InnerText
										.Replace("\n", "")
										.Replace("\t", "")
										.Replace("\r", "")
										.Replace("GBP", "")
										.Replace("&pound;", string.Empty)
										.Replace("£", "")
										.Trim()
										.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
								) * 1.20 + 12) * 1.02).ToString("0.00");

				var product = new ProductData { Image = img, Title = title, Price = price };

				DeepHarvestMatalanNode(node, product);
				product.Handle = handle;
				data.Add(product);
			}
			return data;
		}

		private void DeepHarvestMatalanNode(HtmlNode node, ProductData product)
		{
            //product.Sku = productLink;
            product.Option1Name = "Title";
			product.Option1Value = "Title";
			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Matalan";
			product.InventoryQuantity = "0";
			product.Tags = "Mens Shoes";
		}
	}
}