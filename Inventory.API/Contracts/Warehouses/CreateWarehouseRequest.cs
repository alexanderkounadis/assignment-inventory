namespace Inventory.API.Contracts.Warehouses
{
    public sealed record CreateWarehouseRequest(string Name, bool IsActive = true);

}
