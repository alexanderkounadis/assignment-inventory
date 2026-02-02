namespace Inventory.API.Contracts.Inventory
{
    public sealed record CreateMovementRequest(
    string Type,              // Purchase, Sale, Adjustment, Transfer
    int ProductId,
    decimal Quantity,
    int? WarehouseId = null,  // required for Purchase/Sale/Adjustment
    int? FromWarehouseId = null, // required for Transfer
    int? ToWarehouseId = null,   // required for Transfer
    string? Notes = null
);
}
