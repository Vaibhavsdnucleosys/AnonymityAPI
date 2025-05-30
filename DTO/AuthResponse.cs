using AnonymityAPI.Model;

namespace AnonymityAPI.DTO
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public User User { get; set; }
    }
}
