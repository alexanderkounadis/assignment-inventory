namespace Inventory.API.Contracts.Inventory
{
    public sealed record StockRowResponse(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string Sku,
    string ProductName,
    decimal QuantityOnHand
);
}
