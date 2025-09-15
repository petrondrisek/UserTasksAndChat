using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Data;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Repositories
{
    public interface IUserRefreshTokenRepository
    {
        Task<UserRefreshToken?> GetUserRefreshTokenAsync(string token);
        Task<int> GetUserValidTokensCountAsync(User user);
        Task<bool> CreateRefreshTokenAsync(User user, string token, int validUntilHours = 24);
        Task<int> DeleteExpiredUserRefreshTokensAsync(User user);
        Task<bool> IsRefreshTokenValidAsync(User user, string token);
        Task<bool> DeleteUserRefreshTokenAsync(string token);
    }

    public class UserRefreshTokenRepository : IUserRefreshTokenRepository
    {
        private readonly ILogger<UserRefreshTokenRepository> _logger;
        private readonly ApplicationDbContext _context;

        public UserRefreshTokenRepository(ApplicationDbContext context, ILogger<UserRefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserRefreshToken?> GetUserRefreshTokenAsync(string token)
        {
            try
            {
                return await _context.UserRefreshTokens
                    .AsNoTracking()
                    .Include(u => u.User)
                    .FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving refresh token");
                return null;
            }
        }

        public async Task<int> GetUserValidTokensCountAsync(User user)
        {
            return await _context.UserRefreshTokens
                .CountAsync(t => t.UserId == user.Id && t.ValidUntil > DateTime.UtcNow);
        }

        public async Task<bool> CreateRefreshTokenAsync(User user, string token, int validUntilHours = 24)
        {
            try
            {
                var userToken = new UserRefreshToken
                {
                    UserId = user.Id,
                    Token = token,
                    ValidUntil = DateTime.UtcNow.AddHours(validUntilHours)
                };

                _context.UserRefreshTokens.Add(userToken);
                var saved = await _context.SaveChangesAsync();

                return saved > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refresh token for user {UserId}", user.Id);
                return false;
            }
        }

        public async Task<int> DeleteExpiredUserRefreshTokensAsync(User user)
        {
            try
            {
                var expiredTokens = await _context.UserRefreshTokens
                    .Where(t => t.UserId == user.Id && t.ValidUntil < DateTime.UtcNow)
                    .ToArrayAsync();

                if (expiredTokens.Length == 0)
                    return 0;

                _context.UserRefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} expired tokens for user {UserId}",
                    expiredTokens.Length, user.Id);

                return expiredTokens.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expired tokens for user {UserId}", user.Id);
                throw;
            }
        }

        public async Task<bool> IsRefreshTokenValidAsync(User user, string token)
        {
            return await _context.UserRefreshTokens
                .AnyAsync(t => t.UserId == user.Id &&
                              t.Token == token &&
                              t.ValidUntil > DateTime.UtcNow);
        }

        public async Task<bool> DeleteUserRefreshTokenAsync(string token)
        {
            try
            {
                var refreshToken = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (refreshToken == null)
                    return false;

                _context.UserRefreshTokens.Remove(refreshToken);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting refresh token");
                throw;
            }
        }
    }
}
