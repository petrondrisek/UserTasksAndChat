using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Data;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Repositories
{
    public interface IMissionLastVisitRepository
    {
        Task<DateTime?> GetUserLastVisitAsync(Guid missionId, Guid userId);
        Task<MissionLastVisit?> UpdateUserLastVisitAsync(Guid missionId, Guid userId);
        Task<MissionLastVisit[]> UpdateMissionLastVisitsAsync(Guid missionId, Guid[] userIds);
        Task<Dictionary<Guid, DateTime?>> GetLastVisitsForMissionsAsync(Guid[] missionIds, Guid userId);
        Task<bool> DeleteMissionChatAsync(Guid missionChatId);
        Task<MissionChat?> GetMissionChatByIdAsync(Guid missionChatId);
    }

    public class MissionLastVisitRepository : IMissionLastVisitRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MissionLastVisitRepository> _logger;

        public MissionLastVisitRepository(ApplicationDbContext context, ILogger<MissionLastVisitRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DateTime?> GetUserLastVisitAsync(Guid missionId, Guid userId)
        {
            var lastVisit = await _context.MissionLastVisits
                .AsNoTracking()
                .FirstOrDefaultAsync(mlv => mlv.MissionId == missionId && mlv.UserId == userId);

            return lastVisit?.LastVisitAt;
        }

        public async Task<MissionLastVisit?> UpdateUserLastVisitAsync(Guid missionId, Guid userId)
        {
            try
            {
                var lastVisit = await _context.MissionLastVisits
                    .FirstOrDefaultAsync(mlv => mlv.MissionId == missionId && mlv.UserId == userId);

                if (lastVisit == null)
                {
                    _logger.LogWarning("No existing last visit found for mission {MissionId} and user {UserId}",
                        missionId, userId);
                    return null;
                }

                lastVisit.LastVisitAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return lastVisit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last visit for mission {MissionId} and user {UserId}",
                    missionId, userId);
                throw;
            }
        }

        public async Task<MissionLastVisit[]> UpdateMissionLastVisitsAsync(Guid missionId, Guid[] userIds)
        {
            try
            {
                var existingVisits = await _context.MissionLastVisits
                    .Where(mlv => mlv.MissionId == missionId && !userIds.Contains(mlv.UserId))
                    .ToListAsync();

                if (existingVisits.Count > 0)
                {
                    _context.MissionLastVisits.RemoveRange(existingVisits);
                }

                var existingUserIds = await _context.MissionLastVisits
                    .Where(mlv => mlv.MissionId == missionId)
                    .Select(mlv => mlv.UserId)
                    .ToListAsync();

                var newUserIds = userIds.Except(existingUserIds);
                var newVisits = newUserIds.Select(userId => new MissionLastVisit
                {
                    Id = Guid.NewGuid(),
                    MissionId = missionId,
                    UserId = userId,
                    LastVisitAt = DateTime.UtcNow
                }).ToList();

                if (newVisits.Count > 0)
                {
                    _context.MissionLastVisits.AddRange(newVisits);
                }

                return newVisits.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission last visits for mission {MissionId}", missionId);
                throw;
            }
        }

        public async Task<Dictionary<Guid, DateTime?>> GetLastVisitsForMissionsAsync(Guid[] missionIds, Guid userId)
        {
            var lastVisits = await _context.MissionLastVisits
                .AsNoTracking()
                .Where(mlv => missionIds.Contains(mlv.MissionId) && mlv.UserId == userId)
                .ToDictionaryAsync(mlv => mlv.MissionId, mlv => (DateTime?)mlv.LastVisitAt);

            foreach (var missionId in missionIds)
            {
                if (!lastVisits.ContainsKey(missionId))
                {
                    lastVisits[missionId] = null;
                }
            }

            return lastVisits;
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

        public async Task<MissionChat?> GetMissionChatByIdAsync(Guid missionChatId)
        {
            return await _context.MissionChats
                .AsNoTracking()
                .Include(mc => mc.User)
                .FirstOrDefaultAsync(mc => mc.Id == missionChatId);
        }
    }
}
