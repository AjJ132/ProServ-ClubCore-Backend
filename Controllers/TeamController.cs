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
                    Team_Lookup_DTO team = await db.Teams
                        .Where(t => t.Team_Join_Code == team_code.Team_Join_Code)
                        .Select(t => new Team_Lookup_DTO
                        {
                            Team_Name = t.Team_Name,
                            Team_Location = t.Team_City + ", " + t.Team_State
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

        [HttpPost("join-team")]
        [Authorize]
        public async Task<IActionResult> JoinTeam([FromQuery] Team_Code_DTO team_code)
        {
            try
            {
                using(var db = _contextFactory.CreateDbContext())
                {
                    var user = await _userManager.GetUserAsync(User);

                    if(user == null)
                    {
                        return Unauthorized("User does not exist");
                    }

                    var team = await db.Teams.FirstOrDefaultAsync(t => t.Team_Join_Code == team_code.Team_Join_Code);

                    if(team == null)
                    {
                        return NotFound("Team does not exist");
                    }

                    var userToUpdate = await db.Users.FirstOrDefaultAsync(u => u.User_ID == user.Id);

                    if(userToUpdate != null)
                    {
                        return BadRequest("User is already on a team");
                    }

                    userToUpdate.Club_ID = team.Team_ID;

                    await db.SaveChangesAsync();

                    return Ok("User joined team");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


    
    }
}
