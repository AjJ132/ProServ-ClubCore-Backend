using System.Diagnostics;
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
[Route("api/[controller]")]
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

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        return Ok();
    }

    //Method to take in new users. This will not be used for mass registering new users such as staff adding athlete/student accounts in mass quantities.
    [HttpPost("Signup")]
    public async Task<IActionResult> Signup(RegisterModel loginModel)
    {
        try
        {
            //check is user exists under email
            var userExists = await _userManager.FindByEmailAsync(loginModel.Email);

            if (userExists != null)
            {
                //return 409 conflict
                return Conflict("User already exists");
            }

            //create new user
            var user = new IdentityUser
            {
                UserName = loginModel.Email + "-NewUser",
                Email = loginModel.Email
            };

            var result = await _userManager.CreateAsync(user, loginModel.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            else
            {
                await _signInManager.SignInAsync(user, isPersistent: true); // isPersistent determines if the cookie is persistent or session-based

                return Ok();
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

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
                Date_Joined = DateTime.Now
            };

            //check database for club with matching team code
            using (var db = _contextFactory.CreateDbContext())
            {
                var club = await db.Clubs.FirstOrDefaultAsync(c => c.Club_Join_Code == namesToUpdate.TeamCode);

                if (club == null)
                {
                    //exit loop
                    goto ExitLoop;
                }

                newUser.Club_ID = club.Club_ID;

                //add user to database
                db.Users.Add(newUser);

                //save changes
                await db.SaveChangesAsync();

                return Ok();
            }

        ExitLoop:;
            



        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost(Name = "Logout")]
    public async Task<IActionResult> Logout(LoginModel loginModel)
    {
        return Ok();
    }
}

