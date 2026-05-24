using Microsoft.AspNetCore.Mvc;

namespace Planora.Presentation;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Posts controller is reachable." });
    }
}