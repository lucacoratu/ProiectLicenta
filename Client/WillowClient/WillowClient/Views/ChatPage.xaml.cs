using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ChatPage : ContentPage
{
	public ChatPage(ChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void PageLoaded(object sender, EventArgs e)
    {
		var vm = BindingContext as ChatViewModel;
		vm.GetRoomId();
		//vm.GetHistory();
    }
}