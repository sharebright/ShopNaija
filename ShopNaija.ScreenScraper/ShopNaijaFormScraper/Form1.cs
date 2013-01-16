using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HtmlAgilityPack;
using ShopNaija.ScreenScraper;

namespace ShopNaijaFormScraper
{
	public partial class Form1 : Form
	{
		private WebBrowser webBrowser;
		private bool loaded = false;
		private List<ProductData> Products = new List<ProductData>();

		public Form1()
		{
			InitializeComponent();

			webBrowser = new WebBrowser();
			webBrowser.AllowNavigation = true;
			webBrowser.DocumentCompleted += webBrowserCompleted;

		}

		private void webBrowserCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			var browser = sender as WebBrowser;
			Body = browser.Document.Body.InnerHtml;
			btnLoad.Text = "Continue";
		}					

		private void btnLoad_Click(object sender, EventArgs e)
		{
			HtmlAgilityPack.HtmlDocument document = null;
			if (btnLoad.Text == "Continue")
			{
				Nodes = document.DocumentNode.SelectNodes("//ul[@class = 'product']");
				foreach (var node in Nodes)
				{
					foreach(var product in ConvertNodeToProduct(node))
					{
						Products.Add(product);
					}
				}
			}
			else
			{
				var uriString = textBox1.Text;
				var productLink = new Uri(uriString);
				webBrowser.Navigate(productLink);
			}
		}

		private IEnumerable<ProductData> ConvertNodeToProduct(HtmlNode node)
		{
			var products = new List<ProductData>();
			return products;
		}

		protected string Body { get; set; }

		public HtmlAgilityPack.HtmlNodeCollection Nodes { get; set; }
	}
}
