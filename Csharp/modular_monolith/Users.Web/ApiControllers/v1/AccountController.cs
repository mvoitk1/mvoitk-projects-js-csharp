using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Contracts.Users.Events;
using Modules.SharedKernel;
using Users.Application.Contracts;
using Users.Application.Dtos.Identity;
using Users.Domain;
using Users.Web.Helpers;

namespace Users.Web.ApiControllers.v1;

/// <summary>
/// User account controller — login, register, refresh, logout.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IIdentityService _identityService;
    private readonly IPublisher _publisher;

    private const string UserPassProblem = "User/Password problem";
    private const int RandomDelayMin = 500;
    private const int RandomDelayMax = 5000;

    private const string SettingsJWTPrefix = "JWT";
    private const string SettingsJWTKey = SettingsJWTPrefix + ":Key";
    private const string SettingsJWTIssuer = SettingsJWTPrefix + ":Issuer";
    private const string SettingsJWTAudience = SettingsJWTPrefix + ":Audience";
    private const string SettingsJWTExpiresInSeconds = SettingsJWTPrefix + ":ExpiresInSeconds";
    private const string SettingsJWTRefreshTokenExpiresInSeconds = SettingsJWTPrefix + ":RefreshTokenExpiresInSeconds";

    public AccountController(
        IConfiguration configuration,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AccountController> logger,
        IIdentityService identityService,
        IPublisher publisher)
    {
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _identityService = identityService;
        _publisher = publisher;
    }

    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RestMessage), StatusCodes.Status404NotFound)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> Login(
        [FromBody] Login loginInfo,
        [FromQuery] int? jwtExpiresInSeconds,
        [FromQuery] int? refreshTokenExpiresInSeconds)
    {
        var appUser = await _userManager.FindByEmailAsync(loginInfo.Email);
        if (appUser == null)
        {
            _logger.LogWarning("WebApi login failed, email {Email} not found", loginInfo.Email);
            await Task.Delay(Random.Shared.Next(RandomDelayMin, RandomDelayMax));
            return NotFound(new RestMessage(UserPassProblem));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginInfo.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("WebApi login failed, password wrong for email {Email}", loginInfo.Email);
            await Task.Delay(Random.Shared.Next(RandomDelayMin, RandomDelayMax));
            return NotFound(new RestMessage(UserPassProblem));
        }

        appUser.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(appUser);

        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        await _identityService.PurgeExpiredRefreshTokensAsync(appUser.Id);

        var refreshTokenExpiration = GetExpirationDateTime(refreshTokenExpiresInSeconds, SettingsJWTRefreshTokenExpiresInSeconds);
        var refreshToken = await _identityService.IssueRefreshTokenAsync(appUser.Id, refreshTokenExpiration);

        var jwt = JwtHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>(SettingsJWTKey)!,
            _configuration.GetValue<string>(SettingsJWTIssuer)!,
            _configuration.GetValue<string>(SettingsJWTAudience)!,
            GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds));

        return Ok(new JWTResponse { JWT = jwt, RefreshToken = refreshToken });
    }

    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RestMessage), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> Register(
        [FromBody] Register registerModel,
        [FromQuery] int? jwtExpiresInSeconds,
        [FromQuery] int? refreshTokenExpiresInSeconds)
    {
        var appUser = await _userManager.FindByEmailAsync(registerModel.Email);
        if (appUser != null)
        {
            _logger.LogWarning(" User {User} already registered", registerModel.Email);
            return BadRequest(new RestMessage("User already registered"));
        }

        appUser = new AppUser
        {
            Email = registerModel.Email,
            UserName = registerModel.Email,
            FirstName = registerModel.FirstName,
            LastName = registerModel.LastName,
            CreatedAt = DateTime.UtcNow,
        };
        var result = await _userManager.CreateAsync(appUser, registerModel.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created a new account with password", appUser.Email);

            await _publisher.Publish(new UserRegisteredEvent(appUser.Id, appUser.Email!));

            var refreshTokenExpiration = GetExpirationDateTime(refreshTokenExpiresInSeconds, SettingsJWTRefreshTokenExpiresInSeconds);
            var refreshToken = await _identityService.IssueRefreshTokenAsync(appUser.Id, refreshTokenExpiration);

            var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
            var jwt = JwtHelpers.GenerateJwt(
                claimsPrincipal.Claims,
                _configuration.GetValue<string>(SettingsJWTKey)!,
                _configuration.GetValue<string>(SettingsJWTIssuer)!,
                _configuration.GetValue<string>(SettingsJWTAudience)!,
                GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds));

            return Ok(new JWTResponse { JWT = jwt, RefreshToken = refreshToken });
        }

        var errors = result.Errors.Select(error => error.Description).ToList();
        return BadRequest(new RestMessage { Messages = errors });
    }

    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RestMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> RenewRefreshToken(
        [FromBody] RefreshTokenModel refreshTokenModel,
        [FromQuery] int? jwtExpiresInSeconds,
        [FromQuery] int? refreshTokenExpiresInSeconds)
    {
        JwtSecurityToken jwtToken;
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenModel.Jwt);
            if (jwtToken == null)
                return BadRequest(new RestMessage("No token"));
        }
        catch (Exception e)
        {
            return BadRequest(new RestMessage($"Cant parse the token, {e.Message}"));
        }

        if (!JwtHelpers.ValidateJwt(
                refreshTokenModel.Jwt,
                _configuration.GetValue<string>(SettingsJWTKey)!,
                _configuration.GetValue<string>(SettingsJWTIssuer)!,
                _configuration.GetValue<string>(SettingsJWTAudience)!))
        {
            return BadRequest("JWT validation fail");
        }

        var userEmail = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
            return BadRequest(new RestMessage("No email in jwt"));

        var appUser = await _userManager.FindByEmailAsync(userEmail);
        if (appUser == null)
            return NotFound($"User with email {userEmail} not found");

        var newRefreshExpiration = GetExpirationDateTime(refreshTokenExpiresInSeconds, SettingsJWTRefreshTokenExpiresInSeconds);
        var rotation = await _identityService.TryRotateRefreshTokenAsync(
            appUser.Id, refreshTokenModel.RefreshToken, newRefreshExpiration);

        switch (rotation.Status)
        {
            case RefreshTokenRotationStatus.NoValidToken:
                return Problem("RefreshTokens collection is empty, no valid refresh tokens found");
            case RefreshTokenRotationStatus.AmbiguousToken:
                return Problem("More than one valid refresh token found.");
        }

        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        var jwt = JwtHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>(SettingsJWTKey)!,
            _configuration.GetValue<string>(SettingsJWTIssuer)!,
            _configuration.GetValue<string>(SettingsJWTAudience)!,
            GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds));

        return Ok(new JWTResponse { JWT = jwt, RefreshToken = rotation.NewRefreshToken! });
    }

    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RestMessage), StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    public async Task<ActionResult> Logout([FromBody] LogoutInfo logout)
    {
        var userId = User.UserId();
        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser == null)
            return NotFound(new RestMessage(UserPassProblem));

        var deleteCount = await _identityService.RevokeRefreshTokenAsync(userId, logout.RefreshToken);
        return Ok(new { TokenDeleteCount = deleteCount });
    }

    private DateTime GetExpirationDateTime(int? expiresInSeconds, string settingsKey)
    {
        if (expiresInSeconds <= 0) expiresInSeconds = int.MaxValue;
        expiresInSeconds = expiresInSeconds < _configuration.GetValue<int>(settingsKey)
            ? expiresInSeconds
            : _configuration.GetValue<int>(settingsKey);

        return DateTime.UtcNow.AddSeconds(expiresInSeconds ?? 60);
    }
}
