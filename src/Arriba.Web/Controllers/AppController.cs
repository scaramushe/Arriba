using System.Reflection;
using Arriba.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arriba.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppController : ControllerBase
{
    private readonly ILogger<AppController> _logger;
    private readonly ILogCollector _logCollector;

    public AppController(ILogger<AppController> logger, ILogCollector logCollector)
    {
        _logger = logger;
        _logCollector = logCollector;
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        
        return Ok(new
        {
            version = informationalVersion,
            buildDate = System.IO.File.GetLastWriteTimeUtc(assembly.Location).ToString("yyyy-MM-dd HH:mm:ss"),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int count = 100)
    {
        var logs = _logCollector.GetRecentLogs(Math.Min(count, 500));
        return Ok(logs);
    }
}
