using CommunityToolkit.Maui.Views;
using WillowClient.ViewModel;
using WillowClient.ViewsPopups;

namespace WillowClient.Views;

public partial class GroupChatPage : ContentPage
{
	public GroupChatPage(GroupChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        var vm = BindingContext as GroupChatViewModel;
        vm.CreateGroupParticipantsList();
        vm.GetHistory();
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) {
        //Show the pop up to react to a message
        var senderGrid = sender as Grid;
        var border = senderGrid.Children[1] as Border;
        var vstLayout = border.Content as VerticalStackLayout;
        var labelMessage = vstLayout.Children[1] as Label;
        string message = labelMessage.Text;
        var reactPopup = new ReactMessagePopup(message);
        var childBorder = senderGrid.Children[0] as Label;
        int messageId = Int32.Parse(childBorder.Text);
        //reactPopup.Anchor = childBorder;
        var result = await this.ShowPopupAsync(reactPopup) as string;

        //Send the message to the chat service to register the reaction
        if (result != null) {
            var vm = BindingContext as GroupChatViewModel;
            vm.ReactToMessage(messageId, result);
        }
    }
}