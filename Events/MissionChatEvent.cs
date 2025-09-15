namespace UserTasksAndChat.Events
{
    public record CreateMissionChatEvent(Guid missionId, DateTime dateTime): IDomainEvent;
}
