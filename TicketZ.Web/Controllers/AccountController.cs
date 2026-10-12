using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketZ.Core.Models;

namespace TicketZ.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() => Redirect("/Identity/Account/Register");

    [HttpGet]
    public IActionResult Login() => Redirect("/Identity/Account/Login");

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Event");
    }
}
