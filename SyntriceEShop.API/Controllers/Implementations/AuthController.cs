using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SyntriceEShop.API.ApplicationOptions;
using SyntriceEShop.API.Controllers.Interfaces;
using SyntriceEShop.API.Models.AuthModel.DTO;
using SyntriceEShop.API.Services;
using SyntriceEShop.API.Services.Interfaces;

namespace SyntriceEShop.API.Controllers.Implementations;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IOptions<JWTOptions> jwtOptions) : ControllerBase, IAuthController
{
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] AuthRegisterRequest authRegisterRequest)
    {
        var result = await authService.RegisterAsync(authRegisterRequest);

        switch (result.Type)
        {
            case ServiceResponseType.Success:
                return Ok();
            case ServiceResponseType.Conflict:
                return Conflict(result.Message);
            default:
                return StatusCode(500); // Fallback response
        }
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> LoginAsync([FromBody] AuthLoginRequest authLoginRequest,
        [FromQuery] bool useCookies = false)
    {
        var result = await authService.LoginAsync(authLoginRequest);

        if (result is { Type: ServiceResponseType.Success, Value: not null }) 
        {
            //  Whether to send the tokens as cookies or not
            if (useCookies) // TODO: Unit tests
            {
                // Set cookies for both access token and refresh token
                Response.Cookies.Append("accessToken", result.Value.AccessToken, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpirationInMinutes),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true,
                    // unfortunately we have to use SameSiteMode.None as this is an API and will be called by a separate
                    // front end server likely on a different domain. 
                    SameSite = SameSiteMode.None 
                });
                
                Response.Cookies.Append("refreshToken", result.Value.RefreshToken, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true,
                    // unfortunately we have to use SameSiteMode.None as this is an API and will be called by a separate
                    // front end server likely on a different domain. 
                    SameSite = SameSiteMode.None 
                });
                return Ok();
            }
            else
            {
                // Send the tokens in the response rather than as cookies
                return Ok(result.Value);
            }

            return Ok(result.Value);
        }

        if (result.Type == ServiceResponseType.NotFound)
        {
            return NotFound(result.Message);
        }

        if (result.Type == ServiceResponseType.InvalidCredentials)
        {
            return Unauthorized(result.Message);
        }

        return StatusCode(500);
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<IActionResult> RefreshAsync([FromBody] AuthRefreshRequest authRefreshRequest, [FromQuery] bool useCookies = false)
    {
        if (useCookies) // TODO: Unit tests
        {
            var refreshTokenCookie = Request.Cookies["refreshToken"]; // attempt to get cookie
            
            if (string.IsNullOrEmpty(refreshTokenCookie)) // check if cookie exists
            {
                return BadRequest();
            }
            var userRefreshRequestDtoFromCookies = new AuthRefreshRequest() { RefreshToken = refreshTokenCookie };
            
            var result = await authService.RefreshAsync(userRefreshRequestDtoFromCookies);

            // If success update cookies
            if (result is { Type: ServiceResponseType.Success, Value: not null })
            {
                Response.Cookies.Append("accessToken", result.Value.AccessToken, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpirationInMinutes),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true,
                    // unfortunately we have to use SameSiteMode.None as this is an API and will be called by a separate
                    // front end server likely on a different domain. 
                    SameSite = SameSiteMode.None
                });

                Response.Cookies.Append("refreshToken", result.Value.RefreshToken, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays),
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true,
                    // unfortunately we have to use SameSiteMode.None as this is an API and will be called by a separate
                });
                
                return Ok();
            }
            
            if (result.Type == ServiceResponseType.InvalidCredentials)
            {
                return Unauthorized(result.Message);
            }

            return StatusCode(500);
        }
        else
        {
            var result = await authService.RefreshAsync(authRefreshRequest);
            
            return result.Type switch
            {
                ServiceResponseType.Success => Ok(result.Value),
                ServiceResponseType.InvalidCredentials => Unauthorized(result.Message),
                _ => StatusCode(500) // Fallback response

            };
        }
    }

    // TODO: Unit test
    [HttpDelete]
    [Route("{id:int}/refresh-tokens")]
    public async Task<IActionResult> RevokeRefreshTokensAsync([FromRoute] int id)
    {
        // check if user matches logged-in user
        var userId = int.TryParse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out int parsed)
            ? parsed
            : 0;
        if (id != userId)
        {
            return Unauthorized();
        }

        var result = await authService.RevokeRefreshTokensAsync(id);

        switch (result.Type)
        {
            case ServiceResponseType.Success:
                return Ok();
            default:
                return StatusCode(500); // Fallback response
        }
    }

    // Test authentication
    [HttpGet]
    [Authorize]
    [Route("test-authentication")]
    public async Task<IActionResult> TestAuthenticationAsync()
    {
        var username = HttpContext.User.FindFirstValue("username");
        return Ok($"Hi there, {username}. You are authenticated!");
    }
}