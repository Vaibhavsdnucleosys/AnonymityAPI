using AnonymityAPI.Data;
using AnonymityAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace AnonymityAPI.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Tb_Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByGoogleIdAsync(string googleId)
        {
            return await _context.Tb_Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _context.Tb_Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsByEmail(string email)
        {
            return await _context.Tb_Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UserExistsByGoogleId(string googleId)
        {
            return await _context.Tb_Users.AnyAsync(u => u.GoogleId == googleId);
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
