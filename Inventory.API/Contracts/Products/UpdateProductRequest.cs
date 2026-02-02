namespace Inventory.API.Contracts.Products
{
    public sealed record UpdateProductRequest(
    string Name,
    decimal Price,
    bool Active
);
}
