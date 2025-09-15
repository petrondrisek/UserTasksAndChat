using UserTasksAndChat.Events;
using UserTasksAndChat.Models;
using UserTasksAndChat.Repositories;

namespace UserTasksAndChat.Services
{
    public interface IMissionLastVisitService
    {
        Task<DateTime?> GetUserLastVisitAsync(Guid missionId, Guid userId);
        Task<MissionLastVisit> UpdateUserLastVisitAsync(Guid missionId, Guid userId);
    }

    public class MissionLastVisitService: IMissionLastVisitService, IDomainEventHandler<CreateMissionEvent>, IDomainEventHandler<UpdateMissionEvent>
    {
        private readonly IMissionLastVisitRepository _missionLastVisitRepository;
        private readonly ILogger<MissionLastVisitService> _logger;

        public MissionLastVisitService(
            IMissionLastVisitRepository missionLastVisitRepository, 
            ILogger<MissionLastVisitService> logger
        )
        {
            _missionLastVisitRepository = missionLastVisitRepository;
            _logger = logger;
        }

        public async Task<DateTime?> GetUserLastVisitAsync(Guid missionId, Guid userId)
        {
            return await _missionLastVisitRepository.GetUserLastVisitAsync(missionId, userId);
        }

        public async Task<MissionLastVisit> UpdateUserLastVisitAsync(Guid missionId, Guid userId)
        {
            var lastVisit = await _missionLastVisitRepository.UpdateUserLastVisitAsync(missionId, userId);
            if (lastVisit == null)
            {
                _logger.LogWarning("No existing last visit found for mission {MissionId} and user {UserId}", missionId, userId);
                throw new KeyNotFoundException("No existing last visit found to update");
            }
            return lastVisit;
        }

        public async Task OnEventDispatch(CreateMissionEvent mission, CancellationToken cancellationToken)
        {
            await _missionLastVisitRepository.UpdateMissionLastVisitsAsync(mission.missionId, mission.userIds);
        }

        public async Task OnEventDispatch(UpdateMissionEvent mission, CancellationToken cancellationToken)
        {
            await _missionLastVisitRepository.UpdateMissionLastVisitsAsync(mission.missionId, mission.userIds);
        }
    }
}
