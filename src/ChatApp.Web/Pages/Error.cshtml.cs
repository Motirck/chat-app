using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }
    
    public void OnGet(int? statusCode = null, string? message = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        StatusCode = statusCode ?? HttpContext.Response.StatusCode;
        ErrorMessage = message;

        // Log the error for monitoring
        _logger.LogError("Error page accessed - Status: {StatusCode}, RequestId: {RequestId}, Message: {Message}", 
            StatusCode, RequestId, ErrorMessage);

        // Set appropriate error message based on status code
        if (string.IsNullOrEmpty(ErrorMessage))
        {
            ErrorMessage = StatusCode switch
            {
                404 => "The page you are looking for could not be found.",
                401 => "You are not authorized to access this resource.",
                403 => "Access to this resource is forbidden.",
                500 => "An internal server error occurred.",
                _ => "An unexpected error occurred."
            };
        }
    }
}