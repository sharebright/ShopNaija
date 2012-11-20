using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ZaraScraperImplementation : ScraperImplementationBase, IScraperImplementation
	{
		public ZaraScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			var nodes = document.DocumentNode.SelectNodes("//li[contains(@class,'filteredItem')]");

			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				var img =
					node.SelectNodes("a/img").First().Attributes["data-src"].Value.Split(new[] {"?"}, StringSplitOptions.None)[0];
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
										.Split(new[] {" was "}, StringSplitOptions.RemoveEmptyEntries)[0]
								) * 1.125 + 8) * 1.02).ToString("0.00");

				var product = new ProductData {Image = img, Title = title, Price = price};

				DeepHarvestZaraNode(node, product);
				int count = 0;
				bool added = false;
				foreach (var p in product.Colours)
				{
					if (product.Sizes != null && product.Sizes.Any())
					{
						foreach (var s in product.Sizes)
						{
							if (count == 0)
							{
								added = true;
								data.Add(product);
								count++;
								continue;
							}
							var subProduct = ProductData.Clone(product);

							var splits = p.SelectSingleNode("a").Attributes["title"].Value.Split(new[] {" "}, StringSplitOptions.None);
							var l = splits[1];
							if (splits.Length > 2)
							{
								l = splits[1] + " " + splits[2];
							}

							if (!l.Contains("not"))
							{

								subProduct.Option1Name = "Colour";
								subProduct.Option1Value = l;

								subProduct.Option2Name = "Size";
								subProduct.Option2Value = s.InnerText == string.Empty
															? s.Attributes["value"].Value.Replace("&frac12;", ".5")
															: s.InnerText
																.Replace("\r", "")
																.Replace("\r", "")
																.Replace("&nbsp;", "")
																.Trim();
							}
							else
							{
								subProduct.Option1Name = "Size";
								subProduct.Option1Value = s.InnerText == string.Empty
															? s.Attributes["value"].Value.Replace("&frac12;", ".5")
															: s.InnerText
																.Replace("\r", "")
																.Replace("\r", "")
																.Replace("&nbsp;", "")
																.Trim();

							}
							data.Add(subProduct);
							added = true;
						}
					}
					else
					{
						if (count == 0)
						{
							data.Add(product);
							added = true;
							count++;
							continue;
						}
						var subProduct = ProductData.Clone(product);
						var splits = p.SelectSingleNode("a").Attributes["title"].Value.Split(new[] {" "}, StringSplitOptions.None);
						var l = splits[1];
						if (splits.Length > 2)
						{
							l = splits[1] + " " + splits[2];
						}
						if (!l.Contains("not"))
						{
							subProduct.Option1Name = "Colour";
							subProduct.Option1Value = l;
							data.Add(subProduct);
							added = true;
						}
					}
				}
				if (!added)
				{
					data.Add(product);
				}
			}
			return data;
		}

		private void DeepHarvestZaraNode(HtmlNode node, ProductData product)
		{
			var productLink = node.SelectNodes("div[@class='infoProd']/a").First().Attributes["href"].Value;

			var mainProductHtml = new HtmlDocument();
			HtmlNode doc = HtmlNode.CreateNode("");
			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;

				product.Handle = doc.SelectNodes("//div[@class='prodInfoDesc']/h2")
					.First()
					.InnerText
					.Replace("É", "E")
					.Replace("É".ToLower(), "e")
					.Replace(" ", "-")
					.Replace("\r", "")
					.Replace("\n", "")
					.Replace("\t", "")
					.Replace("&amp;-", "")
					.Replace("É", "E")
					.Replace("É".ToLower(), "e")
					.Trim();

				product.Body = "\"" +
							   doc.SelectNodes("//div[@class='prodInfoDesc']/p[@class='description']").First().InnerText.Replace(
								"\"", "'")
								.Replace("\r", "")
								.Replace("\n", "")
								.Replace("\t", "") + "\"";
				product.Type = "Mens Knitwear"; //DiscernType(product.Body, product.Title);
			}
			catch
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			HtmlNodeCollection sizes = null;
			var productForm = doc.SelectNodes("//div[@class='formProduct']").First();
			//Debugger.Launch();
			product.Option1Name = "Title";
			product.Option1Value = "Title";
			HtmlNodeCollection colours = doc.SelectNodes("//ul[contains(@class,'colorImage')]/li");
			if (colours != null)
			{
				product.Option1Name = "Colour";
				var splits = colours.First().SelectSingleNode("a").Attributes["title"].Value.Split(new[] {" "},
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

			var notAvailable = doc.SelectNodes("//div[@class='tableOptions']/table/tr");
			if (notAvailable.Any())
			{
				sizes = notAvailable.First().SelectNodes("//b[@class='sizeDetail1']");
			}

			if (productForm.InnerHtml.Contains("Size"))
			{
				if (sizes != null)
				{
					if (product.Option1Name == "Title")
					{
						product.Option1Name = "Size";
						product.Option1Value = sizes.First().SelectSingleNode("//b")
							.InnerText
							.Replace("\r", "")
							.Replace("\r", "")
							.Replace("&nbsp;", "")
							.Trim();
					}
					else
					{
						product.Option2Name = "Size";
						product.Option2Value = sizes.First().SelectSingleNode("//b")
							.InnerText
							.Replace("\r", "")
							.Replace("\r", "")
							.Replace("&nbsp;", "")
							.Trim();
					}
				}
			}

			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Zara";
			product.InventoryQuantity = "0";
			product.Tags = "Mens Knitwear";
			product.Sizes = sizes ?? new HtmlNodeCollection(null);
			product.Colours = colours ?? new HtmlNodeCollection(null);
		}
	}
}