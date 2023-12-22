using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.Models;
using ProServ_ClubCore_Server_API.Models.Util;
using LoginModel = ProServ_ClubCore_Server_API.Models.LoginModel;
using RegisterModel = ProServ_ClubCore_Server_API.Models.RegisterModel;

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
                var club = await db.Clubs.FirstOrDefaultAsync(c => c.Club_Join_Code == namesToUpdate.TeamCode);

                if (club == null)
                {
                    newUser.Club_ID = "";
                }
                else
                {
                    newUser.Club_ID = club.Club_ID;
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
    [Authorize]
    public async Task<IActionResult> ValidateSession()
    {
        try
        {
            return Ok();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("test-api")]
    [Authorize]
    public async Task<IActionResult> TestApi()
    {
        return Ok("This is an authorization test");
    }
}

