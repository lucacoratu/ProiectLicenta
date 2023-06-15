using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class GroupDetailsPage : ContentPage
{
	public GroupDetailsPage(GroupDetailsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		GroupDetailsViewModel vm = BindingContext as GroupDetailsViewModel;
		vm.PopulateGroupParticipants();
		vm.PrepareUI();
    }
}