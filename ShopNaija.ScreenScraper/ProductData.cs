using System.Collections.Generic;
using HtmlAgilityPack;

namespace ShopNaija.ScreenScraper
{
    public class ProductData
    {
        public string Handle { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Vendor { get; set; }
        public string Type { get; set; }
        public string Tags { get; set; }
        public string Option1Name { get; set; }
        public string Option1Value { get; set; }
        public string Option2Name { get; set; }
        public string Option2Value { get; set; }
        public string Option3Name { get; set; }
        public string Option3Value { get; set; }
        public string Sku { get; set; }
        public string Weight { get; set; }
        public string InventoryTracker { get; set; }
        public string InventoryQuantity { get; set; }
        public string InventoryPolicy { get; set; }
        public string FulfillmentService { get; set; }
        public string Price { get; set; }
        public string CompareAtPrice { get; set; }
        public string RequiresShipping { get; set; }
        public string Taxable { get; set; }
        public string Image { get; set; }

        public HtmlAgilityPack.HtmlNodeCollection Colours { get; set; }

        public IEnumerable<HtmlNode> Sizes { get; set; }

    	public static ProductData Clone(ProductData product)
        {
        	return new ProductData
        	{
                Sku = product.Sku,
        	    Handle = product.Handle,
        	    Weight = product.Weight,
        	    InventoryQuantity = product.InventoryQuantity,
        	    InventoryPolicy = product.InventoryPolicy,
        	    FulfillmentService = product.FulfillmentService,
        	    Price = product.Price
        	};
        }
    }
}
