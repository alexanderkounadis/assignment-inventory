namespace Inventory.API.Contracts.Warehouses
{
    public sealed record UpdateWarehouseRequest(string Name, bool IsActive);
}
