namespace UserTasksAndChat.Dto.User
{
    public class UserLoggedInDto
    {
        public string Token { get; set; } = string.Empty;
        public bool IsPasswordSet { get; set; } = false;

        public string RefreshToken { get; set; } = string.Empty;

        public int RefreshTokenValidHours { get; set; } = 60;
    }
}
