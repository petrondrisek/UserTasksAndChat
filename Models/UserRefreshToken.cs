namespace UserTasksAndChat.Models
{
    public class UserRefreshToken: Entity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required string Token { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntil { get; set; } = new DateTime().AddHours(24);

        public User? User { get; set; }
    }
}
