using Android.App;
using Android.Content.PM;
using Android.OS;

namespace WillowClient;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected async override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window.SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
        //Window.SetSoftInputMode(Android.Views.SoftInput.AdjustUnspecified);
        //Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);

        await Permissions.RequestAsync<Permissions.Camera>();
        await Permissions.RequestAsync<Permissions.Microphone>();
        //await Permissions.RequestAsync<Permissions.Media>();
    }
}
