using UserTasksAndChat.Dto.User;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Extensions
{
    public static class UserExtensions
    {
        public static UserInfoDto ToInfoDto(this User user)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Permissions = user.Permissions
            };
        }

        public static bool HasPermission(this User user, UserPermissions permission)
        {
            return user?.Permissions?.Contains(permission) == true;
        }
    }
}
