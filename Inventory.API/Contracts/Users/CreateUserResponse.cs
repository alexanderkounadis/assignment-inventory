namespace Inventory.API.Contracts.Users
{
    public sealed record UserResponse(int Id, string Email, string Role, bool IsActive);

}
