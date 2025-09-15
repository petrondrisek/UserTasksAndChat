namespace UserTasksAndChat.Models
{
    public class MissionLastVisit: Entity
    {
        public Guid Id { get; set; }
        public Guid MissionId { get; set; }
        public Mission? Mission { get; set; } = null;
        public Guid UserId { get; set; }
        public User? User { get; set; } = null;
        public DateTime LastVisitAt { get; set; }
    }
}
