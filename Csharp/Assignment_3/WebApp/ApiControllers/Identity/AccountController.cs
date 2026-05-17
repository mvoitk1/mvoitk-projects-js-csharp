using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.BLL.Contracts;
using App.Domain.Identity;
using RefreshTokenRotationStatus = App.BLL.DTO.Identity.RefreshTokenRotationStatus;
using App.DTO.v1.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Identity;

/// <summary>
/// User account controller - login, register, etc.
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
    private readonly Random _random = new Random();

    private const string UserPassProblem = "User/Password problem";
    private const int RandomDelayMin = 500;
    private const int RandomDelayMax = 5000;

    private const string SettingsJWTPrefix = "JWT";
    private const string SettingsJWTKey = SettingsJWTPrefix + ":Key";
    private const string SettingsJWTIssuer = SettingsJWTPrefix + ":Issuer";
    private const string SettingsJWTAudience = SettingsJWTPrefix + ":Audience";
    private const string SettingsJWTExpiresInSeconds = SettingsJWTPrefix + ":ExpiresInSeconds";
    private const string SettingsJWTRefreshTokenExpiresInSeconds = SettingsJWTPrefix + ":RefreshTokenExpiresInSeconds";


    /// <summary>
    /// Constructor
    /// </summary>
    public AccountController(IConfiguration configuration, UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, ILogger<AccountController> logger, IIdentityService identityService)
    {
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _identityService = identityService;
    }

    /// <summary>
    /// User authentication, returns JWT and refresh token
    /// </summary>
    /// <param name="loginInfo">Login model</param>
    /// <param name="jwtExpiresInSeconds">Optional, use custom jwt expiration</param>
    /// <param name="refreshTokenExpiresInSeconds">Optional, use custom refresh token expiration</param>
    /// <returns>JWT and refresh token</returns>
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.Dto.v1.Message), StatusCodes.Status404NotFound)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> Login(
        [FromBody]
        Login loginInfo,
        [FromQuery]
        int? jwtExpiresInSeconds,
        [FromQuery]
        int? refreshTokenExpiresInSeconds
    )
    {
        // verify user
        var appUser = await _userManager.FindByEmailAsync(loginInfo.Email);
        if (appUser == null)
        {
            _logger.LogWarning("WebApi login failed, email {} not found", loginInfo.Email);
            await Task.Delay(_random.Next(RandomDelayMin, RandomDelayMax));
            return NotFound(new App.Dto.v1.Message(UserPassProblem));
        }

        // verify password
        var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginInfo.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("WebApi login failed, password {} for email {} was wrong", loginInfo.Password,
                loginInfo.Email);
            await Task.Delay(_random.Next(RandomDelayMin, RandomDelayMax));
            return NotFound(new App.Dto.v1.Message(UserPassProblem));
        }

        appUser.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(appUser);

        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        await _identityService.PurgeExpiredRefreshTokensAsync(appUser.Id);

        var refreshTokenExpiration = GetExpirationDateTime(refreshTokenExpiresInSeconds, SettingsJWTRefreshTokenExpiresInSeconds);
        var refreshToken = await _identityService.IssueRefreshTokenAsync(appUser.Id, refreshTokenExpiration);

        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>(SettingsJWTKey)!,
            _configuration.GetValue<string>(SettingsJWTIssuer)!,
            _configuration.GetValue<string>(SettingsJWTAudience)!,
            GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds)
        );

        var responseData = new JWTResponse()
        {
            JWT = jwt,
            RefreshToken = refreshToken
        };

        return Ok(responseData);
    }


    /// <summary>
    /// Register new user, returns JWT and refresh token
    /// </summary>
    /// <param name="registerModel">Reg info</param>
    /// <param name="jwtExpiresInSeconds">Optional custom jwt expiration</param>
    /// <param name="refreshTokenExpiresInSeconds">Optional custom refresh token expiration</param>
    /// <returns></returns>
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.Dto.v1.Message), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> Register(
        [FromBody]
        Register registerModel,
        [FromQuery]
        int? jwtExpiresInSeconds,
        [FromQuery]
        int? refreshTokenExpiresInSeconds
    )
    {
        var appUser = await _userManager.FindByEmailAsync(registerModel.Email);
        if (appUser != null)
        {
            _logger.LogWarning(" User {User} already registered", registerModel.Email);
            return BadRequest(new App.Dto.v1.Message("User already registered"));
        }

        appUser = new AppUser()
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

            var refreshTokenExpiration = GetExpirationDateTime(refreshTokenExpiresInSeconds, SettingsJWTRefreshTokenExpiresInSeconds);
            var refreshToken = await _identityService.IssueRefreshTokenAsync(appUser.Id, refreshTokenExpiration);

            var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
            var jwt = IdentityExtensions.GenerateJwt(
                claimsPrincipal.Claims,
                _configuration.GetValue<string>(SettingsJWTKey)!,
                _configuration.GetValue<string>(SettingsJWTIssuer)!,
                _configuration.GetValue<string>(SettingsJWTAudience)!,
                GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds)
            );
            _logger.LogInformation("WebApi login. User {User}", registerModel.Email);
            return Ok(new JWTResponse()
            {
                JWT = jwt,
                RefreshToken = refreshToken,
            });
        }

        var errors = result.Errors.Select(error => error.Description).ToList();
        return BadRequest(new App.Dto.v1.Message() { Messages = errors });
    }

    /// <summary>
    /// Renew JWT using refresh token
    /// </summary>
    /// <param name="refreshTokenModel">Data for renewal</param>
    /// <param name="jwtExpiresInSeconds">Optional custom expiration for jwt</param>
    /// <param name="refreshTokenExpiresInSeconds">Optional custom expiration for refresh token</param>
    /// <returns></returns>
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.Dto.v1.Message), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ActionResult<JWTResponse>> RenewRefreshToken(
        [FromBody]
        RefreshTokenModel refreshTokenModel,
        [FromQuery]
        int? jwtExpiresInSeconds,
        [FromQuery]
        int? refreshTokenExpiresInSeconds
    )
    {
        JwtSecurityToken jwtToken;
        // get user info from jwt
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenModel.Jwt);
            if (jwtToken == null)
            {
                return BadRequest(new App.Dto.v1.Message("No token"));
            }
        }
        catch (Exception e)
        {
            return BadRequest(new App.Dto.v1.Message($"Cant parse the token, {e.Message}"));
        }

        // validate jwt, ignore expiration date
        if (!IdentityExtensions.ValidateJwt(
                refreshTokenModel.Jwt,
                _configuration.GetValue<string>(SettingsJWTKey)!,
                _configuration.GetValue<string>(SettingsJWTIssuer)!,
                _configuration.GetValue<string>(SettingsJWTAudience)!
            ))
        {
            return BadRequest("JWT validation fail");
        }

        var userEmail = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
        {
            return BadRequest(new App.Dto.v1.Message("No email in jwt"));
        }

        // get user
        var appUser = await _userManager.FindByEmailAsync(userEmail);
        if (appUser == null)
        {
            return NotFound($"User with email {userEmail} not found");
        }

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

        // generate new jwt
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>(SettingsJWTKey)!,
            _configuration.GetValue<string>(SettingsJWTIssuer)!,
            _configuration.GetValue<string>(SettingsJWTAudience)!,
            GetExpirationDateTime(jwtExpiresInSeconds, SettingsJWTExpiresInSeconds)
        );

        var res = new JWTResponse()
        {
            JWT = jwt,
            RefreshToken = rotation.NewRefreshToken!,
        };

        return Ok(res);
    }

    /// <summary>Invalidate the current refresh token, effectively logging the user out.</summary>
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.Dto.v1.Message), StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    public async Task<ActionResult> Logout([FromBody] LogoutInfo logout)
    {
        // delete the refresh token - so user is kicked out after jwt expiration.
        // We do not invalidate the jwt on serverside - that would require pipeline modification
        // and checking against db on every request. The client can keep using the JWT until it
        // expires (keep jwt expiration short ~1 min).

        var userId = User.UserId();
        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser == null)
        {
            return NotFound(new App.Dto.v1.Message(UserPassProblem));
        }

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
