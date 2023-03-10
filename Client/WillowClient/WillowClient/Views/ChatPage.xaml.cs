using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using WillowClient.Model;
using WillowClient.ViewModel;
using WillowClient.ViewsPopups;

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

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) {
        //Show the pop up to react to a message
        var senderGrid = sender as Grid;
        var border = senderGrid.Children[1] as Border;
        var vstLayout = border.Content as VerticalStackLayout;
        var labelMessage = vstLayout.Children[0] as Label;
        string message = labelMessage.Text;
        var reactPopup = new ReactMessagePopup(message);
        var childBorder = senderGrid.Children[0] as Label;
        int messageId = Int32.Parse(childBorder.Text);
        //reactPopup.Anchor = childBorder;
        var result = await this.ShowPopupAsync(reactPopup) as string;

        //Send the message to the chat service to register the reaction
        if (result != null) {
            var vm = BindingContext as ChatViewModel;
            vm.ReactToMessage(messageId, result);
        }
    }
}