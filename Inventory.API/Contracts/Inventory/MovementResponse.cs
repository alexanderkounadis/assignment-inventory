namespace Inventory.API.Contracts.Inventory
{
    public sealed record MovementResponse(
    long Id,
    string Type,
    int ProductId,
    decimal Quantity,
    int? WarehouseId,
    int? FromWarehouseId,
    int? ToWarehouseId,
    int CreatedByUserId,
    DateTimeOffset CreatedAt
);
}
