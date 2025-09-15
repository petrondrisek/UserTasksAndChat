using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserTasksAndChat.Dto;
using UserTasksAndChat.Dto.Token;
using UserTasksAndChat.Dto.User;
using UserTasksAndChat.Exceptions;
using UserTasksAndChat.Extensions;
using UserTasksAndChat.Models;
using UserTasksAndChat.Services;

namespace UserTasksAndChat.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController(
        ILogger<UserController> _logger,
        IUserService _userService,
        ITokenService _tokenService
    ) : ControllerBase
    {
        [HttpGet("get")]
        [Authorize]
        public async Task<ActionResult<GetResponseDto<UserInfoDto>>> GetAllUsers(
            [FromQuery] RequestDto requestDto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            if (!user.HasPermission(UserPermissions.ManageUsers))
                return StatusCode(403, new { message = "Permission denied to manage users" });

            try
            {
                var users = await _userService.GetAllUsersAsync(
                    requestDto.Offset, requestDto.Limit, requestDto.Search);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("register")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> Register(
            [FromBody] UserRegisterDto userRegisterDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            if (!user.HasPermission(UserPermissions.ManageUsers))
                return StatusCode(403, new { message = "Permission denied to manage users" });

            try
            {
                var createdUser = await _userService.CreateUserAsync(userRegisterDto);
                return CreatedAtAction(
                    nameof(Me),
                    new { id = createdUser.Id },
                    createdUser.ToInfoDto());
            }
            catch (UserAlreadyExistsException ex)
            {
                _logger.LogWarning("Attempt to create duplicate user: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with username {Username}", userRegisterDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoggedInDto>> Login(
            [FromBody] UserLoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var loginResult = await _userService.AuthenticateUserAsync(loginDto);

                if (!loginResult.IsSuccess)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", loginDto.Username);
                    return loginResult.ErrorCode switch
                    {
                        "USER_NOT_FOUND" or "INVALID_PASSWORD" => Unauthorized(new { message = "Invalid credentials" }),
                        "USER_INACTIVE" => Unauthorized(new { message = "User account is inactive" }),
                        "MAX_DEVICES_REACHED" => BadRequest(new { message = "Maximum number of logged devices reached" }),
                        _ => StatusCode(500, new { message = "Internal server error" })
                    };
                }

                return Ok(loginResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username {Username}", loginDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> Me()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            return Ok(user.ToInfoDto());
        }

        [HttpPatch("change-password")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> ChangePassword(
            [FromBody] UserChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { messsage = "User not found" });

            try
            {
                var updatedUser = await _userService.ChangePasswordAsync(
                    user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

                return Ok(updatedUser.ToInfoDto());
            }
            catch (InvalidPasswordException)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("update/{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> UpdateUser(
            [FromRoute] Guid id,
            [FromBody] UserUpdateDto userUpdateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new { message = "User not found" });

            if (!currentUser.HasPermission(UserPermissions.ManageUsers))
                return StatusCode(403, new { message = "Permission denied to manage users" });

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
                return BadRequest(new { message = "Password updates not allowed through this endpoint. Use reset password functionality." });

            try
            {
                var updatedUser = await _userService.UpdateUserAsync(id, userUpdateDto);
                if (updatedUser == null)
                    return NotFound(new { message = "User not found" });

                return Ok(updatedUser.ToInfoDto());
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("delete/{id:guid}")]
        [Authorize]
        public async Task<ActionResult<bool>> DeleteUser([FromRoute] Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            if (!user.HasPermission(UserPermissions.ManageUsers))
                return StatusCode(403, new { message = "Permission denied to manage users" });

            try
            {
                var deleted = await _userService.DeleteUserAsync(id);
                if (!deleted)
                    return NotFound(new { message = "User not found" });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoggedInDto>> RefreshToken(
            [FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _tokenService.RefreshTokenAsync(refreshTokenDto.Token);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Invalid refresh token attempt: {Token}", refreshTokenDto.Token);
                    return BadRequest(new { message = "Invalid refresh token" });
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("invalidate-token")]
        [Authorize]
        public async Task<ActionResult<bool>> InvalidateTokenLogout(
            [FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _tokenService.InvalidateRefreshTokenAsync(refreshTokenDto.Token);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating refresh token");
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
    }
}