namespace Inventory.API.Contracts.Auth
{
    public sealed record LoginRequest(string Email, string Password);
}
