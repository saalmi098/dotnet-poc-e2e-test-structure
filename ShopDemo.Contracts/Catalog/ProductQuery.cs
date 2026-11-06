namespace ShopDemo.Contracts.Catalog;

public sealed record ProductQuery(
    string? Search = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    bool OnSaleOnly = false,
    ProductSort Sort = ProductSort.Featured,
    int Page = 1,
    int PageSize = 12);
