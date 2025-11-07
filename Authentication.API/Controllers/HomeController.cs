using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace  Authentication.API.Controllers;

[ApiController]
[Route("home")]
[Authorize] // This is to make the endpoints not accessable without authentication.
public class HomeController : ControllerBase
{
    [HttpGet("movies")]
    public async Task<IActionResult> GetMovies()
    {
        return Ok(new List<string> { "Matrix", "Comandos", "SomeMove" });
    }


    [AllowAnonymous] // This endpoint accessable to anyone without authentication
    [HttpGet("Text")]
    public async Task<IActionResult> GetString()
    {
        return Ok("This is Allow to anyone ");
    }
}