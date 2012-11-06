using System.Collections.Generic;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper.Scrapers
{
	public interface IScraperImplementation
	{
		string GetHtmlString(string urlToGetDataFrom = "");
		IEnumerable<ProductData> RecurseNodes(HtmlDocument document);
	}
}