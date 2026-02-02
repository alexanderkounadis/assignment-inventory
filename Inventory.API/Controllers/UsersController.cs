using Inventory.API.Contracts.Users;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Inventory.Domain.Utilities.Enums;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public sealed class UsersController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        private readonly IPasswordHasher<AppUser> _hasher;

        public UsersController(InventoryDbContext db, IPasswordHasher<AppUser> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
        {
            var users = await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new UserResponse(u.Id, u.Email, u.Role.ToString(), u.IsActive))
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new { error = "Email, password, and role are required." });
            }

            if (!Enum.TryParse<AppRole>(request.Role, ignoreCase: true, out var role))
                return BadRequest(new { error = "Invalid role. Use Admin, Manager, or Clerk." });

            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (exists)
                return Conflict(new { error = "A user with this email already exists in this tenant." });

            var user = new AppUser
            {
                Email = email,
                Role = role,
                IsActive = true
                // TenantId will be set by your SaveChanges tenant rules
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var response = new UserResponse(user.Id, user.Email, user.Role.ToString(), user.IsActive);
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, response);
        }

    }
}
