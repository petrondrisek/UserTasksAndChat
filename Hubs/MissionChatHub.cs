using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using UserTasksAndChat.Dto.MissionChat;
using UserTasksAndChat.Models;
using UserTasksAndChat.Services;

namespace UserTasksAndChat.Hubs
{
    [Authorize]
    public class MissionChatHub(
        IUserService _userService,
        IMissionService _missionService,
        IMissionChatService _missionChatService,
        ILogger<MissionChatHub> _logger
    ) : Hub
    {
        // Cache user
        private static readonly ConcurrentDictionary<string, User> _userCache = new();
        private static readonly ConcurrentDictionary<string, DateTime> _userCacheExpiry = new();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

        public async Task SendMessageToMission(Guid missionId, string message)
        {
            try
            {
                ValidateMessageInput(message);

                var (mission, user) = await ValidateUserMissionAccessAsync(missionId);

                var missionChatDto = new MissionChatDto { MissionId = missionId, Message = message.Trim() };
                var chatMessage = await _missionChatService.AddMissionChatMessageAsync(missionChatDto, user);

                await Clients.Group(GetMissionGroupName(missionId))
                    .SendAsync("ReceiveMessage", chatMessage);

                _logger.LogInformation("Message sent to mission {MissionId} by user {UserId}", missionId, user.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid message input for mission {MissionId}: {Error}", missionId, ex.Message);
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending message to mission {MissionId}", missionId);
                throw new HubException("Unexpected error occurred while sending message");
            }
        }

        public async Task DeleteMessage(Guid missionId, Guid messageId)
        {
            try
            {
                var (mission, user) = await ValidateUserMissionAccessAsync(missionId);

                var message = await _missionChatService.GetMissionChatByIdAsync(messageId) 
                        ?? throw new HubException("Message not found");

                // Kontrola oprávnění pro smazání zprávy
                if (!CanUserDeleteMessage(message, user, mission))
                    throw new HubException("You are not allowed to delete this message");

                await _missionChatService.DeleteMissionChatAsync(messageId);
                await Clients.Group(GetMissionGroupName(missionId))
                    .SendAsync("DeleteMessage", messageId);

                _logger.LogInformation("Message {MessageId} deleted from mission {MissionId} by user {UserId}",
                    messageId, missionId, user.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting message {MessageId} from mission {MissionId}",
                    messageId, missionId);
                throw new HubException("Unexpected error occurred while deleting message");
            }
        }

        public async Task JoinMissionGroup(Guid missionId)
        {
            try
            {
                var (mission, user) = await ValidateUserMissionAccessAsync(missionId);

                await Groups.AddToGroupAsync(Context.ConnectionId, GetMissionGroupName(missionId));

                _logger.LogInformation("User {UserId} joined mission group {MissionId} with connection {ConnectionId}",
                    user.Id, missionId, Context.ConnectionId);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error joining mission group {MissionId} for user {Username}",
                    missionId, Context.User?.Identity?.Name);
                throw new HubException("Connection failed, unable to join mission chat");
            }
        }

        public async Task LeaveMissionGroup(Guid missionId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetMissionGroupName(missionId));

                _logger.LogInformation("Connection {ConnectionId} left mission group {MissionId}",
                    Context.ConnectionId, missionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving mission group {MissionId} for connection {ConnectionId}",
                    missionId, Context.ConnectionId);
                throw new HubException("Unexpected error occurred while leaving group");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Clear cache on disconnect
            _userCache.TryRemove(Context.ConnectionId, out _);
            _userCacheExpiry.TryRemove(Context.ConnectionId, out _);

            if (exception != null)
            {
                _logger.LogWarning(exception, "User disconnected with error. Connection: {ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task<(Mission mission, User user)> ValidateUserMissionAccessAsync(Guid missionId)
        {
            var user = await GetCurrentUserAsync()
                ?? throw new HubException("User not found - please log in again");

            var mission = await _missionService.GetMissionByIdAsync(missionId)
                    ?? throw new HubException("Mission not found");

            if (!_missionService.HasUserAccessToMissionAsync(mission, user))
                throw new HubException("You are not authorized to access this mission");

            return (mission, user);
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            var cacheKey = Context.ConnectionId;
            if (_userCache.TryGetValue(cacheKey, out var cachedUser) &&
                _userCacheExpiry.TryGetValue(cacheKey, out var expiry) &&
                DateTime.UtcNow < expiry)
            {
                return cachedUser;
            }

            var user = await _userService.GetUserAsync(username);
            if (user != null)
            {
                _userCache.TryAdd(cacheKey, user);
                _userCacheExpiry.TryAdd(cacheKey, DateTime.UtcNow.Add(CacheExpiry));
            }

            return user;
        }

        private static void ValidateMessageInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty");

            if (message.Length > 2000)
                throw new ArgumentException("Message is too long (maximum 2000 characters)");
        }

        private static bool CanUserDeleteMessage(MissionChat message, User user, Mission mission)
        {
            return message.UserId == user.Id || mission.CreatedById == user.Id;
        }

        private static string GetMissionGroupName(Guid missionId)
        {
            return $"mission_{missionId}";
        }

        private static readonly Timer _cacheCleanupTimer = new(CleanupExpiredCache, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        private static void CleanupExpiredCache(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _userCacheExpiry
                .Where(kvp => now >= kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _userCache.TryRemove(key, out _);
                _userCacheExpiry.TryRemove(key, out _);
            }
        }
    }
}