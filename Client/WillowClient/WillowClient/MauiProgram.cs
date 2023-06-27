using WillowClient.Services;
using WillowClient.ViewModel;
using WillowClient.Views;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using UraniumUI;
using Plugin.LocalNotification;
using WillowClient.Database;
using Mopups.Hosting;
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
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureMopups()
#if ANDROID
			.UseLocalNotification()
#endif
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("NeusharpBold.otf", "NeusharpBold");
				fonts.AddFont("Labrada.ttf", "Labrada");
				fonts.AddFont("Labrada-Medium.ttf", "Labrada-Medium");
				fonts.AddFont("Labrada-Regular.ttf", "Labrada-Regular");
                fonts.AddFontAwesomeIconFonts();
            });

        builder.ConfigureMauiHandlers((handlers) => {
#if ANDROID
            handlers.AddCompatibilityRenderer(typeof(CustomWebView), typeof(CustomWebViewRenderer));
#endif
            handlers.AddUraniumUIHandlers();
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

		builder.Services.AddSingleton<SignalingService>();

		builder.Services.AddSingleton<ChatService>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<ChatPage>();

		builder.Services.AddTransient<GroupChatViewModel>();
		builder.Services.AddTransient<GroupChatPage>();
		builder.Services.AddTransient<GroupDetailsViewModel>();
		builder.Services.AddTransient<GroupDetailsPage>();

		builder.Services.AddTransient<CallViewModel>();
        builder.Services.AddTransient<CalleePage>();
        builder.Services.AddTransient<CallerPage>();
        builder.Services.AddTransient<WindowsCallPage>();
#if ANDROID
		builder.Services.AddTransient<AndroidCallPage>();
		builder.Services.AddTransient<AndroidGroupCallPage>();
#endif
		builder.Services.AddSingleton<CreateGroupPage>();
		builder.Services.AddSingleton<AddFriendPage>();
		builder.Services.AddSingleton<FriendRequestPage>();
		builder.Services.AddSingleton<SentFriendRequestPage>();

        builder.Services.AddSingleton<FeedbackViewModel>();
		builder.Services.AddSingleton<FeedbackService>();
		builder.Services.AddTransient<ReportABugPage>();
		builder.Services.AddTransient<UserReportedBugsPage>();
		builder.Services.AddSingleton<InformationPage>();
		builder.Services.AddSingleton<InformationViewModel>();
		builder.Services.AddSingleton<SubmitedFeedbackPage>();
		builder.Services.AddSingleton<NewFeedbackPage>();

        builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddSingleton<ProfileService>();
        builder.Services.AddTransient<EditProfilePage>();
		builder.Services.AddTransient<EditStatusPage>();

		builder.Services.AddSingleton<DesktopSettingsPage>();
		builder.Services.AddSingleton<SettingsViewModel>();

		builder.Services.AddTransient<UserProfilePage>();
		builder.Services.AddTransient<UserProfileViewModel>();

		builder.Services.AddTransient<WindowsGroupCallPage>();

		//Notification service
		builder.Services.AddSingleton<NotificationService>();

		//Database service
		builder.Services.AddSingleton<DatabaseService>();

		//The zoom image page
		builder.Services.AddTransient<ZoomImagePage>();
		builder.Services.AddTransient<ZoomImageViewModel>();

        return builder.Build();
	}
}
