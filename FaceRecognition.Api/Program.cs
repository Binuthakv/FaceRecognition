using FaceRecognitionApp.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserDatabaseService, UserDatabaseService>();
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
