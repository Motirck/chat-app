
using ChatApp.Core.Configuration;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Repositories;
using ChatApp.Infrastructure.Services;
using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Add Entity Framework
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ASP.NET Core Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ChatDbContext>();

// Configure Identity paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

// Add authorization policy
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Configure Razor Pages with specific authorization
builder.Services.AddRazorPages(options =>
{
    // Allow anonymous access to specific pages
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToFolder("/Account");
    
    // Require authentication for other pages
    options.Conventions.AuthorizePage("/Chat");
});

// Add configuration
builder.Services.Configure<StockApiOptions>(
    builder.Configuration.GetSection(StockApiOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Register services
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();

// Register broadcaster implementation (Web layer)
builder.Services.AddScoped<IStockQuoteBroadcaster, SignalRStockQuoteBroadcaster>();

// Register background service (Infrastructure layer)
builder.Services.AddHostedService<StockQuoteHandlerService>();


var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Works for first-time users AND updates existing databases
    context.Database.Migrate();
    
    // Create bot user if it doesn't exist
    await EnsureBotUserExists(userManager);
}

app.Run();

static async Task EnsureBotUserExists(UserManager<ApplicationUser> userManager)
{
    const string botUsername = "StockBot";
    const string botEmail = "stockbot@chatapp.system";
    
    var botUser = await userManager.FindByNameAsync(botUsername);
    if (botUser == null)
    {
        botUser = new ApplicationUser
        {
            UserName = botUsername,
            Email = botEmail,
            EmailConfirmed = true,
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        };
        
        // Create the bot user without a password (system account)
        var result = await userManager.CreateAsync(botUser);
        
        if (result.Succeeded)
        {
            Console.WriteLine("Bot user 'StockBot' created successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to create bot user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
