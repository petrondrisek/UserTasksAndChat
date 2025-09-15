namespace UserTasksAndChat.Dto.Mission
{
    public class MissionDto
    {
        public UserTasksAndChat.Models.Mission Mission { get; set; } = null!;
        public UserTasksAndChat.Models.MissionChat[] Chat { get; set; } = Array.Empty<UserTasksAndChat.Models.MissionChat>();
    }
}