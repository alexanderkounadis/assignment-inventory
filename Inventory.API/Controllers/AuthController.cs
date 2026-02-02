using Inventory.API.Contracts.Auth;
using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(InventoryDbContext dbContext,
    IPasswordHasher<AppUser> hasher,
    IConfiguration config,
    ITenantProvider tenantProvider) : ControllerBase
    {
        private readonly InventoryDbContext _dbContext = dbContext;
        private readonly IPasswordHasher<AppUser> _hasher = hasher;
        private readonly IConfiguration _config = config;
        private readonly ITenantProvider _tenantProvider = tenantProvider;

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            // Tenant is already enforced by middleware extra defensive check here
            if (!_tenantProvider.HasTenant)
                return BadRequest(new { error = "Tenant is not resolved." });

            // Find user in *this tenant* (global filter will automatically apply)
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user is null)
                return Unauthorized(new { error = "Invalid credentials." });

            // Verify password
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { error = "Invalid credentials." });

            // Issue JWT
            var token = CreateJwt(user, _tenantProvider.TenantId);

            return Ok(new LoginResponse(token));
        }

        private string CreateJwt(AppUser user, Guid tenantId)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // "Admin" / "Manager" / "Clerk"
            new(ClaimTypes.Role, user.Role.ToString()),     
            // added tenant claim
            new("tenant", tenantId.ToString()),                  
            new(JwtRegisteredClaimNames.Email, user.Email)
        };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
