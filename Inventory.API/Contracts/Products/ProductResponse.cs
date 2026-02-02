namespace Inventory.API.Contracts.Products
{
    public sealed record ProductResponse(
    int Id,
    string Sku,
    string Name,
    decimal Price,
    bool Active
);
}
