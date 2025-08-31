
using ChatApp.Core.Entities;
using ChatApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages.Account;

/// <summary>
/// Handles user registration using ASP.NET Core Identity
/// </summary>
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(Input.Username);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(Input.Username), "Username is already taken");
            return Page();
        }

        // Check if email already exists
        var existingEmail = await _userManager.FindByEmailAsync(Input.Email);
        if (existingEmail != null)
        {
            ModelState.AddModelError(nameof(Input.Email), "Email is already registered");
            return Page();
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = Input.Username,
            Email = Input.Email,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsOnline = false
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Username} created successfully", Input.Username);

            // Sign in the user immediately after registration
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            return LocalRedirect(returnUrl);
        }

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}