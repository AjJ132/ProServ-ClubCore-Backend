using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.DTO;

namespace ProServ_ClubCore_Server_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbContextFactory<ProServDbContext> _contextFactory;

        public TeamController(UserManager<IdentityUser> userManager, IDbContextFactory<ProServDbContext> contextFactory)
        {
            _userManager = userManager;
            _contextFactory = contextFactory;
        }


        [HttpGet("team-lookup")]
        [Authorize]
        public async Task<IActionResult> TeamLookup([FromQuery] Team_Code_DTO team_code)
        {
            try
            {
                using(var db = _contextFactory.CreateDbContext())
                {
                    Team_Code_DTO team = await db.Teams
                        .Where(t => t.Team_Join_Code == team_code.Team_Join_Code)
                        .Select(t => new Team_Code_DTO
                        {
                            Team_Join_Code = t.Team_Join_Code
                        })
                        .FirstOrDefaultAsync();

                    if(team == null)
                    {
                        return NotFound("Team does not exist");
                    }

                    return Ok(team);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }   
    
    
    
    }
}
