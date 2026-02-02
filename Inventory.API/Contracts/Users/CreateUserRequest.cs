namespace Inventory.API.Contracts.Users
{
    public sealed record CreateUserRequest(string Email, string Password, string Role);
}
