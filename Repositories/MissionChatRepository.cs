using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Data;
using UserTasksAndChat.Dto.Mission;
using UserTasksAndChat.Events;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Repositories
{
    public interface IMissionChatRepository
    {
        Task<MissionChat?> CreateMissionChatAsync(MissionChat missionChat);
        Task<MissionChat[]> GetMissionChatsByMissionIdAsync(Guid missionId, int offset, int limit);
        Task<int> GetMissionChatsCountByMissionIdAsync(Guid missionId);
        Task<MissionChat?> UpdateMissionChatAsync(Guid missionChatId, MissionChat updatedMissionChat);
        Task<bool> DeleteMissionChatAsync(Guid missionChatId);
        Task<MissionChat?> GetMissionChatByIdAsync(Guid missionChatId);
    }

    public class MissionChatRepository : IMissionChatRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MissionChatRepository> _logger;

        public MissionChatRepository(ApplicationDbContext context, ILogger<MissionChatRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MissionChat?> GetMissionChatByIdAsync(Guid missionChatId)
        {
            return await _context.MissionChats
                .AsNoTracking()
                .Include(mc => mc.User)
                .FirstAsync(mc => mc.Id == missionChatId);
        }

        public async Task<bool> DeleteMissionChatAsync(Guid missionChatId)
        {
            try
            {
                var missionChat = await _context.MissionChats.FindAsync(missionChatId);
                if (missionChat == null)
                    return false;
                _context.MissionChats.Remove(missionChat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mission chat {MissionChatId}", missionChatId);
                throw;
            }
        }

        public async Task<MissionChat?> CreateMissionChatAsync(MissionChat missionChat)
        {
            if (missionChat == null)
                return null;

            try
            {
                missionChat.AddDomainEvent(new CreateMissionChatEvent(missionChat.MissionId, missionChat.CreatedAt));

                _context.MissionChats.Add(missionChat);
                await _context.SaveChangesAsync();

                return missionChat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission chat for mission {MissionId}", missionChat.MissionId);
                throw;
            }
        }

        public async Task<MissionChat[]> GetMissionChatsByMissionIdAsync(Guid missionId, int offset, int limit)
        {
            return await _context.MissionChats
                .AsNoTracking()
                .Include(mc => mc.User)
                .Where(mc => mc.MissionId == missionId)
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToArrayAsync();
        }

        public async Task<int> GetMissionChatsCountByMissionIdAsync(Guid missionId)
        {
            return await _context.MissionChats
                .CountAsync(mc => mc.MissionId == missionId);
        }

        public async Task<MissionChat?> UpdateMissionChatAsync(Guid missionChatId, MissionChat updatedMissionChat)
        {
            try
            {
                var existingMissionChat = await _context.MissionChats.FindAsync(missionChatId);
                if (existingMissionChat == null)
                    return null;

                existingMissionChat.Message = updatedMissionChat.Message;
                existingMissionChat.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingMissionChat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission chat {MissionChatId}", missionChatId);
                throw;
            }
        }

        public async Task<bool> DeleteMissionAsync(Guid missionId)
        {
            try
            {
                var mission = await GetMissionByIdAsync(missionId);
                if (mission == null)
                    return false;

                _context.Missions.Remove(mission);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mission {MissionId}", missionId);
                throw;
            }
        }

        public async Task<Mission?> GetMissionByIdAsync(Guid missionId)
        {
            try
            {
                var mission = await _context.Missions
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Include(m => m.CreatedBy)
                    .FirstOrDefaultAsync(m => m.Id == missionId);

                if (mission != null)
                {
                    await mission.LoadRelatedUsersAsync(_context);
                }

                return mission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission {MissionId}", missionId);
                throw;
            }
        }

        public async Task<Mission[]> GetMissionsByUserIdAsync(
            Guid userId,
            int offset = 0,
            int limit = 10,
            string? filterTag = null,
            bool completed = false)
        {
            try
            {
                var query = BuildMissionsQuery(userId, filterTag, completed);

                var missions = await query
                    .Include(m => m.User)
                    .Include(m => m.CreatedBy)
                    .OrderBy(m => m.Deadline == null ? 1 : 0) // Null deadlines at the end
                    .ThenBy(m => m.Deadline)
                    .ThenByDescending(m => m.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .ToArrayAsync();

                var loadRelatedUsersTasks = missions.Select(m => m.LoadRelatedUsersAsync(_context));
                await Task.WhenAll(loadRelatedUsersTasks);

                return missions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving missions for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetMissionsCountByUserIdAsync(
            Guid userId,
            string? filterTag,
            bool completed = false)
        {
            try
            {
                var query = BuildMissionsQuery(userId, filterTag, completed);
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting missions for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Mission?> UpdateMissionAsync(Mission mission, UpdateMissionDto updateMissionDto)
        {
            try
            {
                mission.Title = updateMissionDto.Title ?? mission.Title;
                mission.Description = updateMissionDto.Description ?? mission.Description;
                mission.Deadline = updateMissionDto.Deadline ?? mission.Deadline;
                mission.IsCompleted = updateMissionDto.IsCompleted ?? mission.IsCompleted;
                mission.UpdatedAt = DateTime.UtcNow;
                mission.Tags = updateMissionDto.Tags ?? mission.Tags;
                mission.Files = updateMissionDto.StoredFiles ?? mission.Files;
                mission.RelatedUserIds = updateMissionDto.RelatedUserIds ?? mission.RelatedUserIds;

                var userIds = BuildUserIdsList(mission);
                mission.AddDomainEvent(new UpdateMissionEvent(mission.Id, userIds.ToArray()));

                await _context.SaveChangesAsync();
                return mission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission {MissionId}", mission.Id);
                throw;
            }
        }

        private IQueryable<Mission> BuildMissionsQuery(Guid userId, string? filterTag, bool completed)
        {
            var query = _context.Missions
                .AsNoTracking()
                .Where(m =>
                    m.UserId == userId ||
                    m.CreatedById == userId ||
                    m.RelatedUserIds.Contains(userId))
                .Where(m => m.IsCompleted == completed);

            if (!string.IsNullOrWhiteSpace(filterTag))
            {
                query = query.Where(m => m.Tags.Contains(filterTag));
            }

            return query;
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
