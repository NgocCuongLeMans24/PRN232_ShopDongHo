namespace ClientSide.DataDtos
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; } 
        public string Email { get; set; }
        public string FullName { get; set; }
    }
}
