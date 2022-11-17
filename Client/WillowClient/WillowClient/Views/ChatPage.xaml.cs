using Microsoft.Maui.Controls;
using WillowClient.Model;
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

        //MessageGroupModel group = vm.MessageGroups.FirstOrDefault(a => a.Name == "Today");
        //MessageModel monkey = group[group.Count - 1];
        //messageGrid.ScrollTo(monkey, group);
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        ChatViewModel viewModel = BindingContext as ChatViewModel;
        MessageGroupModel group = viewModel.MessageGroups.FirstOrDefault(a => a.Name == "Today");
        MessageModel monkey = group[group.Count - 1];
        messageGrid.ScrollTo(monkey, group);
    }
}