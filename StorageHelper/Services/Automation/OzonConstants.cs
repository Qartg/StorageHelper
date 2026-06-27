namespace StorageHelper.Services.Automation
{
    public static class OzonConstants
    {
        public const string BrowserProfileName = "OzonBrowserProfile";

        public const string Home = "https://www.ozon.ru/";
        public const string Cart = Home + "cart";
        public static string Product(string sku) => $"{Home}product/{sku}/?oos_search=false";

        public const string JsonLdScript    = "script[type='application/ld+json']";
        public const string VendorLink      = "a[href*='/seller/'][title]";
        public const string OutOfStockBlock = "[data-widget='webOutOfStock']";
        public const string AddToCartWidget = "[data-widget='webAddToCart']:visible";

        public const string QuantityInput = "input[inputmode='decimal']";
        public const string QuantityInputAncestor = "xpath=ancestor::*[.//input[@inputmode='decimal']][1]";

        public const string AnonymousProfileMenu = "[data-widget='profileMenuAnonymous']";

        public const string AddToCartButtonText = "В корзину";
        public const string InCartButtonText    = "В корзине";

        public const string AddToCartResponse   = "addToCart";
        public const string CartSummaryResponse = "_action/summary";
    }
}
