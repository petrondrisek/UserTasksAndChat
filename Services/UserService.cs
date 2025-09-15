using Microsoft.AspNetCore.Identity;
using UserTasksAndChat.Controllers;
using UserTasksAndChat.Dto;
using UserTasksAndChat.Dto.Token;
using UserTasksAndChat.Dto.User;
using UserTasksAndChat.Exceptions;
using UserTasksAndChat.Extensions;
using UserTasksAndChat.Models;
using UserTasksAndChat.Repositories;
using UserTasksAndChat.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(Guid userId);
    Task<User?> GetUserAsync(string usernameOrEmail);
    Task<User> CreateUserAsync(UserRegisterDto userRegisterDto);
    Task<User?> UpdateUserAsync(Guid id, UserUpdateDto userUpdateDto);
    Task<bool> DeleteUserAsync(Guid id);
    Task<GetResponseDto<UserInfoDto>> GetAllUsersAsync(int offset, int limit, string? search = null);
    Task<User> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    Task<OperationResult<UserLoggedInDto>> AuthenticateUserAsync(UserLoginDto loginDto);
    string CreatePasswordHash(User user, string password);
    bool VerifyPassword(User user, string password);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        return await _userRepository.GetUserByIdAsync(userId);
    }

    public async Task<User?> GetUserAsync(string usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return null;

        // Pokus o nalezení podle username, pak podle emailu
        return await _userRepository.GetUserByUsernameAsync(usernameOrEmail) ??
               await _userRepository.GetUserByEmailAsync(usernameOrEmail);
    }

    public async Task<User> CreateUserAsync(UserRegisterDto userRegisterDto)
    {
        // Kontrola unikátnosti
        await ValidateUserUniquenessAsync(userRegisterDto.Username, userRegisterDto.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userRegisterDto.Username,
            Email = userRegisterDto.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateUserAsync(user);
        return createdUser ?? throw new InvalidOperationException("Failed to create user");
    }

    public async Task<User?> UpdateUserAsync(Guid id, UserUpdateDto userUpdateDto)
    {
        if (id == Guid.Empty)
            return null;

        // Kontrola unikátnosti emailu při aktualizaci
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Email))
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(userUpdateDto.Email);
            if (existingUser != null && existingUser.Id != id)
            {
                throw new UserAlreadyExistsException($"User with email '{userUpdateDto.Email}' already exists");
            }
        }

        return await _userRepository.UpdateUserAsync(id, userUpdateDto);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        return await _userRepository.DeleteUserAsync(id);
    }

    public async Task<GetResponseDto<UserInfoDto>> GetAllUsersAsync(int offset, int limit, string? search = null)
    {
        if (offset < 0 || limit <= 0)
            return new GetResponseDto<UserInfoDto> { TotalCount = 0, Items = Array.Empty<UserInfoDto>() };

        var totalCountTask = _userRepository.GetUsersCountAsync(search);
        var usersTask = _userRepository.GetUsersAsync(offset, limit, search);

        await Task.WhenAll(totalCountTask, usersTask);

        var totalCount = await totalCountTask;
        var users = await usersTask;

        return new GetResponseDto<UserInfoDto>
        {
            TotalCount = totalCount,
            Items = users.Select(u => u.ToInfoDto()).ToArray()
        };
    }

    public async Task<User> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        if (!VerifyPassword(user, currentPassword))
            throw new InvalidPasswordException("Current password is incorrect");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("New password cannot be empty", nameof(newPassword));

        var hashedPassword = CreatePasswordHash(user, newPassword);
        var updateDto = new UserUpdateDto { Password = hashedPassword };

        var updatedUser = await _userRepository.UpdateUserAsync(user.Id, updateDto);
        return updatedUser ?? throw new InvalidOperationException("Failed to update password");
    }

    public async Task<OperationResult<UserLoggedInDto>> AuthenticateUserAsync(UserLoginDto loginDto)
    {
        var user = await GetUserAsync(loginDto.Username);
        if (user == null)
            return OperationResult<UserLoggedInDto>.Failure("USER_NOT_FOUND", "Invalid credentials");

        if (!user.IsActive)
            return OperationResult<UserLoggedInDto>.Failure("USER_INACTIVE", "User account is inactive");

        if (!VerifyPassword(user, loginDto.Password))
            return OperationResult<UserLoggedInDto>.Failure("INVALID_PASSWORD", "Invalid credentials");

        try
        {
            var token = _tokenService.GenerateJwtToken(user);
            await _tokenService.DeleteExpiredUserRefreshTokensAsync(user);

            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
            if (refreshToken == null)
                return OperationResult<UserLoggedInDto>.Failure("MAX_DEVICES_REACHED", "Maximum number of devices reached");

            var result = new UserLoggedInDto
            {
                Token = token,
                IsPasswordSet = !string.IsNullOrWhiteSpace(user.Password),
                RefreshToken = refreshToken.Token,
                RefreshTokenValidHours = refreshToken.ValidUntilHours
            };

            return OperationResult<UserLoggedInDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", loginDto.Username);
            return OperationResult<UserLoggedInDto>.Failure("AUTHENTICATION_ERROR", "Authentication failed");
        }
    }

    public string CreatePasswordHash(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrEmpty(user.Password))
            return true; // User should be recommended to change password on next login

        if (string.IsNullOrWhiteSpace(password))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        return result == PasswordVerificationResult.Success;
    }

    private async Task ValidateUserUniquenessAsync(string username, string email)
    {
        var existingByUsername = await _userRepository.GetUserByUsernameAsync(username);
        if (existingByUsername != null)
            throw new UserAlreadyExistsException($"User with username '{username}' already exists");

        var existingByEmail = await _userRepository.GetUserByEmailAsync(email);
        if (existingByEmail != null)
            throw new UserAlreadyExistsException($"User with email '{email}' already exists");
    }
}
