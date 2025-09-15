namespace UserTasksAndChat.Events
{
    public record CreateMissionEvent(Guid missionId, Guid[] userIds): IDomainEvent;
    public record UpdateMissionEvent(Guid missionId, Guid[] userIds): IDomainEvent;
}
