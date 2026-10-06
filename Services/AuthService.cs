using Microsoft.AspNetCore.Identity;
using Api.Models;
using Api.Models.Auth;
using Api.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace Api.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;

    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly IConfiguration _configuration;
    public AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var existingUser =
            await _userRepository.GetByUsernameAsync(request.Username);

        if (existingUser is not null)
            return false;

        var user = new User
        {
            Username = request.Username
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        await _userRepository.CreateAsync(user);

        return true;
    }

    public async Task<string?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository
            .GetByUsernameAsync(request.Username);

        if (user is null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result != PasswordVerificationResult.Success)
            return null;

        return GenerateToken(user);
    }
    private string GenerateToken(User user)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}