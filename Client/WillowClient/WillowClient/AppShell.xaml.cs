using WillowClient.Views;

namespace WillowClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
        Routing.RegisterRoute(nameof(GroupChatPage), typeof(GroupChatPage));
        Routing.RegisterRoute(nameof(MobileMainPage), typeof(MobileMainPage));
        Routing.RegisterRoute(nameof(WindowsCallPage), typeof(WindowsCallPage));
        Routing.RegisterRoute(nameof(CreateGroupPage), typeof(CreateGroupPage));
        Routing.RegisterRoute(nameof(AddFriendPage), typeof(AddFriendPage));
        Routing.RegisterRoute(nameof(FriendRequestPage), typeof(FriendRequestPage));
        Routing.RegisterRoute(nameof(SentFriendRequestPage), typeof(SentFriendRequestPage));
        Routing.RegisterRoute(nameof(CallerPage), typeof(CallerPage));
        Routing.RegisterRoute(nameof(CalleePage), typeof(CalleePage));
        Routing.RegisterRoute(nameof(ReportABugPage), typeof(ReportABugPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
        Routing.RegisterRoute(nameof(EditStatusPage), typeof(EditStatusPage));
        Routing.RegisterRoute(nameof(InformationPage), typeof(InformationPage));
        Routing.RegisterRoute(nameof(SubmitedFeedbackPage), typeof(SubmitedFeedbackPage));
        Routing.RegisterRoute(nameof(NewFeedbackPage), typeof(NewFeedbackPage));
        Routing.RegisterRoute(nameof(DesktopSettingsPage), typeof(DesktopSettingsPage));
        Routing.RegisterRoute(nameof(GroupDetailsPage), typeof(GroupDetailsPage));
        Routing.RegisterRoute(nameof(UserProfilePage), typeof(UserProfilePage));
        Routing.RegisterRoute(nameof(WindowsGroupCallPage), typeof(WindowsGroupCallPage));
#if ANDROID
        Routing.RegisterRoute(nameof(AndroidCallPage), typeof(AndroidCallPage));
        Routing.RegisterRoute(nameof(AndroidGroupCallPage), typeof(AndroidGroupCallPage));
#endif
    }
}
