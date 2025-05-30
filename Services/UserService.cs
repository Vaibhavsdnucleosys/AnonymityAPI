using AnonymityAPI.Data;
using AnonymityAPI.DTO;
using AnonymityAPI.Model;
using AnonymityAPI.Repositories;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AnonymityAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userrepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        private readonly ApplicationDbContext _context;

        public UserService(IRepository<User> userrepository, ApplicationDbContext context, IConfiguration configuration, IUserRepository userRepository)
        {
            _userrepository = userrepository;
            _context = context;
            _configuration = configuration;
            _userRepository = userRepository;
        }
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userrepository.GetAll();

            return users.Where(u => !u.IsDeleted).Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                IsDeleted = u.IsDeleted,
            }).ToList();
        }
        
        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userrepository.GetByIdAsync(id);

            if (user == null || user.IsDeleted)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                IsDeleted = user.IsDeleted
            };
        }

        public async Task<User> AddUserAsync(User user)
        {
            var existingUser = (await _userrepository.GetAll())
                .FirstOrDefault(u => u.Email == user.Email);

            if (existingUser != null && !existingUser.IsDeleted)
                throw new InvalidOperationException("User already registered");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            if (existingUser == null || existingUser.IsDeleted)
            {
                var newUser = new User
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    Password = hashedPassword,
                    Token = "",
                    IsDeleted = false,
                    GoogleId = null,
                    AuthProvider = "Email"
                };

                await _userrepository.Add(newUser);

                return new User
                {
                    Id = newUser.Id,
                    Email = newUser.Email,
                    UserName = newUser.UserName
                };
            }

            return null;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            var existingUser = await _userrepository.GetByIdAsync(user.Id);
            if (existingUser == null || existingUser.IsDeleted) return false;

            existingUser.Email = user.Email;
            existingUser.UserName = user.UserName;
            existingUser.Password = user.Password;
            existingUser.Token = user.Token;

            _userrepository.Update(existingUser);
            await _userrepository.SaveChangesAsync();
            return true;
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Tb_Users.FindAsync(id);
            if (user != null)
            {
                user.IsDeleted = true;
                _context.Tb_Users.Update(user);
                await _context.SaveChangesAsync();
                await _userrepository.SoftDelete(user);
            }
        }

        //public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        //{
        //    var user = (await _userrepository.GetAll())
        //        .FirstOrDefault(u => u.Email == loginDto.Email && !u.IsDeleted);

        //    if (user == null)
        //    {

        //        if (string.IsNullOrWhiteSpace(loginDto.Password))
        //        {
        //            return new LoginResponseDto
        //            {
        //                IsSuccess = false,
        //                Message = "Invalid Email and Password"
        //            };
        //        }

        //        return new LoginResponseDto
        //        {
        //            IsSuccess = false,
        //            Message = "Invalid Email"
        //        };
        //    }

        //    //if (user.AuthProvider != "Email")
        //    //{
        //    //    return new LoginResponseDto
        //    //    {
        //    //        IsSuccess = false,
        //    //        Message = "please login with Google"
        //    //    };
        //    //}

        //    if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
        //    {
        //        return new LoginResponseDto
        //        {
        //            IsSuccess = false,
        //            Message = "Invalid Password"
        //        };
        //    }

        //    var token = GenerateJwtToken(user);
        //    user.Token = token;
        //    await _userrepository.Update(user);

        //    return new LoginResponseDto
        //    {
        //        IsSuccess = true,
        //        Message = "Login Successful",
        //        Token = token
        //    };
        //}

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = (await _userrepository.GetAll())
                .FirstOrDefault(u => u.Email == loginDto.Email && !u.IsDeleted);

            if (user == null)
            {

                if (string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return new LoginResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid Email and Password"
                    };
                }

                return new LoginResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Email"
                };
            }

            if (user.AuthProvider != "Email")
            {
                return new LoginResponseDto
                {
                    IsSuccess = false,
                    Message = "please login with Google"
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                return new LoginResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Password"
                };
            }

            var token = GenerateJwtToken(user);
            user.Token = token;
            await _userrepository.Update(user);

            return new LoginResponseDto
            {
                IsSuccess = true,
                Message = "Login Successful",
                Token = token
            };
        }

        public async Task<AuthResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest request)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token);

            // First check if a user with this Google ID exists (only if GoogleId is not null)
            User user = null;
            if (!string.IsNullOrEmpty(payload.Subject))
            {
                user = await _userRepository.GetUserByGoogleIdAsync(payload.Subject);
            }

            if (user == null)
            {
                // If no user with Google ID, check if a user with this email exists
                user = await _userRepository.GetUserByEmailAsync(payload.Email);

                if (user != null)
                {
                    // User exists with this email but signed up differently - update with Google info
                    // Only update GoogleId if it's not already set
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = payload.Subject;
                        user.AuthProvider = "Google";
                        await _userRepository.SaveAsync();
                    }
                }
                else
                {
                    // Completely new user - create record
                    user = new User
                    {
                        Email = payload.Email,
                        GoogleId = payload.Subject, // This could be null if payload.Subject is null
                        UserName = payload.Name,
                        AuthProvider = "Google",
                        Password = null,
                        Token = null,
                        IsDeleted = false
                    };

                    await _userRepository.CreateUserAsync(user);
                }
            }

            // Generate token regardless of how we found/created the user
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = user
            };
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var expirationMinutes = Convert.ToInt32(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", user.Id.ToString()),
            new Claim("auth_provider", user.AuthProvider)
        }),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }




    }
}
