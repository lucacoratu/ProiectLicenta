using WillowClient.Services;
using WillowClient.ViewModel;
using WillowClient.Views;
using Syncfusion.Maui.Core;
using Syncfusion.Maui.Core.Hosting;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Compatibility.Hosting;
#if ANDROID
	using WillowClient.Platforms.Android;
#endif

namespace WillowClient;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCompatibility()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        builder.ConfigureMauiHandlers((handlers) => {
#if ANDROID
            handlers.AddCompatibilityRenderer(typeof(CustomWebView), typeof(CustomWebViewRenderer));
#endif
        });

        builder.Services.AddSingleton<LoginService>();
		builder.Services.AddSingleton<LoginViewModel>();
		builder.Services.AddSingleton<LoginPage>();

        builder.Services.AddSingleton<RegisterService>();
        builder.Services.AddSingleton<RegisterViewModel>();
        builder.Services.AddSingleton<RegisterPage>();

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MobileMainPage>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<FriendService>();

		builder.Services.AddSingleton<ChatService>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<ChatPage>();

		builder.Services.AddTransient<GroupChatViewModel>();
		builder.Services.AddTransient<GroupChatPage>();

		builder.Services.AddTransient<CallViewModel>();
		builder.Services.AddTransient<WindowsCallPage>();
#if ANDROID
		builder.Services.AddTransient<AndroidCallPage>();
#endif
		builder.Services.AddSingleton<CreateGroupPage>();
		builder.Services.AddSingleton<AddFriendPage>();
		builder.Services.AddSingleton<FriendRequestPage>();

        return builder.Build();
	}
}
