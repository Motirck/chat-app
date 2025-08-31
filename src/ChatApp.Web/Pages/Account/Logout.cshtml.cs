using ChatApp.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages.Account;

/// <summary>
/// Handles user logout with confirmation page
/// </summary>
public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Shows the logout confirmation page
    /// </summary>
    public void OnGet()
    {
        // Display logout confirmation page
    }

    /// <summary>
    /// Processes the logout request and updates user status
    /// </summary>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Update user online status before logout
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.IsOnline = false;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {Username} status updated to offline", user.UserName);
            }
        }

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out successfully");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        
        return RedirectToPage("/Index");
    }
}