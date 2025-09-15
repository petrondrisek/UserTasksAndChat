using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.Token
{
    public class RefreshTokenDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
