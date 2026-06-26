using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Приветствие")]
    [SwaggerResponse(200, "Возвращает приветствие")]
    public ActionResult<Result<string>> SayHello()
    {
        return Ok(Result<string>.Ok("Hello, World!"));
    }
}
