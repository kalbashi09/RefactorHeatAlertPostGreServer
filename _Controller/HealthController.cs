using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Data;

namespace RefactorHeatAlertPostGre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public HealthController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetHealth()
        {
            var dbConnected = await _dbContext.Database.CanConnectAsync();
            return Ok(new
            {
                status = dbConnected ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                database = dbConnected ? "connected" : "disconnected"
            });
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok("pong");
    }
}