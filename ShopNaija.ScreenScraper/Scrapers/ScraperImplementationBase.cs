using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Web;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ScraperImplementationBase
	{
		protected string RootUrlToGetDataFrom;
		protected string BaseAddress;

		protected string DiscernType(string body, string title)
		{
			var val = string.Empty;
			if (body.ToLower().Contains("shoe") || body.ToLower().Contains("sneaker") || body.ToLower().Contains("boot") || body.ToLower().Contains("slipper") || body.ToLower().Contains("loafer"))
			{
				val = "Mens Shoes";
			}
            if (body.ToLower().Contains("sandal"))
            {
                val = "Mens Sandals";
            }
			if (body.ToLower().Contains("maxi") || body.ToLower().Contains("dress"))
			{
				val = "Dresses";
			}

			if (body.ToLower().Contains("belt"))
			{
				val = "Mens Belt";
			}
			if (title.ToLower().Contains("jean") || body.ToLower().Contains("jean"))
			{
				val = "Jeans";
			}

			return val;
		}

		public string GetHtmlString(string urlToGetDataFrom = "")
		{
			string responseHtml;
			var source = string.IsNullOrEmpty(urlToGetDataFrom) ? RootUrlToGetDataFrom : urlToGetDataFrom;
			var request = WebRequest.Create(source);
			request.Method = "GET";
			((HttpWebRequest) request).UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.15 (KHTML, like Gecko) Chrome/24.0.1295.0 Safari/537.15";
			((HttpWebRequest) request).Accept = "text/html";

			try
			{
				//request.ImpersonationLevel=TokenImpersonationLevel.Impersonation;
				using (var response = request.GetResponse())
				{
					using (var sr = new StreamReader(response.GetResponseStream()))
					{

						responseHtml = sr.ReadToEnd();
					}
				}
			}
			catch
			{
				WebClient client = new WebClient();
				
				responseHtml = client.DownloadString(source);
			}
			return responseHtml;
		}
	}
}