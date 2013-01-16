using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
	public partial class Form1 : Form
	{
		private WebBrowser webBrowser;
		private bool loaded;

		public Form1()
		{
			InitializeComponent();

			webBrowser = new WebBrowser();
			webBrowser.AllowNavigation = true;
			webBrowser.DocumentCompleted += b_DocumentCompleted;

			btnFinish.Enabled = false;
		}

		public string BodyHtml
		{
			get { return webBrowser.Document.Body.InnerHtml; }

		}

		private void b_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			btnFinish.Enabled = loaded;
			var b = sender as WebBrowser;
			loaded = true;
		}

		private void LoadHtmlClick(object sender, EventArgs e)
		{
			var uriString = textBox2.Text;
			var productLink = new Uri(uriString);
			webBrowser.Navigate(productLink);
		}

		private void btnFinish_Click(object sender, EventArgs e)
		{
			var d = new HtmlAgilityPack.HtmlDocument();

			d.LoadHtml(BodyHtml);

			var doc = d.DocumentNode;

			var b = doc.InnerHtml.Contains("");

			MessageBox.Show(b.ToString());
		}
	}
}
