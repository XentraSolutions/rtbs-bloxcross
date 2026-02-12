using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Data;
using Rtbs.Bloxcross.Models;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Rtbs.Bloxcross.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    public WebhookController(IConfiguration config, AppDbContext context)
    {
        _config = config;
        _context = context;
    }

   
}
