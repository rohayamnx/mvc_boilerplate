
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mvc_boilerplate.Authorization;
using mvc_boilerplate.Data;
using mvc_boilerplate.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Home/AccessDenied";
    });

// StaffHub API service (HttpClient factory pattern)
builder.Services.AddHttpClient<IStaffHubApiService, StaffHubApiService>(client =>
{
    var baseUrl = builder.Configuration["StaffHubApi:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// StaffHub Auth Info service
builder.Services.AddHttpClient<IStaffHubAuthInfoService, StaffHubAuthInfoService>(client =>
{
    var baseUrl = builder.Configuration["StaffHubApi:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// AD authentication service (Windows only)
if (OperatingSystem.IsWindows())
{
    builder.Services.AddScoped<IADAuthenticationService, ADAuthenticationService>();
}

// In-memory cache for StaffHub API responses
builder.Services.AddMemoryCache();

// Session support (for storing user profile data from StaffHub)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register the dynamic permission policy provider — allows [Authorize(Policy = "Module.Action")]
// without manually registering each policy.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed default data
await SeedData.InitializeAsync(app.Services);

app.Run();
