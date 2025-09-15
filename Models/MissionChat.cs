namespace UserTasksAndChat.Models
{
    public class MissionChat: Entity
    {
        public Guid Id { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public Guid MissionId { get; set; }
    }
}
