using UserTasksAndChat.Models;
using UserTasksAndChat.Data;
using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Dto.User;

namespace UserTasksAndChat.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> CreateUserAsync(User user);
        Task<User?> UpdateUserAsync(Guid userId, UserUpdateDto userUpdateDto);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<User[]> GetUsersAsync(int offset, int limit, string? search = null);
        Task<int> GetUsersCountAsync(string? search);
        Task<bool> ExistsAsync(Guid userId);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsByEmailAsync(string email);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> CreateUserAsync(User user)
        {
            if (user == null)
                return null;

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", user.Username);
                throw;
            }
        }

        public async Task<User?> UpdateUserAsync(Guid userId, UserUpdateDto userUpdateDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return null;

                UpdateUserProperties(user, userUpdateDto);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                throw;
            }
        }

        public async Task<User[]> GetUsersAsync(int offset, int limit, string? search)
        {
            var query = BuildUsersSearchQuery(search);

            return await query
                .AsNoTracking()
                .OrderBy(u => u.Username)
                .Skip(offset)
                .Take(limit)
                .ToArrayAsync();
        }

        public async Task<int> GetUsersCountAsync(string? search)
        {
            var query = BuildUsersSearchQuery(search);
            return await query.CountAsync();
        }

        public async Task<bool> ExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        private IQueryable<User> BuildUsersSearchQuery(string? search)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchLower) ||
                    (u.FirstName ?? string.Empty).ToLower().Contains(searchLower) ||
                    (u.LastName ?? string.Empty).ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            return query;
        }

        private static void UpdateUserProperties(User user, UserUpdateDto userUpdateDto)
        {
            if (userUpdateDto.ResetPassword)
                user.Password = string.Empty;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
                user.Password = userUpdateDto.Password;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.FirstName))
                user.FirstName = userUpdateDto.FirstName;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.LastName))
                user.LastName = userUpdateDto.LastName;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Email))
                user.Email = userUpdateDto.Email;

            if (userUpdateDto.Permissions?.Any() == true)
                user.Permissions = userUpdateDto.Permissions;
        }
    }
}
