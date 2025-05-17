using Microsoft.AspNetCore.Mvc;
using Seagull.Infrastructure.Data;
using Seagull.Infrastructure.Hooks;

namespace Seagull.API.Controllers;

[Route("api/island")]
[ApiController]
public class IslandController(MainContext context, S3Hook hook) : ControllerBase
{
    private readonly MainContext _context = context;
    private readonly S3Hook _hook = hook;


}
