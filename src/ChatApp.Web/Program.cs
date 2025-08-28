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

// Add configuration
builder.Services.Configure<StockApiOptions>(
    builder.Configuration.GetSection(StockApiOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Register services
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
builder.Services.AddScoped<IStockService, StockService>();

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

app.UseAuthentication(); // Add authentication
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");

// Ensure database is created and updated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    context.Database.EnsureCreated();
}

app.Run();