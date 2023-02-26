using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class UserProfilePage : ContentPage
{
	public UserProfilePage(UserProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		var vm = BindingContext as UserProfileViewModel;
		vm.PopulateProfileData();
		vm.PopulateCommonGroups();
    }
}