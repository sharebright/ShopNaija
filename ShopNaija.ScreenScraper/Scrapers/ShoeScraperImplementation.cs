using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ShoeScraperImplementation : ShoeScraperImplementationBase, IScraperImplementation
	{
		public ShoeScraperImplementation(string rootUrlToGetDataFrom,string baseAddress)
		{
			this.rootUrlToGetDataFrom = rootUrlToGetDataFrom;
			this.baseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{
			var nodes = document.DocumentNode.SelectNodes("//div[@id='Product_List']/div[@class='product_box']");

			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				// /a/img[@src]
				var img = baseAddress + node.SelectNodes("a/img").First().Attributes["src"].Value;
				var title = node.SelectNodes("p[@class='title']/a").First().InnerText;
				var price = (Convert.ToDouble(
					node.SelectNodes("p[@class='price']/a").First().InnerText
						.Replace("&pound;", string.Empty)
						.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
				             	) * 1.5 * 270).ToString();
				var product = new ProductData { Image = img, Title = title, Price = price };

				DeepHarvestShoeNode(node, product);
				int count = 0;

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
							subProduct.Option1Value = p.InnerText;
							subProduct.Option2Name = "Size";
							subProduct.Option2Value = s.InnerText.Replace("&frac12;", ".5");

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
						subProduct.Option1Value = p.InnerText;
						data.Add(subProduct);
					}
				}


			}

			return data;
		}

		private void DeepHarvestShoeNode(HtmlNode node, ProductData product)
		{
			var productLink = baseAddress + node.SelectNodes("p[@class='view_buy']/a").First().Attributes["href"].Value;

			var mainProductHtml = new HtmlDocument();
			HtmlNode doc = HtmlNode.CreateNode("");
			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;

				product.Handle = new Uri(productLink).AbsolutePath.Replace("/products/", string.Empty).Replace("/", "-");
				product.Body = "\"" + doc.SelectNodes("//div[@id='productright']/div[@class='product_info']/p").First().InnerText.Replace("\"", "'") + "\"";
				product.Type = DiscernType(product.Body, product.Title);
			}
			catch
			{
				Console.WriteLine("Exception thrown trying to parse: {0}", productLink);
			}

			HtmlNodeCollection colours = null;
			HtmlNodeCollection sizes = null;
			var productForm = doc.SelectNodes("//div[@id='product_form']").First();

			product.Option1Name = "Title";
			product.Option1Value = "Title";

			if (productForm.InnerHtml.Contains("Colour"))
			{
				colours = doc.SelectNodes("//div[@id='colours_js_box']").First().SelectNodes("//a[contains(@class,'product_colour')]");
				product.Option1Name = "Colour";
				product.Option1Value = colours.First().InnerText;
			}

			if (productForm.InnerHtml.Contains("Size"))
			{
				sizes = doc.SelectNodes("//div[@id='sizes_js_box']").First().SelectNodes("//a[contains(@class,'product_sizes')]");
				if (product.Option1Name == "Title")
				{
					product.Option1Name = "Colour";
					product.Option1Value = sizes.First().InnerText;
				}
				else
				{
					product.Option2Name = "Size";
					product.Option2Value = sizes.First().InnerText;
				}
			}

			product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Henry James";
			product.InventoryQuantity = "0";
			product.Tags = "Henry James Shoes Belts Boots Loafers Leather Suede";
			product.Sizes = sizes;
			product.Colours = colours;
		}
	}
}