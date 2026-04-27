using FaceRecognitionApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserDatabaseService, UserDatabaseService>();
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

// Add Authentication
builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseStaticFiles();

// Initialize the database on startup
using (var scope = app.Services.CreateScope())
{
    var userDb = scope.ServiceProvider.GetRequiredService<IUserDatabaseService>();
    await userDb.InitializeAsync();
    var faceSrv = scope.ServiceProvider.GetRequiredService<IFaceRecognitionService>();
    await faceSrv.InitializeFaceAiSharpAsync();
    var attendanceSrv = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
    await attendanceSrv.InitializeAsync();
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Redirect root path to Login page
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login", permanent: false);
    return Task.CompletedTask;
});

app.MapControllers();
app.MapRazorPages();

app.Run();
