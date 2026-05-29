using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserDatabaseService, UserDatabaseService>();
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();
builder.Services.AddSingleton<IFaceONNXService, FaceONNXService>();

// FIX : GZip response compression — reduces payload size for embedding responses
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest; // Speed over size
});

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

// ✅ FIX 8: Enable response compression middleware
app.UseResponseCompression();

// Initialize the database on startup
using (var scope = app.Services.CreateScope())
{
    var userDb = scope.ServiceProvider.GetRequiredService<IUserDatabaseService>();
    await userDb.InitializeAsync();
    // ✅ FIX 5: Warm up embedding cache at startup — zero DB reads during verification
    await userDb.RefreshEmbeddingCacheAsync();
    var faceSrv = scope.ServiceProvider.GetRequiredService<IFaceRecognitionService>();
    await faceSrv.InitializeFaceAiSharpAsync();
    var attendanceSrv = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
    await attendanceSrv.InitializeAsync();
}

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();

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
