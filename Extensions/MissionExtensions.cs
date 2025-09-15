using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Models;

public static class MissionExtensions
{
    public static async Task LoadRelatedUsersAsync(this Mission mission, DbContext context)
    {
        if (mission.RelatedUserIds.Length > 0)
        {
            mission.RelatedUsers = await context.Set<User>()
                .Where(u => mission.RelatedUserIds.Contains(u.Id))
                .ToListAsync();
        }
    }

    public static void AddRelatedUser(this Mission mission, User user)
    {
        if (!mission.RelatedUserIds.Contains(user.Id))
        {
            mission.RelatedUserIds = mission.RelatedUserIds.Append(user.Id).ToArray();
            if (!mission.RelatedUsers.Any(u => u.Id == user.Id))
            {
                mission.RelatedUsers.Add(user);
            }
        }
    }

    public static void AddRelatedUser(this Mission mission, Guid userId)
    {
        if (!mission.RelatedUserIds.Contains(userId))
        {
            mission.RelatedUserIds = mission.RelatedUserIds.Append(userId).ToArray();
        }
    }

    public static void RemoveRelatedUser(this Mission mission, User user)
    {
        mission.RelatedUserIds = mission.RelatedUserIds.Where(id => id != user.Id).ToArray();
        var existingUser = mission.RelatedUsers.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser != null)
        {
            mission.RelatedUsers.Remove(existingUser);
        }
    }

    public static void RemoveRelatedUser(this Mission mission, Guid userId)
    {
        mission.RelatedUserIds = mission.RelatedUserIds.Where(id => id != userId).ToArray();
        var existingUser = mission.RelatedUsers.FirstOrDefault(u => u.Id == userId);
        if (existingUser != null)
        {
            mission.RelatedUsers.Remove(existingUser);
        }
    }
}