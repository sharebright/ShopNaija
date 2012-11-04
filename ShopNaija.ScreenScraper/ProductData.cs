using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public string SKU { get; set; }
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

        public HtmlAgilityPack.HtmlNodeCollection Sizes { get; set; }

        internal static ProductData Clone(ProductData product)
        {
            ProductData p = new ProductData();
            p.Handle = product.Handle;
            p.Weight = product.Weight;
            p.InventoryQuantity = product.InventoryQuantity;
            p.InventoryPolicy = product.InventoryPolicy;
            p.FulfillmentService = product.FulfillmentService;
            p.Price = product.Price;
            return p;
        }
    }
}
