using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class MonsoonScraperImplementation : ShoeScraperImplementationBase, IScraperImplementation
	{
		public MonsoonScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			this.RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			this.BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			var nodes = document.DocumentNode.SelectNodes("//div[contains(@class,'productList_item')]");
			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				// /a/img[@src]
				var img = node.SelectNodes("div/a/img").First().Attributes["src"].Value;
				var title = node.SelectNodes("div[@class='productList_info']/div[@class='productList_name']/a").First().InnerText.Replace("\n", "").Replace("\t", "").Trim();

				var price = (Convert.ToDouble(
					node.SelectNodes("div[@class='productList_info']/div[@class='productList_prices']/div[contains(@class,'price')]/a").First().InnerText.Replace("\n", "").Replace("\t", "").Trim()
						.Replace("&pound;", string.Empty)
						.Replace("£", "")
						.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
				             	) * 1.5 * 270).ToString(CultureInfo.InvariantCulture);
				var product = new ProductData { Image = img, Title = title, Price = price };

				DeepHarvestMonsoonNode(node, product);
				var count = 0;

				foreach (var p in product.Colours)
				{
					if (product.Sizes != null && product.Sizes.Count > 0)
					{
						foreach (var s in product.Sizes)
						{
							if (count == 0)
							{
								data.Add(product);
								count++;
								continue;
							}
							var subProduct = ProductData.Clone(product);
							subProduct.Option1Name = "Colour";
							subProduct.Option1Value = p.InnerText == string.Empty ? p.Attributes["value"].Value : p.InnerText;
							subProduct.Option2Name = "Size";
							subProduct.Option2Value = s.InnerText == string.Empty ? s.Attributes["value"].Value.Replace("&frac12;", ".5") : s.InnerText;

							data.Add(subProduct);
						}
					}
					else
					{
						if (count == 0)
						{
							data.Add(product);
							count++;
							continue;
						}
						var subProduct = ProductData.Clone(product);
						subProduct.Option1Name = "Colour";
						subProduct.Option1Value = p.InnerText == string.Empty ? p.Attributes["value"].Value : p.InnerText;
						data.Add(subProduct);
					}
				}
			}
			return data;
		}

		private void DeepHarvestMonsoonNode(HtmlNode node, ProductData product)
		{
			var productLink = BaseAddress + node.SelectNodes("div[@class='productList_img']/a").First().Attributes["href"].Value;

			var mainProductHtml = new HtmlDocument();
			var doc = HtmlNode.CreateNode("");
			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;

				product.Handle = doc.SelectNodes("//div[@class='productDetail_name_and_description']/h1")
					.First()
					.InnerText
					.Replace(" ", "-")
					.Replace("\r", "")
					.Replace("\n", "")
					.Replace("\t", "")
					.Replace("&amp;-", "")
					.Trim();

				product.Body = "\"" + doc.SelectNodes("//div[@class='productDescriptionText']").First().InnerText.Replace("\"", "'")
				                      	.Replace("\r", "")
				                      	.Replace("\n", "")
				                      	.Replace("\t", "") + "\"";
				product.Type = "Dresses"; DiscernType(product.Body, product.Title);
			}
			catch
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			HtmlNodeCollection colours = null;
			HtmlNodeCollection sizes = null;
			var productForm = doc.SelectNodes("//div[@class='clearBoth variant_matrix']").First();

			product.Option1Name = "Title";
			product.Option1Value = "Title";

			if (productForm.InnerHtml.Contains("Colour"))
			{
				colours = doc.SelectNodes("//select [@id=\"attributes'colour'\"]").First().SelectNodes("option");
				product.Option1Name = "Colour";
				product.Option1Value = colours.Select(x => x.Attributes["value"].Value).First();
			}

			if (productForm.InnerHtml.Contains("Size"))
			{
				sizes = doc.SelectNodes("//select [@id=\"attributes'size'\"]").First().SelectNodes("option");
				if (product.Option1Name == "Title")
				{
					product.Option1Name = "Size";
					product.Option1Value = sizes.Select(x => x.Attributes["value"].Value).First();
				}
				else
				{
					product.Option2Name = "Size";
					product.Option2Value = sizes.Select(x => x.Attributes["value"].Value).First();
				}
			}

			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Monsoon";
			product.InventoryQuantity = "0";
			product.Tags = "Monsoon Dress Dresses Maxi Dress Casual Style";
			product.Sizes = sizes;
			product.Colours = colours;
		}
	}
}