using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProServ_ClubCore_Server_API.Models;
using LoginModel = ProServ_ClubCore_Server_API.Models.LoginModel;
using RegisterModel = ProServ_ClubCore_Server_API.Models.RegisterModel;

namespace ProServ_University_Server_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet(Name = "Login")]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        return Ok();
    }

    //Method to take in new users. This will not be used for mass registering new users such as staff adding athlete/student accounts in mass quantities.
    [HttpPost(Name = "Register")]
    public async Task<IActionResult> Register(RegisterModel loginModel)
    {
        try
        {
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

                return RedirectToAction("Index", "Home");
            }
            
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

