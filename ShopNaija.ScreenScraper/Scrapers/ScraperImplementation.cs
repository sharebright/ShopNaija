using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ScraperImplementation : ScraperImplementationBase, IScraperImplementation
	{
		public ScraperImplementation(string rootUrlToGetDataFrom, string baseAddress)
		{
			RootUrlToGetDataFrom = rootUrlToGetDataFrom;
			BaseAddress = baseAddress;
		}

		public IEnumerable<ProductData> RecurseNodes(HtmlDocument document)
		{

			var nodes = document.DocumentNode.SelectNodes("//div[@id='Product_List']/div[@class='product_box']");

			var data = new List<ProductData>();

			foreach (var node in nodes)
			{
				// /a/img[@src]
				var title = node.SelectNodes("p[@class='title']/a").First().InnerText;
				var price = ((Convert.ToDouble(
					node.SelectNodes("p[@class='price']/a").First().InnerText
						.Replace("&pound;", string.Empty)
						.Split(new[] { " was " }, StringSplitOptions.RemoveEmptyEntries)[0]
								) * 1.03 + 11) * 1.02).ToString("0.00");
				var product = new ProductData { Title = title, Price = price };

				//Debugger.Launch();
				var images = DeepHarvestShoeNode(node, product).ToList();
				try
				{ product.Image = images[0]; }
				catch (Exception e)
				{ }
				var count = 0;
				foreach (var p in product.Colours)
				{
					if (product.Sizes != null && product.Sizes.Any())
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
							if (images.Count >= count)
							{
								subProduct.Image = images[count - 1];
							}
							count++;
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
						if (images.Count >= count)
						{
							subProduct.Image = images[count - 1];
						}
						count++;
						data.Add(subProduct);
					}
				}
			}
			return data;
		}

		private IEnumerable<string> GetVariantImages(Browser browser)
		{
			var variantImages = new List<string>();

			var links = browser.Div("colours_js_box").Divs.First().Links.Select(x => x.Title);
			foreach (var link in links)
			{
				Browser b = new IE(browser.Url);
				b.ShowWindow(NativeMethods.WindowShowStyle.Hide);
				b.Link(Find.ByTitle(link)).Click();
				var d = new HtmlDocument();
				d.LoadHtml(GetHtmlString(b.Url));
				b.Close();
				var node = d.DocumentNode.SelectNodes("//img[@id='productimage']").First();

				variantImages.Add(BaseAddress + node.Attributes["src"].Value);
			}
			return variantImages;
		}

		private IEnumerable<string> DeepHarvestShoeNode(HtmlNode node, ProductData product)
		{
			//Debugger.Launch();
			var productLink = BaseAddress + node.SelectNodes("p[@class='view_buy']/a").First().Attributes["href"].Value;

			var browser = new IE(productLink, false);
			browser.GoTo(productLink);
			browser.ShowWindow(NativeMethods.WindowShowStyle.Hide);

			var mainProductHtml = new HtmlDocument();
			var doc = HtmlNode.CreateNode("");
			IEnumerable<string> images = new string[0];
			try
			{
				mainProductHtml.LoadHtml(GetHtmlString(productLink));
				// //div[@id="productright"]/div[@class=product_info]/p
				doc = mainProductHtml.DocumentNode;

				images = GetVariantImages(browser);
				browser.Close();

				product.Handle = new Uri(productLink).AbsolutePath.Replace("/products/", string.Empty).Replace("/", "-");
				product.Body = "\"" + doc.SelectNodes("//div[@id='productright']/div[@class='product_info']/p").First().InnerText.Replace("\"", "'") + "\"";
				product.Type = "Mens Shoes"; DiscernType(product.Body, product.Title);
			}
			catch (Exception e)
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

            product.Sku = productLink;
            product.Taxable = "FALSE";
			product.RequiresShipping = "TRUE";
			product.FulfillmentService = "manual";
			product.InventoryPolicy = "continue";
			product.Vendor = "Henry James";
			product.InventoryQuantity = "0";
			product.Tags = "Mens Shoes";
			product.Sizes = sizes;
			product.Colours = colours;

			return images;
		}
	}
}