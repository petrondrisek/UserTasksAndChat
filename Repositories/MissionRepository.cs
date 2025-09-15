using UserTasksAndChat.Models;
using UserTasksAndChat.Data;
using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Dto.Mission;
using UserTasksAndChat.Extensions;
using UserTasksAndChat.Events;

namespace UserTasksAndChat.Repositories
{
    public interface IMissionRepository
    {
        Task<Mission?> GetMissionByIdAsync(Guid missionId);
        Task<Mission[]> GetMissionsByUserIdAsync(Guid userId, int offset, int limit, string? filterTag, bool completed = false);
        Task<Mission?> CreateMissionAsync(Mission mission);
        Task<Mission?> UpdateMissionAsync(Mission mission, UpdateMissionDto updateMissionDto);
        Task<bool> DeleteMissionAsync(Guid missionId);
        Task<int> GetMissionsCountByUserIdAsync(Guid userId, string? filterTag, bool completed = false);
    }

    public class MissionRepository : IMissionRepository
    {
        public readonly ApplicationDbContext _context;
        public readonly ILogger<MissionRepository> _logger;

        public MissionRepository(ApplicationDbContext context, ILogger<MissionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Mission?> CreateMissionAsync(Mission mission)
        {
            if (mission == null)
                return null;

            try
            {
                var userIds = BuildUserIdsList(mission);
                mission.Id = Guid.NewGuid();
                mission.AddDomainEvent(new CreateMissionEvent(mission.Id, userIds.ToArray()));

                _context.Missions.Add(mission);
                await _context.SaveChangesAsync();

                return mission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission with title {Title}", mission.Title);
                throw;
            }
        }

        public async Task<bool> DeleteMissionAsync(Guid missionId)
        {
            var mission = await GetMissionByIdAsync(missionId);
            if (mission == null)
                return false;

            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Mission?> GetMissionByIdAsync(Guid missionId)
        {
            var mission = await _context.Missions
                    .Include(m => m.User)
                    .Include(m => m.CreatedBy)
                    .FirstAsync(m => m.Id == missionId);

            if(mission != null)
            {
                await mission.LoadRelatedUsersAsync(_context);
            }

            return mission;
        }

        public async Task<Mission[]> GetMissionsByUserIdAsync(
            Guid userId,
            int offset = 0,
            int limit = 10,
            string? filterTag = null,
            bool completed = false
        )
        {
            var missions = _context.Missions
                .Where(m =>
                    m.UserId == userId ||
                    m.CreatedById == userId ||
                    m.RelatedUserIds.Contains(userId)
                 );

            if (filterTag != null)
                missions = missions.Where(m => string.IsNullOrEmpty(filterTag) || m.Tags.Contains(filterTag));

            missions = missions
                .Include(m => m.User)
                .Include(m => m.CreatedBy)
                .Where(m => m.IsCompleted == completed)
                .OrderBy(m => m.Deadline == null)
                .ThenBy(m => m.Deadline)
                .Skip(offset)
                .Take(limit);

            foreach (var mission in await missions.ToArrayAsync())
            {
                await mission.LoadRelatedUsersAsync(_context);
            }
            
            return await missions.ToArrayAsync();
        }

        public async Task<int> GetMissionsCountByUserIdAsync(
            Guid userId, 
            string? filterTag, 
            bool completed = false
        )
        {
            var missions = _context.Missions
                .Where(m =>
                    m.UserId == userId ||
                    m.CreatedById == userId ||
                    m.RelatedUserIds.Contains(userId)
                 );

            if(filterTag != null)
                missions = missions.Where(m => string.IsNullOrEmpty(filterTag) || m.Tags.Contains(filterTag));

            missions = missions.Where(m => m.IsCompleted == completed);

            return await missions.CountAsync();
        }

        public async Task<Mission?> UpdateMissionAsync(Mission mission, UpdateMissionDto updateMissionDto)
        {
            mission.Title = updateMissionDto.Title ?? mission.Title;
            mission.Description = updateMissionDto.Description ?? mission.Description;
            mission.Deadline = updateMissionDto.Deadline ?? mission.Deadline;
            mission.IsCompleted = updateMissionDto.IsCompleted ?? mission.IsCompleted;
            mission.UpdatedAt = DateTime.UtcNow;
            mission.Tags = updateMissionDto.Tags ?? mission.Tags;
            mission.Files = updateMissionDto.StoredFiles ?? mission.Files;
            mission.UserId = updateMissionDto.UserId ?? mission.UserId;
            mission.RelatedUserIds = updateMissionDto.RelatedUserIds ?? mission.RelatedUserIds;

            // Event
            var usersIds = BuildUserIdsList(mission);
            mission.AddDomainEvent(new UpdateMissionEvent(mission.Id, usersIds.ToArray()));

            // Save
            await _context.SaveChangesAsync();

            return mission;
        }

        private static List<Guid> BuildUserIdsList(Mission mission)
        {
            var users = new List<Guid> { mission.CreatedById };

            if (mission.UserId.HasValue && mission.UserId != Guid.Empty)
                users.Add(mission.UserId.Value);

            users.AddRange(mission.RelatedUserIds);

            return users.Distinct().ToList();
        }
    }
}
