using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ChatPage : ContentPage
{
	public ChatPage(ChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}