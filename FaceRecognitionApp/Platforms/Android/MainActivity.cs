using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using FaceRecognitionApp.Helpers;

namespace FaceRecognitionApp;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    LaunchMode = LaunchMode.SingleTop, 
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    Exported = true,
    HardwareAccelerated = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            AppLogger.Info("MainActivity.OnCreate starting");
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#512BD4"));
            }
            
            base.OnCreate(savedInstanceState);
            
            AppLogger.Success("MainActivity.OnCreate completed");
        }
        catch (Exception ex)
        {
            AppLogger.Error("MainActivity.OnCreate failed", ex);
            Android.Widget.Toast.MakeText(this, $"Startup Error: {ex.Message}", Android.Widget.ToastLength.Long)?.Show();
            throw;
        }
    }

    protected override void OnResume()
    {
        try
        {
            base.OnResume();
            AppLogger.Trace("MainActivity.OnResume");
        }
        catch (Exception ex)
        {
            AppLogger.Error("MainActivity.OnResume failed", ex);
        }
    }

    protected override void OnStart()
    {
        try
        {
            base.OnStart();
            AppLogger.Trace("MainActivity.OnStart");
        }
        catch (Exception ex)
        {
            AppLogger.Error("MainActivity.OnStart failed", ex);
        }
    }
    
    protected override void OnPause()
    {
        try
        {
            base.OnPause();
            AppLogger.Trace("MainActivity.OnPause");
        }
        catch (Exception ex)
        {
            AppLogger.Error("MainActivity.OnPause failed", ex);
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
