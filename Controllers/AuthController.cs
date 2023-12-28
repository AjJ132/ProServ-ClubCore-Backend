using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.DTO;
using ProServ_ClubCore_Server_API.Models;
using ProServ_ClubCore_Server_API.Models.Util;
using System.Diagnostics;

namespace ProServ_University_Server_API.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IDbContextFactory<ProServDbContext> _contextFactory;

    public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IDbContextFactory<ProServDbContext> contextFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _contextFactory = contextFactory;
    }

    


    //Method to take in new users names and team codes if they have one
    [HttpPost("update-user-info")]
    public async Task<IActionResult> SaveNames(MissingNames namesToUpdate)
    {
        try
        {
            //find user by email
            var user = await _userManager.FindByEmailAsync(namesToUpdate.Email);

            if (user == null)
            {
                //return 400 bad request
                return BadRequest("User does not exist");
            }

            //create new user
            var newUser = new Users
            {
                User_ID = user.Id,
                First_Name = namesToUpdate.FirstName,
                Last_Name = namesToUpdate.LastName,
                User_Type = 1,
                Date_Joined = DateTime.Today.ToUniversalTime()
            };

            //check database for club with matching team code
            using (var db = _contextFactory.CreateDbContext())
            {
                var club = await db.Teams.FirstOrDefaultAsync(c => c.Team_Join_Code == namesToUpdate.TeamCode);

                if (club == null)
                {
                    newUser.Team_ID = null;
                }
                else
                {
                    newUser.Team_ID = club.Team_ID;
                }

                //add user to database
                db.Users.Add(newUser);

                //save changes
                await db.SaveChangesAsync();

                return Ok("User created successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("validate-session")]
    public async Task<IActionResult> ValidateSession()
    {
        try
        {
            //find user by email
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                //return 401 unauthorized
                return Unauthorized();
            }

            //check database for user with matching user id
            using (var db = _contextFactory.CreateDbContext())
            {
                var userToUpdate = await db.Users
                    .Where(u => u.User_ID == user.Id)
                    .FirstOrDefaultAsync();

                if (userToUpdate == null)
                {
                    //return 400 bad request
                    return BadRequest("User does not exist");
                }

                //return user info
                return Ok(userToUpdate);
            }
        }
        catch (Exception e)
        {
            //return 500 internal server error
            return StatusCode(500, e.Message);
        }
    }

}

