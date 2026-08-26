using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


// =========================================================
// PAYFAST SETTINGS
// =========================================================

builder.Services.Configure<PayFastSettings>(
    builder.Configuration.GetSection("PayFast"));


// =========================================================
// AUTHENTICATION
// =========================================================

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";

        // Keep the authentication cookie available
        // during the PayFast return flow.
        options.Cookie.Name =
            "DUT_Campus_FIT_Gym_Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });


// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "GymDatabase")
    ));


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// ERROR HANDLING
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}


// =========================================================
// HTTPS
// =========================================================

app.UseHttpsRedirection();


// =========================================================
// STATIC FILES
// =========================================================

app.UseStaticFiles();


// =========================================================
// ROUTING
// =========================================================

app.UseRouting();


// =========================================================
// AUTHENTICATION
// =========================================================

app.UseAuthentication();


// =========================================================
// AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// CREATE INITIAL ADMIN ACCOUNT
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<GymDbContext>();

    var passwordHasher =
        new PasswordHasher<Member>();

    var adminEmail =
        "admin@dut.ac.za";

    var existingAdmin =
        context.Members
            .FirstOrDefault(
                m => m.Email == adminEmail);

    if (existingAdmin == null)
    {
        var admin = new Member
        {
            Name =
                "System",

            Surname =
                "Administrator",

            StudentNumber =
                "ADMIN001",

            Email =
                adminEmail,

            PhoneNumber =
                "0000000000",

            Role =
                "Admin"
        };

        admin.PasswordHash =
            passwordHasher.HashPassword(
                admin,
                "Admin@123");

        context.Members.Add(admin);

        context.SaveChanges();
    }
}


// =========================================================
// DEFAULT ROUTE
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Register}/{id?}");


// =========================================================
// RUN
// =========================================================

app.Run();