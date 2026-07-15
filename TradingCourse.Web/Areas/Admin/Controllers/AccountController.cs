using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradingCourse.Application.Models;

namespace TradingCourse.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly SignInManager<AdminUser> _signInManager;
    private readonly UserManager<AdminUser> _userManager;

    public AccountController(SignInManager<AdminUser> signInManager, UserManager<AdminUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Email and Password are required.");
            return View();
        }

        // Search for user
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        // Verify password
        var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
