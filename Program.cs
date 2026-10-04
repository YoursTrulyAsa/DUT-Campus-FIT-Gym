using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DUT_Campus_FIT_Gym.Services;
using FFMpegCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

GlobalFFOptions.Configure(new FFOptions
{
    BinaryFolder = Path.Combine(builder.Environment.ContentRootPath, "ffmpeg")
});
builder.Services.AddHttpClient<NgrokService>();

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

builder.Services.AddScoped<MembershipPricingService>();
builder.Services.AddScoped<RewardService>();
builder.Services.AddScoped<MonthlyLeaderboardService>();
builder.Services.AddHostedService<MonthlyLeaderboardBackgroundService>();
builder.Services.AddHostedService<ReservationBackgroundService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();

    if (!context.Exercises.Any())
    {
        context.Exercises.AddRange(
            new Exercise
            {
                ExerciseName = "Treadmill Walking",
                Category = "Cardio",
                Difficulty = "Beginner",
                MuscleGroup = "Legs",
                Description = "A low-impact cardio exercise performed on a treadmill.",
                Instructions = "Walk at a comfortable pace while maintaining good posture."
            },
            new Exercise
            {
                ExerciseName = "Treadmill Running",
                Category = "Cardio",
                Difficulty = "Intermediate",
                MuscleGroup = "Legs",
                Description = "A running exercise performed on a treadmill to improve cardiovascular endurance.",
                Instructions = "Start with a warm-up and gradually increase the treadmill speed."
            },
            new Exercise
            {
                ExerciseName = "Cycling",
                Category = "Cardio",
                Difficulty = "Beginner",
                MuscleGroup = "Legs",
                Description = "A cardiovascular exercise performed using an exercise bike.",
                Instructions = "Adjust the seat and resistance before cycling at a comfortable pace."
            },
            new Exercise
            {
                ExerciseName = "Dumbbell Bicep Curl",
                Category = "Strength",
                Difficulty = "Beginner",
                MuscleGroup = "Biceps",
                Description = "A dumbbell exercise targeting the biceps.",
                Instructions = "Hold the dumbbells at your sides and curl them toward your shoulders while keeping your elbows close to your body."
            },
            new Exercise
            {
                ExerciseName = "Dumbbell Shoulder Press",
                Category = "Strength",
                Difficulty = "Beginner",
                MuscleGroup = "Shoulders",
                Description = "A dumbbell exercise targeting the shoulder muscles.",
                Instructions = "Hold dumbbells at shoulder height and press them upward before slowly lowering them."
            },
            new Exercise
            {
                ExerciseName = "Barbell Squat",
                Category = "Strength",
                Difficulty = "Intermediate",
                MuscleGroup = "Legs",
                Description = "A compound strength exercise targeting the lower body.",
                Instructions = "Position the barbell securely, squat while keeping your back controlled, then return to the starting position."
            },
            new Exercise
            {
                ExerciseName = "Cable Chest Press",
                Category = "Strength",
                Difficulty = "Intermediate",
                MuscleGroup = "Chest",
                Description = "A cable exercise targeting the chest and upper-body pushing muscles.",
                Instructions = "Stand in a stable position and press the cable handles forward until your arms are extended."
            },
            new Exercise
            {
                ExerciseName = "Lat Pulldown",
                Category = "Strength",
                Difficulty = "Beginner",
                MuscleGroup = "Back",
                Description = "A machine-based exercise targeting the upper back.",
                Instructions = "Pull the bar toward your upper chest while keeping your torso controlled."
            },
            new Exercise
            {
                ExerciseName = "Push-Ups",
                Category = "Bodyweight",
                Difficulty = "Beginner",
                MuscleGroup = "Chest",
                Description = "A bodyweight exercise targeting the chest, shoulders and triceps.",
                Instructions = "Keep your body aligned and lower your chest toward the floor before pushing back up."
            },
            new Exercise
            {
                ExerciseName = "Bodyweight Squats",
                Category = "Bodyweight",
                Difficulty = "Beginner",
                MuscleGroup = "Legs",
                Description = "A bodyweight exercise targeting the lower body.",
                Instructions = "Stand with your feet shoulder-width apart, lower into a squat, then return to standing."
            },
            new Exercise
            {
                ExerciseName = "Lunges",
                Category = "Bodyweight",
                Difficulty = "Beginner",
                MuscleGroup = "Legs",
                Description = "A lower-body bodyweight exercise that works the legs and glutes.",
                Instructions = "Step forward, lower your body under control, then push through the front foot to return."
            },
            new Exercise
            {
                ExerciseName = "Plank",
                Category = "Core",
                Difficulty = "Beginner",
                MuscleGroup = "Core",
                Description = "An isometric exercise that strengthens the core.",
                Instructions = "Support your body using your forearms and toes while keeping your body in a straight line."
            },
            new Exercise
            {
                ExerciseName = "Mountain Climbers",
                Category = "Cardio",
                Difficulty = "Intermediate",
                MuscleGroup = "Core",
                Description = "A dynamic bodyweight exercise combining cardio and core training.",
                Instructions = "Start in a plank position and alternate driving your knees toward your chest."
            },
            new Exercise
            {
                ExerciseName = "Leg Press",
                Category = "Strength",
                Difficulty = "Beginner",
                MuscleGroup = "Legs",
                Description = "A machine exercise targeting the lower-body muscles.",
                Instructions = "Place your feet securely on the platform, lower the weight under control, then press it away."
            },
            new Exercise
            {
                ExerciseName = "Cable Tricep Pushdown",
                Category = "Strength",
                Difficulty = "Beginner",
                MuscleGroup = "Triceps",
                Description = "A cable exercise targeting the triceps.",
                Instructions = "Keep your elbows close to your sides and push the cable handle downward before returning slowly."
            }
        );

        context.SaveChanges();
    }
}

app.Run();