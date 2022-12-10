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
    }
}
