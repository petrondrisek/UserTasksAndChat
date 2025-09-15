using UserTasksAndChat.Dto.Mission;
using UserTasksAndChat.Events;
using UserTasksAndChat.Models;
using UserTasksAndChat.Repositories;

namespace UserTasksAndChat.Services
{
    public interface IMissionService
    {
        Task<Mission?> GetMissionByIdAsync(Guid missionId);
        Task<Mission[]> GetMissionsByUserAsync(User user, int offset, int limit, string? filterTag = null, bool completed = false);
        Task<(Mission[] missions, int totalCount)> GetUserMissionsWithCountAsync(User user, int offset, int limit, string? filterTag = null, bool completed = false);
        Task<int> GetMissionCountByUserAsync(User user, string? filterTag = null, bool completed = false);
        Task<Mission> CreateMissionAsync(CreateMissionDto createMissionDto, User user, string[] uploadedFiles);
        Task<Mission> UpdateMissionAsync(Mission mission, UpdateMissionDto updateMissionDto);
        Task<Mission> UpdateMissionWithFilesAsync(Mission mission, UpdateMissionDto updateMissionDto, IFormFileCollection files);
        bool HasUserAccessToMissionAsync(Mission mission, User user);
    }

    public class MissionService(
        IMissionRepository _missionRepository,
        IFilesService _filesService,
        ILogger<MissionService> _logger
    ) : IMissionService, IDomainEventHandler<CreateMissionChatEvent>
    {
        public async Task<Mission?> GetMissionByIdAsync(Guid missionId)
        {
            if (missionId == Guid.Empty)
                return null;

            return await _missionRepository.GetMissionByIdAsync(missionId);
        }

        public async Task<Mission[]> GetMissionsByUserAsync(User user, int offset = 0, int limit = 10, string? filterTag = null, bool completed = false)
        {
            return await _missionRepository.GetMissionsByUserIdAsync(user.Id, offset, limit, filterTag, completed);
        }

        public async Task<(Mission[] missions, int totalCount)> GetUserMissionsWithCountAsync(User user, int offset, int limit, string? filterTag = null, bool completed = false)
        {
            var missionsTask = GetMissionsByUserAsync(user, offset, limit, filterTag, completed);
            var countTask = GetMissionCountByUserAsync(user, filterTag, completed);

            await Task.WhenAll(missionsTask, countTask);

            return (await missionsTask, await countTask);
        }

        public async Task<int> GetMissionCountByUserAsync(User user, string? filterTag = null, bool completed = false)
        {
            return await _missionRepository.GetMissionsCountByUserIdAsync(user.Id, filterTag, completed);
        }

        public async Task<Mission> CreateMissionAsync(CreateMissionDto createMissionDto, User user, string[] uploadedFiles)
        {
            ValidateCreateMissionDto(createMissionDto);

            var mission = new Mission
            {
                Title = createMissionDto.Title,
                Description = createMissionDto.Description ?? string.Empty,
                Deadline = createMissionDto.Deadline,
                UserId = createMissionDto.UserId,
                CreatedById = user.Id,
                RelatedUserIds = createMissionDto.RelatedUserIds ?? [],
                Tags = createMissionDto.Tags ?? [],
                Files = uploadedFiles
            };

            var createdMission = await _missionRepository.CreateMissionAsync(mission);
            return createdMission ?? throw new InvalidOperationException("Failed to create mission");
        }

        public async Task<Mission> UpdateMissionWithFilesAsync(Mission mission, UpdateMissionDto updateMissionDto, IFormFileCollection files)
        {
            // Správa souborů
            var filesToDelete = mission.Files.Except(updateMissionDto.StoredFiles ?? []).ToArray();
            if (filesToDelete.Length > 0)
            {
                _filesService.DeleteMultipleFiles(filesToDelete);
                _logger.LogInformation("Deleted {Count} files for mission {MissionId}", filesToDelete.Length, mission.Id);
            }

            var uploadedFiles = await _filesService.UploadMultipleFormFilesAsync(files);
            updateMissionDto.StoredFiles = [..mission.Files.Except(filesToDelete).Concat(uploadedFiles)];

            return await UpdateMissionAsync(mission, updateMissionDto);
        }

        public async Task<Mission> UpdateMissionAsync(Mission mission, UpdateMissionDto updateMissionDto)
        {
            ValidateUpdateMissionDto(updateMissionDto);

            var updatedMission = await _missionRepository.UpdateMissionAsync(mission, updateMissionDto);
            return updatedMission ?? throw new InvalidOperationException("Failed to update mission");
        }

        public bool HasUserAccessToMissionAsync(Mission mission, User user)
        {
            return mission.UserId == user.Id ||
                   mission.CreatedBy?.Id == user.Id ||
                   (mission.RelatedUsers?.Any(u => u.Id == user.Id) ?? false);
        }

        public async Task OnEventDispatch(CreateMissionChatEvent domainEvent, CancellationToken cancellationToken)
        {
            var mission = await _missionRepository.GetMissionByIdAsync(domainEvent.missionId);
            if (mission != null)
            {
                mission.LastChatMessageAt = domainEvent.dateTime;
            }
        }

        private static void ValidateCreateMissionDto(CreateMissionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Mission title cannot be empty", nameof(dto));
        }

        private static void ValidateUpdateMissionDto(UpdateMissionDto dto)
        {
            if (dto.Id == Guid.Empty)
                throw new ArgumentException("Mission ID cannot be empty", nameof(dto));
        }
    }
}
