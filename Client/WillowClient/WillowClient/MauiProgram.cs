using WillowClient.Services;
using WillowClient.ViewModel;
using WillowClient.Views;

namespace WillowClient;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<LoginService>();
		builder.Services.AddSingleton<LoginViewModel>();
		builder.Services.AddSingleton<LoginPage>();

        builder.Services.AddSingleton<RegisterService>();
        builder.Services.AddSingleton<RegisterViewModel>();
        builder.Services.AddSingleton<RegisterPage>();

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<FriendService>();

		builder.Services.AddSingleton<ChatService>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<ChatPage>();

        return builder.Build();
	}
}
