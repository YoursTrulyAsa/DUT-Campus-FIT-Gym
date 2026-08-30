using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

builder.Services.Configure<PayFastSettings>(
builder.Configuration.GetSection("PayFast"));

builder.Services.AddAuthentication(
CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.Name = "DUT_Campus_FIT_Gym_Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    options.SlidingExpiration = true;
});

builder.Services.AddDbContext<GymDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("GymDatabase")
));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

app.UseHsts();

}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var context =
    scope.ServiceProvider.GetRequiredService<GymDbContext>();

var passwordHasher =
    new PasswordHasher<Member>();

    var adminEmail =
        "admin@dut.ac.za";

    var existingAdmin =
        context.Members
            .FirstOrDefault(m => m.Email == adminEmail);

    if (existingAdmin == null)
    {
        var admin = new Member
        {
            Name = "System",
            Surname = "Administrator",
            StudentNumber = "ADMIN001",
            Email = adminEmail,
            PhoneNumber = "0000000000",
            Role = "Admin"
        };

        admin.PasswordHash =
            passwordHasher.HashPassword(
                admin,
                "Admin@123");

        context.Members.Add(admin);

        context.SaveChanges();
    }

}

app.MapControllerRoute(
name: "default",
pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
