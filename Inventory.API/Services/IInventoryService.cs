using Inventory.API.Contracts.Inventory;

namespace Inventory.API.Services
{
    public interface IInventoryService
    {
        Task<MovementResponse> CreateMovementAsync(CreateMovementRequest request, CancellationToken ct = default);
    }
}
