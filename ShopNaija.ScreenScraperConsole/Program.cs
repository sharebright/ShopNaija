using System;
using ShopNaija.ScreenScraper;
using System.Linq;

namespace ShopNaija.ScreenScraperConsole
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var url = args[0];
			
			var baseAddress = args[1];

			var scraper = new Scraper(url, baseAddress);
			Console.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}", "Handle", "Title", "Body (HTML)", "Vendor", "Type", "Tags", "Option1 Name", "Option1 Value", "Option2 Name", "Option2 Value", "Option3 Name", "Option3 Value", "Variant SKU", "Variant Grams", "Variant Inventory Tracker", "Variant Inventory Qty", "Variant Inventory Policy", "Variant Fulfillment Service", "Variant Price", "Variant Compare At Price", "Variant Requires Shipping", "Variant Taxable", "Image Src"));
			foreach (var product in scraper.Scrape().Data.OrderBy(x=>x.Handle))
			{
				//"Handle","Title","Body (HTML)","Vendor","Type","Tags","Option1 Name","Option1 Value","Option2 Name","Option2 Value","Option3 Name","Option3 Value","Variant SKU","Variant Grams","Variant Inventory Tracker","Variant Inventory Qty","Variant Inventory Policy","Variant Fulfillment Service","Variant Price","Variant Compare At Price","Variant Requires Shipping","Variant Taxable","Image Src"
				Console.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}", product.Handle, product.Title, product.Body, product.Vendor, product.Type, product.Tags, product.Option1Name, product.Option1Value, product.Option2Name, product.Option2Value, product.Option3Name, product.Option3Value, product.Sku, product.Weight, product.InventoryTracker, product.InventoryQuantity, product.InventoryPolicy, product.FulfillmentService, product.Price, product.CompareAtPrice, product.RequiresShipping, product.Taxable, product.Image));
			}
		    //Console.ReadKey();
		}
	}
}
