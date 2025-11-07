using Microsoft.AspNetCore.Mvc;

namespace  Authentication.API.Controllers;


[ApiController]
[Route("users/account")]
public class UserController:ControllerBase
{
    private readonly IAccountService _accountService;
    public UserController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        // Request property came from ControllerBase class.
        var refreshToken = Request.Cookies["REFRESH_TOKEN"];
        // The method RefreshTokenAsync(Old Token) writes the new refresh token in cookies at response  
        await _accountService.RefreshTokenAsync(refreshToken);
        return Ok();
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        await _accountService.LoginAsync(loginRequest);
        return Ok();
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        await _accountService.RegistrAsync(registerRequest);
        return Ok();
    }
}