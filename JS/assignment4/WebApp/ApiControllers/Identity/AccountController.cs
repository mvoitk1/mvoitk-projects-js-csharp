using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.DAL.EF;
using App.Domain.Identity;
using Asp.Versioning;
using com.akaver.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PublicApi.DTO.v1.Identity;

namespace WebApp.ApiControllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
public class AccountController : ControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly Random _rnd = new();

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager,
        ILogger<AccountController> logger, IConfiguration configuration, AppDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PublicApi.DTO.v1.Identity.JwtResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PublicApi.DTO.v1.Message), StatusCodes.Status404NotFound)]
    [HttpPost]
    public async Task<ActionResult<PublicApi.DTO.v1.Identity.JwtResponse>> Login(
        [FromBody]
        PublicApi.DTO.v1.Identity.Login dto,
        [FromQuery]
        int? expiresInSeconds
    )
    {
        var expirationDateTime = DateTime.UtcNow.AddSeconds(_configuration.GetValue<int>("JWT:ExpireInSeconds"));
        if (expiresInSeconds is > 0 &&
            expiresInSeconds.Value < _configuration.GetValue<int>("JWT:ExpireInSeconds"))
        {
            expirationDateTime = DateTime.UtcNow.AddSeconds(expiresInSeconds.Value);
        }
        
        var appUser = await _userManager.FindByEmailAsync(dto.Email);
        if (appUser == null)
        {
            _logger.LogWarning("WebApi login failed. User {} not found", dto.Email);
            await Task.Delay(_rnd.Next(100, 1000));
            return NotFound(new PublicApi.DTO.v1.Message("User/Password problem!"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("WebApi login failed, password problem for user {}", dto.Email);
            await Task.Delay(_rnd.Next(100, 1000));
            return NotFound(new PublicApi.DTO.v1.Message("User/Password problem!"));
        }


        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);


        var jwt = com.akaver.Extensions.IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration["JWT:Key"]!,
            _configuration["JWT:Issuer"]!,
            _configuration["JWT:Issuer"]!,
            expirationDateTime
        );
        _logger.LogInformation("WebApi login. User {User}", dto.Email);

        appUser.RefreshTokens = await _context
            .Entry(appUser)
            .Collection(a => a.RefreshTokens!)
            .Query()
            .Where(t => t.AppUserId == appUser.Id)
            .ToListAsync();

        foreach (var userRefreshToken in appUser.RefreshTokens)
        {
            if (userRefreshToken.TokenExpirationDateTime < DateTime.UtcNow &&
                userRefreshToken.PreviousTokenExpirationDateTime < DateTime.UtcNow)
            {
                _context.RefreshTokens.Remove(userRefreshToken);
            }
        }

        var refreshToken = new RefreshToken();
        refreshToken.AppUserId = appUser.Id;
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        var res = new PublicApi.DTO.v1.Identity.JwtResponse()
        {
            Token = jwt,
            RefreshToken = refreshToken.Token,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName,
        };

        return Ok(res);
    }

    [ProducesResponseType(typeof(PublicApi.DTO.v1.Identity.JwtResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PublicApi.DTO.v1.Message), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody]
        PublicApi.DTO.v1.Identity.Register dto,
        [FromQuery]
        int? expiresInSeconds
    )
    {
        var expirationDateTime = DateTime.UtcNow.AddSeconds(_configuration.GetValue<int>("JWT:ExpireInSeconds"));
        if (expiresInSeconds is > 0 &&
            expiresInSeconds.Value < _configuration.GetValue<int>("JWT:ExpireInSeconds"))
        {
            expirationDateTime = DateTime.UtcNow.AddSeconds(expiresInSeconds.Value);
        }

        var appUser = await _userManager.FindByEmailAsync(dto.Email);
        if (appUser != null)
        {
            _logger.LogWarning(" User {User} already registered", dto.Email);
            return BadRequest(new PublicApi.DTO.v1.Message("User already registered"));
        }

        var refreshToken = new RefreshToken();

        appUser = new App.Domain.Identity.AppUser()
        {
            Email = dto.Email,
            UserName = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,

            RefreshTokens = new List<RefreshToken>()
            {
                refreshToken
            }
        };
        var result = await _userManager.CreateAsync(appUser, dto.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created a new account with password", appUser.Email);

            // Use the already-tracked appUser directly instead of re-querying via FindByEmailAsync.
            // Re-querying would load a second entity instance with the same key, causing an
            // InvalidOperationException when AddClaimAsync tries to attach it for update.
            await _userManager.AddClaimAsync(appUser, new Claim(ClaimTypes.GivenName, appUser.FirstName));
            await _userManager.AddClaimAsync(appUser, new Claim(ClaimTypes.Surname, appUser.LastName));

            var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
            var jwt = com.akaver.Extensions.IdentityExtensions.GenerateJwt(
                claimsPrincipal.Claims,
                _configuration["JWT:Key"]!,
                _configuration["JWT:Issuer"]!,
                _configuration["JWT:Issuer"]!,
                expirationDateTime
            );
            _logger.LogInformation("WebApi login. User {User}", dto.Email);
            return Ok(new PublicApi.DTO.v1.Identity.JwtResponse()
            {
                Token = jwt,
                RefreshToken = refreshToken.Token,
                FirstName = appUser.FirstName,
                LastName = appUser.LastName,
            });
        }

        var errors = result.Errors.Select(error => error.Description).ToList();
        return BadRequest(new PublicApi.DTO.v1.Message() {Messages = errors});
    }

    [ProducesResponseType(typeof(PublicApi.DTO.v1.Identity.JwtResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PublicApi.DTO.v1.Message), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ActionResult<JwtResponse>> RefreshToken([FromBody] RefreshTokenModel refreshTokenModel,
        [FromQuery]
        int? expiresInSeconds
        )
    {
        
        var expirationDateTime = DateTime.UtcNow.AddSeconds(_configuration.GetValue<int>("JWT:ExpireInSeconds"));
        if (expiresInSeconds is > 0 &&
            expiresInSeconds.Value < _configuration.GetValue<int>("JWT:ExpireInSeconds"))
        {
            expirationDateTime = DateTime.UtcNow.AddSeconds(expiresInSeconds.Value);
        }

        
        
        JwtSecurityToken jwtToken;
        // get user info from jwt
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenModel.Jwt);
            if (jwtToken == null)
            {
                return BadRequest(new PublicApi.DTO.v1.Message("No token"));
            }
        }
        catch (Exception e)
        {
            return BadRequest(new PublicApi.DTO.v1.Message($"Cant parse the token, {e.Message}"));
        }

        // TODO: validate token signature
        // https://stackoverflow.com/questions/49407749/jwt-token-validation-in-asp-net

        var userEmail = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
        {
            return BadRequest(new PublicApi.DTO.v1.Message("No email in jwt"));
        }

        // get user and tokens
        var appUser = await _userManager.FindByEmailAsync(userEmail);
        if (appUser == null)
        {
            return NotFound($"User with email {userEmail} not found");
        }

        // load and compare refresh tokens
        // explicit loading works only when entity is attached
        _context.Attach(appUser);
        await _context.Entry(appUser).Collection(u => u.RefreshTokens!)
            .Query()
            .Where(x =>
                (x.Token == refreshTokenModel.RefreshToken && x.TokenExpirationDateTime > DateTime.UtcNow) ||
                (x.PreviousToken == refreshTokenModel.RefreshToken &&
                 x.PreviousTokenExpirationDateTime > DateTime.UtcNow)
            )
            .ToListAsync();

        if (appUser.RefreshTokens == null)
        {
            return Problem("RefreshTokens collection is null");
        }

        if (appUser.RefreshTokens.Count == 0)
        {
            return Problem("RefreshTokens collection is empty, no valid refresh tokens found");
        }

        if (appUser.RefreshTokens.Count != 1)
        {
            return Problem("More than one valid refresh token found.");
        }

        // generate new jwt

        // get claims based user
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);

        // generate jwt
        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration["JWT:Key"]!,
            _configuration["JWT:Issuer"]!,
            _configuration["JWT:Issuer"]!,
            expirationDateTime
        );

        // make new refresh token, obsolete old ones
        var refreshToken = appUser.RefreshTokens.First();
        if (refreshToken.Token == refreshTokenModel.RefreshToken)
        {
            refreshToken.PreviousToken = refreshToken.Token;
            refreshToken.PreviousTokenExpirationDateTime = DateTime.UtcNow.AddMinutes(1);

            refreshToken.Token = Guid.NewGuid().ToString();
            refreshToken.TokenExpirationDateTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();
        }

        var res = new JwtResponse()
        {
            Token = jwt,
            RefreshToken = refreshToken.Token,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName
        };

        return Ok(res);
    }
}