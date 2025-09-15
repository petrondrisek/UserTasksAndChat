using UserTasksAndChat.Repositories;
using UserTasksAndChat.Models;
using UserTasksAndChat.Dto.MissionChat;

namespace UserTasksAndChat.Services
{
    public interface IMissionChatService
    {
        Task<MissionChat> AddMissionChatMessageAsync(MissionChatDto missionChatDto, User user);
        Task<MissionChat[]> GetMissionChatsByMissionIdAsync(Guid missionId, int offset, int limit);
        Task<MissionChat?> GetMissionChatByIdAsync(Guid messageId);
        Task<bool> DeleteMissionChatAsync(Guid messageId);
    }

    public class MissionChatService : IMissionChatService
    {
        private readonly IMissionChatRepository _missionChatRepository;
        private readonly ILogger<MissionChatService> _logger;

        private const int MinMessageLength = 3;
        private const int MaxMessageLength = 2000;

        public MissionChatService(
            IMissionChatRepository missionChatRepository,
            ILogger<MissionChatService> logger)
        {
            _missionChatRepository = missionChatRepository;
            _logger = logger;
        }

        public async Task<MissionChat> AddMissionChatMessageAsync(MissionChatDto missionChatDto, User user)
        {
            ValidateMessage(missionChatDto.Message);

            var missionChat = new MissionChat
            {
                Id = Guid.NewGuid(),
                MissionId = missionChatDto.MissionId,
                Message = missionChatDto.Message.Trim(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _missionChatRepository.CreateMissionChatAsync(missionChat);
            return created ?? throw new InvalidOperationException("Failed to create mission chat message");
        }

        public async Task<MissionChat[]> GetMissionChatsByMissionIdAsync(Guid missionId, int offset, int limit)
        {
            return await _missionChatRepository.GetMissionChatsByMissionIdAsync(missionId, offset, limit);
        }

        public async Task<MissionChat?> GetMissionChatByIdAsync(Guid messageId)
        {
            return await _missionChatRepository.GetMissionChatByIdAsync(messageId);
        }

        public async Task<bool> DeleteMissionChatAsync(Guid messageId)
        {
            return await _missionChatRepository.DeleteMissionChatAsync(messageId);
        }

        private static void ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            if (message.Length < MinMessageLength)
                throw new ArgumentException($"Message must be at least {MinMessageLength} characters long", nameof(message));

            if (message.Length > MaxMessageLength)
                throw new ArgumentException($"Message cannot exceed {MaxMessageLength} characters", nameof(message));
        }
    }
}
