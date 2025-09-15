using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserTasksAndChat.Services;
using UserTasksAndChat.Models;
using UserTasksAndChat.Dto.Mission;
using UserTasksAndChat.Extensions;

namespace UserTasksAndChat.Controllers
{
    [Route("api/mission")]
    [ApiController]
    [Authorize]
    public class MissionController(
        IUserService _userService,
        IMissionService _missionService,
        IMissionChatService _missionChatService,
        IMissionLastVisitService _missionLastVisitService,
        IFilesService _filesService,
        ILogger<MissionController> _logger
    ) : ControllerBase
    {
        [HttpGet("list")]
        public async Task<ActionResult<UserMissionDto>> GetUserMissions(
            [FromQuery] int limit = 10,
            [FromQuery] int offset = 0,
            [FromQuery] string? filterTag = null,
            [FromQuery] bool completed = false)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found, please log in." });

            try
            {
                var (missions, count) = await _missionService.GetUserMissionsWithCountAsync(
                    user, offset, limit, filterTag, completed);

                var lastVisits = await GetLastVisitsForMissions(missions, user.Id);

                return Ok(new UserMissionDto
                {
                    Items = missions,
                    TotalCount = count,
                    LastVisits = lastVisits
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving missions for user {UserId}", user.Id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("detail/{id:guid}")]
        public async Task<ActionResult<MissionDto>> GetMission(
            Guid id,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 5)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found, please log in." });

            try
            {
                var mission = await _missionService.GetMissionByIdAsync(id);
                if (mission == null)
                    return NotFound(new { message = "Mission not found." });

                if (!_missionService.HasUserAccessToMissionAsync(mission, user))
                    return StatusCode(403, new { message = "You're not related to this mission, access denied." });

                await _missionLastVisitService.UpdateUserLastVisitAsync(mission.Id, user.Id);
                var chat = await _missionChatService.GetMissionChatsByMissionIdAsync(mission.Id, offset, limit);

                return Ok(new MissionDto { Mission = mission, Chat = chat });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission {MissionId} for user {UserId}", id, user.Id);
                return StatusCode(500, new { message = "Internal server error"});
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult<Mission>> CreateMission(
            [FromForm] CreateMissionDto createMissionDto,
            [FromForm] IFormFileCollection files)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found, please log in." });

            if (!user.HasPermission(UserPermissions.ManageMissions))
                return StatusCode(403, new { message = "Permission denied to manage missions" });

            try
            {
                var uploadedFiles = await _filesService.UploadMultipleFormFilesAsync(files);
                var createdMission = await _missionService.CreateMissionAsync(createMissionDto, user, uploadedFiles);

                return CreatedAtAction(
                    nameof(GetMission),
                    new { id = createdMission.Id },
                    createdMission);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid mission data provided by user {UserId}", user.Id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission by user {UserId}", user.Id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("update")]
        public async Task<ActionResult<Mission>> UpdateMission(
            [FromForm] UpdateMissionDto updateMissionDto,
            [FromForm] IFormFileCollection files)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            if (!user.HasPermission(UserPermissions.ManageMissions))
                return StatusCode(403, new { message = "Permission denied to manage missions" });

            try
            {
                var mission = await _missionService.GetMissionByIdAsync(updateMissionDto.Id);
                if (mission == null || mission.CreatedById != user.Id)
                    return NotFound(new { message = "Mission not found or access denied" });

                var updatedMission = await _missionService.UpdateMissionWithFilesAsync(
                    mission, updateMissionDto, files);

                return Ok(updatedMission);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid update data for mission {MissionId}", updateMissionDto.Id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission {MissionId}", updateMissionDto.Id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("update/last-visit/{id}")]
        public async Task<ActionResult<MissionLastVisit?>> UpdateUserLastVisit(Guid id)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var mission = await _missionService.GetMissionByIdAsync(id);
                if (mission == null)
                    return NotFound(new { message = "Mission not found" });

                if (!_missionService.HasUserAccessToMissionAsync(mission, user))
                    return StatusCode(403, new { message = "You're not related to this mission, access denied." });

                return await _missionLastVisitService.UpdateUserLastVisitAsync(id, user.Id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "You're last activity was not found, couldn't be updated." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("Invalid arguments for last user visit on mission: {missionId}, exception: {ExceptionMessage}", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to update last user visit on mission: {MissionId}, exception: {ExceptionMessage}", id, ex.Message);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            return await _userService.GetUserAsync(username);
        }

        private async Task<DateTime[]> GetLastVisitsForMissions(Mission[] missions, Guid userId)
        {
            var lastVisitTasks = missions
                .Select(m => _missionLastVisitService.GetUserLastVisitAsync(m.Id, userId))
                .ToArray();

            var lastVisits = await Task.WhenAll(lastVisitTasks);
            var lastVisitsSelect = lastVisits.Select(visit => visit ?? DateTime.MinValue);

            return [..lastVisitsSelect];
        }
    }
}