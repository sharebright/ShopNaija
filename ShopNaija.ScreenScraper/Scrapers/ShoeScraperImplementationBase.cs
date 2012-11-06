using System.IO;
using System.Net;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public class ShoeScraperImplementationBase
	{
		protected string RootUrlToGetDataFrom;
		protected string BaseAddress;

		protected string DiscernType(string body, string title)
		{
			var val = string.Empty;
			if (body.ToLower().Contains("shoe") || body.ToLower().Contains("sneaker") || body.ToLower().Contains("boot") || body.ToLower().Contains("slipper") || body.ToLower().Contains("loafer"))
			{
				val = "Shoes";
			}

			if (body.ToLower().Contains("maxi") || body.ToLower().Contains("dress"))
			{
				val = "Dresses";
			}

			if (body.ToLower().Contains("belt"))
			{
				val = "Belt";
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

			using (var response = request.GetResponse())
			{
				using (var sr = new StreamReader(response.GetResponseStream()))
				{
					responseHtml = sr.ReadToEnd();
				}
			}

			return responseHtml;
		}
	}
}