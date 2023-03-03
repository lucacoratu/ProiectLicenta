using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage(ProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) {
		var vm = BindingContext as ProfileViewModel;
		vm.ChangeProfilePicture(this.profilePicture);
    }
}