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
    public class UsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbContextFactory<ProServDbContext> _contextFactory;

        public UsersController(UserManager<IdentityUser> userManager, IDbContextFactory<ProServDbContext> contextFactory)
        {
            _userManager = userManager;
            _contextFactory = contextFactory;
        }

        //Method to get user info
        [HttpGet("get-user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                //find user by email
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    //return 400 bad request
                    return BadRequest("User does not exist");
                }

                //check database for user with matching user id
                using (var db = _contextFactory.CreateDbContext())
                {
                    #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    User_DTO userInfo = await db.Users
                        .Where(u => u.User_ID == user.Id)
                        .Select(u => new User_DTO
                        {
                            First_Name = u.First_Name,
                            Last_Name = u.Last_Name,
                            Email = user.Email, 
                            Club_ID = u.Club_ID
                        })
                        .FirstOrDefaultAsync();
                    #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                    if (userInfo == null)
                    {
                        //return 400 bad request
                        return BadRequest("User does not exist");
                    }

                    if (userInfo.Club_ID != "")
                    {
                        #pragma warning disable CS8601 // Possible null reference assignment.
                        userInfo.Club_Name = await db.Clubs
                            .Where(c => c.Club_ID == userInfo.Club_ID)
                            .Select(c => c.Club_Name)
                            .FirstOrDefaultAsync();
                        #pragma warning restore CS8601 // Possible null reference assignment.

                        if (userInfo.Club_Name == null)
                        {
                            userInfo.Club_Name = "";
                        }
                    }
                    else
                    {
                        userInfo.Club_Name = "";
                    }

                    //return user info
                    return Ok(userInfo);
                }
            }
            catch (Exception e)
            {
                //return 500 internal server error
                return StatusCode(500, e.Message);
            }
        }
    }
}
