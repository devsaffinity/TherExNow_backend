using Apsy.Common.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Aps.Apps.HepNow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IAuthService authService;
        private readonly ILogger<AccountController> logger;

        public AccountController(IConfiguration configuration, IAuthService authService, ILogger<AccountController> logger)
        {
            this.configuration = configuration;
            this.authService = authService;
            this.logger = logger;
        }

        [HttpPost("email-login")]
        public async Task<ActionResult<AuthToken>> EmailLogin([FromQuery] string email, [FromQuery] string password)
        {
            try
            {
                var apiKey = configuration["ApiKey"];
                var authConfig = new AuthConfig { ApiKey = apiKey };
                var authToken = await authService.EmailLogin(authConfig, email, password);
                return authToken;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Email login failed for {Email}", email);
                return NotFound("User Not Found");
            }
        }

        [HttpGet("confirm-user")]
        public IActionResult ConfirmUser(string confirmKey, string returnUrl, [FromServices] IUserService userService)
        {
            if (userService.ConfirmUser(confirmKey).Status == ResponseStatus.Success)
                return Redirect(returnUrl);
            else
                return BadRequest();
        }
    }
}
