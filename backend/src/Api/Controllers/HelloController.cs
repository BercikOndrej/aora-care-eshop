using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HelloController : ControllerBase
{
    // GET /api/hello
    [HttpGet]
    public ActionResult<HelloResponse> Get()
        => Ok(new HelloResponse("Hello from aora-care API!"));
}

public sealed record HelloResponse(string Message);
