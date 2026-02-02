namespace Inventory.API.Contracts.Products
{
    public sealed record CreateProductRequest(
    string Sku,
    string Name,
    decimal Price,
    bool Active = true
);
}
