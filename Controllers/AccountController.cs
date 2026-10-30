using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartAttendance.Models;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

public class AccountController : BaseController
{
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Failed sign-in: unknown or deactivated account");
            ModelState.AddModelError(string.Empty, InvalidCredentials);
            return View(model);
        }

        // Lockout on failure set to FALSE
        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} signed in", user.Id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("Failed sign-in for user {UserId}", user.Id);
        ModelState.AddModelError(string.Empty, InvalidCredentials);
        return View(model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        SetSuccess("You have been signed out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, Authorize]
    public IActionResult AccessDenied() => View();
}