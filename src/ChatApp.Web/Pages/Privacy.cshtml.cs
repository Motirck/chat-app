using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages;

/// <summary>
/// Privacy Policy page for ChatApp - explains data collection and usage
/// </summary>
public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _logger;

    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests to the privacy policy page
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("Privacy policy page accessed");
    }
}