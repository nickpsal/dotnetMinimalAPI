namespace MinimalAPI.Models.Domain
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string Role { get; set; } = "User";
        public required string Password { get; set; }
    }
}