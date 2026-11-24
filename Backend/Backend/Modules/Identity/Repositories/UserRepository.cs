using Backend.Infrastructure.Persistence;
using Backend.Modules.Identity.Interfaces;
using Backend.Modules.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Identity.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Explicitly bypasses global query filter to allow authenticating users across tenants or unassigned users
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            // Explicitly bypasses global query filter to verify global email uniqueness across all tenants
            return await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
