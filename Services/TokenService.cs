using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using UserTasksAndChat.Controllers;
using UserTasksAndChat.Dto.Token;
using UserTasksAndChat.Dto.User;
using UserTasksAndChat.Models;
using UserTasksAndChat.Repositories;

namespace UserTasksAndChat.Services
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user);
        Task<int> GetUserValidRefreshTokenCountAsync(User user);
        Task<UserRefreshTokenGenerateDto?> GenerateRefreshTokenAsync(User user);
        Task<int> DeleteExpiredUserRefreshTokensAsync(User user);
        Task<bool> IsRefreshTokenValidAsync(User user, string token);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string token);
        Task<bool> InvalidateRefreshTokenAsync(string token);
        Task<OperationResult<UserLoggedInDto>> RefreshTokenAsync(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRefreshTokenRepository _userRefreshTokenRepository;

        public TokenService(IConfiguration configuration, IUserRefreshTokenRepository userRefreshTokenRepository)
        {
            _configuration = configuration;
            _userRefreshTokenRepository = userRefreshTokenRepository;
        }

        public string GenerateJwtToken(User user)
        {
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var confKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "946533ebd4160398963890ca40347e826e5cb789ca090c6384ffb508c4aabd42";
            int confTokenLifetime = _configuration.GetValue<int>("Jwt:TokenExpirationMinutes", 30);

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(confKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "localhost",
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "localhost",
                claims: claim,
                expires: DateTime.Now.AddMinutes(confTokenLifetime),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<int> GetUserValidRefreshTokenCountAsync(User user)
        {
            int? count = await _userRefreshTokenRepository.GetUserValidTokensCountAsync(user);

            return count ?? 0;
        }

        public async Task<UserRefreshTokenGenerateDto?> GenerateRefreshTokenAsync(User user)
        {
            var random = new byte[32];

            RandomNumberGenerator.Create().GetBytes(random);

            var token = Convert.ToBase64String(random);

            var maximum = _configuration.GetValue<int>("RefreshToken:MaximumTokensPerPerson", 3);
            var userValidTokens = await GetUserValidRefreshTokenCountAsync(user);

            if (userValidTokens >= maximum)
            {
                return null;
            }

            int validForHours = _configuration.GetValue<int>("RefreshToken:TokenExpirationInHours", 24);
            var added = await _userRefreshTokenRepository.CreateRefreshTokenAsync(user, token, validForHours);

            return added 
                ? new UserRefreshTokenGenerateDto { Token = token, ValidUntilHours = validForHours }
                : null;
        }

        public async Task<int> DeleteExpiredUserRefreshTokensAsync(User user)
        {
            var deletedTokensCount = await _userRefreshTokenRepository.DeleteExpiredUserRefreshTokensAsync(user);

            return deletedTokensCount;
        }

        public async Task<bool> IsRefreshTokenValidAsync(User user, string token)
        {
            return await _userRefreshTokenRepository.IsRefreshTokenValidAsync(user, token);
        }

        public async Task<UserRefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _userRefreshTokenRepository.GetUserRefreshTokenAsync(token);
        }

        public async Task<bool> InvalidateRefreshTokenAsync(string token)
        {
            return await _userRefreshTokenRepository.DeleteUserRefreshTokenAsync(token);
        }

        public async Task<OperationResult<UserLoggedInDto>> RefreshTokenAsync(string token)
        {
            var t = await GetRefreshTokenAsync(token);
            if (t == null)
            {
                return OperationResult<UserLoggedInDto>.Failure("INVALID_TOKEN");
            }

            if (t.User == null || !t.User.IsActive)
            {
                return OperationResult<UserLoggedInDto>.Failure("USER_INACTIVE");
            }

            var jwtToken = GenerateJwtToken(t.User);

            return OperationResult<UserLoggedInDto>.Success(new UserLoggedInDto
            {
                Token = jwtToken,
                IsPasswordSet = true,
                RefreshToken = token,
                RefreshTokenValidHours = 0,
            });
        }

    }
}
