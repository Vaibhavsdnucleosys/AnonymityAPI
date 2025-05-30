using AnonymityAPI.Model;

namespace AnonymityAPI.Services
{
    public interface IUserRepository
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByGoogleIdAsync(string googleId);
        Task<User> CreateUserAsync(User user);
        Task<bool> UserExistsByEmail(string email);
        Task<bool> UserExistsByGoogleId(string googleId);
        Task<bool> SaveAsync();
    }
}
