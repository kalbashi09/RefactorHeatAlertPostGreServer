using Microsoft.AspNetCore.Mvc;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Dto;

namespace RefactorHeatAlertPostGre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAdminUserRepository _adminRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAdminUserRepository adminRepository, ILogger<AuthController> logger)
        {
            _adminRepository = adminRepository;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var isValid = await _adminRepository.ValidateCredentialsAsync(request.PersonnelId, request.Passcode);
            
            if (!isValid)
            {
                _logger.LogWarning("Failed login attempt for {PersonnelId}", request.PersonnelId);
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid personnel ID or passcode"
                });
            }

            var admin = await _adminRepository.GetByPersonnelIdAsync(request.PersonnelId);
            if (admin != null)
            {
                await _adminRepository.UpdateLastLoginAsync(admin.AdminUID);
            }

            _logger.LogInformation("Successful login for {PersonnelId}", request.PersonnelId);
            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                FullName = admin?.FullName,
                Token = GenerateSimpleToken() // You can replace with JWT later
            });
        }

        private static string GenerateSimpleToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}