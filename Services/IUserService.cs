using AnonymityAPI.DTO;
using AnonymityAPI.Model;

namespace AnonymityAPI.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int id);
        Task<User> AddUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
       

        Task<AuthResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest request);
       
        Task DeleteUserAsync(int id);
    }
}
