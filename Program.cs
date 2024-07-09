using BugTrackingSystem.Data;
using BugTrackingSystem.Extensions;
using BugTrackingSystem.Models;
using BugTrackingSystem.Services;
using BugTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

// Create a new web application builder with command line arguments
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = DataUtility.GetConnectionString(builder.Configuration) ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure Entity Framework to use PostgreSQl 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// Add a developer exception filter for database-related
// Helps handle database errors and provides useful information for troubleshooting during development
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Idendity services with custom user and role
builder.Services.AddIdentity<BTUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<BTUserClaimsPrincipalFactory>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();


// Register Custom Services with dependency injection
builder.Services.AddScoped<IBTRolesService, BTRolesService>();
builder.Services.AddScoped<IBTCompanyService, BTCompanyService>();
builder.Services.AddScoped<IBTProjectService, BTProjectService>();
builder.Services.AddScoped<IBTTicketService, BTTicketService>();
builder.Services.AddScoped<IBTFileService, BTFileService>();
builder.Services.AddScoped<IBTTicketHistoryService, BTTicketHistoryService>();
builder.Services.AddScoped<IBTInviteService, BTInviteService>();
builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddScoped<IBTNotificationService, BTNotificationService>();


// Bind the email settings to the EmailSettings object
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Add MVC services to the container
// More freedom to customize and edit
builder.Services.AddMvc();

var app = builder.Build();

var scope = app.Services.CreateScope();
await DataUtility.ManageDataAsync(scope.ServiceProvider);



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseRouting();

// Custom BLogPost Details Route
app.UseAuthorization();

// Default MVC Route configuration 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Run the application
app.Run();
