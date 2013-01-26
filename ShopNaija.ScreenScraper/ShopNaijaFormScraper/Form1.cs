using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using HtmlAgilityPack;

//using ShopNaija.ScreenScraper;

//using ShopNaija.ScreenScraper;

namespace ShopNaijaFormScraper
{
    public enum Progression
    {
        Step1 = 0,
        Step2,
        Step3
    }
    public partial class Form1 : Form
    {
        private const double profitRate = 1.235;
        private const double deliveryRate = 6;
        private const double cardRate = 1.02;
        private const string productType = "Womens Dresses";
        private const string vendor = "DorothyPerkins";

        private WebBrowser webBrowser;
        private bool loaded = false;
        private List<ProductData> Products = new List<ProductData>();
        private Progression CurrentStep = Progression.Step1;
        private Dictionary<string, string> titleAndHandle;

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
            CurrentStep = Progression.Step2;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            HtmlAgilityPack.HtmlDocument document = null;
            if (Progression.Step2 == CurrentStep)
            {
                Nodes = document.DocumentNode.SelectNodes("//ul[@class = 'product']");
                ProductSet = ConvertNodeToProduct(Nodes[CurrentNode]);
                if (ProductSet.Count() - 1 > CurrentProductNumber)
                {
                    Products.Add(ProductSet[CurrentProductNumber]);
                    CurrentProductNumber++;
                }
                else
                {
                    CurrentProductNumber = 0;
                    CurrentNode++;
                }

            }
            else
            {
                var uriString = textBox1.Text;
                var productLink = new Uri(uriString);
                webBrowser.Navigate(productLink);
            }
        }

        protected int CurrentProductNumber { get; set; }

        protected ProductData[] ProductSet { get; set; }

        protected int CurrentNode { get; set; }

        private ProductData[] ConvertNodeToProduct(HtmlNode node)
        {
            var products = new List<ProductData>();

            CurrentProduct = new ProductData();
            CurrentProduct.Title = GetTitle(node);
            CurrentProduct.Price = GetPrice(node);
            CurrentProduct.Image = GetMainImage(node);
            CurrentProduct.Handle = GetHandle(node);
            var productDetails = GetDetails(node);
            return products.ToArray();
        }

        private HtmlNode GetDetails(HtmlNode node)
        {
            var productLink = node.SelectNodes("li[@class='product_description']/a").First().Attributes["href"].Value;
            webBrowser.Navigate(productLink);
            return null;
        }

        protected ProductData CurrentProduct { get; set; }

        private string GetHandle(HtmlNode node)
        {
            titleAndHandle = new Dictionary<string, string>();
            var handle = (productType + " " + Guid.NewGuid()).Replace(" ", "-");
            handle = CheckHandle(handle, titleAndHandle);
            titleAndHandle.Add(handle, CurrentProduct.Title);
            return handle;
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


        private string GetMainImage(HtmlNode node)
        {
            var imgSrc = node.SelectNodes("li[@class='product_image']/a/img").First().Attributes["src"].Value.Replace(" ", "%20");
            var image = "\"" + (imgSrc.StartsWith("//") ? "http:" + imgSrc : imgSrc) + "\"";

            return image;

        }

        private string GetPrice(HtmlNode node)
        {
            var amounts = node.SelectNodes("li[@class='product_price']");
            var price = string.Empty;

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
            return price;
        }

        private string GetTitle(HtmlNode node)
        {
           return node.SelectNodes("li[@class='product_description']/a").First().InnerText
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
        }

        protected string Body { get; set; }

        public HtmlNodeCollection Nodes { get; set; }
    }
}
