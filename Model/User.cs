namespace AnonymityAPI.Model
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string? GoogleId { get; set; }
        public string UserName { get; set; }

        public string? Password { get; set; }

        public string? Token { get; set; }

        public string AuthProvider { get; set; }

        public bool IsDeleted { get; set; }
    }
}

