using WillowClient.ViewModel;

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
}