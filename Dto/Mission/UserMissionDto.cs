namespace UserTasksAndChat.Dto.Mission
{
    public class UserMissionDto
    {
        public UserTasksAndChat.Models.Mission[] Items { get; set; } = Array.Empty<UserTasksAndChat.Models.Mission>();
        public int TotalCount { get; set; } = 0;
        public DateTime[] LastVisits { get; set; } = Array.Empty<DateTime>();
    }
}
