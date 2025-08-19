using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGT.EAM.WebServices.Application.Controllers;

[Tags("Prueba")]
[ApiController]
[Route("api/test")]
public class TestController : Controller
{
    [Authorize]
    [HttpGet("private")]
    public IActionResult GetPrivate()
    {
        return Ok(new
        {
            Message = $"Hello {User.Identity.Name}, you’re in the private zone!"
        });
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        return Ok("This endpoint is open to everyone.");
    }

}
