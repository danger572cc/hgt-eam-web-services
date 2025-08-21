using HGT.EAM.WebServices.Infrastructure.Architecture.Controller;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGT.EAM.WebServices.Application.Controllers;

[Tags("Prueba funcionamiento servicio")]
[ApiController]
[Route("api/test")]
public class TestController : HGTController
{
    public TestController(IMediator mediator, ILogger<TestController> logger)
        : base(mediator, logger)
    {
    }

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
        return Ok($"This endpoint is open to everyone. ${DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
    }
}
