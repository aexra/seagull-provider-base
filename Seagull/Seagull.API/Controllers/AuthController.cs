using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Seagull.API.DTO.auth.Request;
using Seagull.API.DTO.auth.Response;
using Seagull.API.Services;
using Seagull.Core.Entities.Identity;
using System.IdentityModel.Tokens.Jwt;

namespace Seagull.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(
    UserManager<User> userManager,
    TokenService tokenService,
    IConfiguration config) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly TokenService _tokenService = tokenService;
    private readonly IConfiguration _config = config;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterDto dto)
    {
        var user = new User
        {
            Email = dto.Email, // уникальный, можно поменять, но сложно
            UserName = dto.UserName, // уникальный, нельзя менять
            DisplayName = dto.DisplayName, // не уникальный, можно поменять
            Tag = dto.DisplayName, // уникальный, можно поменять
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(await GenerateAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDto dto)
    {
        // Ищем по email ИЛИ username
        var user = await _userManager.FindByNameAsync(dto.Login)
                 ?? await _userManager.FindByEmailAsync(dto.Login);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid login or password");

        await _userManager.UpdateAsync(user);
        return Ok(await GenerateAuthResponse(user));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenDto dto)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
        var username = principal?.FindFirst(JwtRegisteredClaimNames.Nickname)?.Value;

        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return Unauthorized();

        return Ok(await GenerateAuthResponse(user));
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AuthResponse(
            AccessToken: _tokenService.GenerateAccessToken(user, roles),
            RefreshToken: _tokenService.GenerateRefreshToken()
        );
    }
}