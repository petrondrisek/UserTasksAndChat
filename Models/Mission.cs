using System.ComponentModel.DataAnnotations.Schema;

namespace UserTasksAndChat.Models
{
    public class Mission: Entity
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? Deadline { get; set; } = null;
        public bool IsCompleted { get; set; } = false;
        public Guid? UserId { get; set; } = null;
        public User? User { get; set; } = null;
        public Guid CreatedById { get; set; }
        public User? CreatedBy { get; set; } = null;
        public Guid[] RelatedUserIds { get; set; } = Array.Empty<Guid>();
        public IList<string> Tags { get; set; } = new List<string>();
        public IList<string> Files { get; set; } = new List<string>();
        public IList<MissionChat> Messages { get; set; } = new List<MissionChat>();
        [NotMapped]
        public IList<User> RelatedUsers { get; set; } = new List<User>();
        public DateTime? LastChatMessageAt { get; set; } = null;
    }
}
