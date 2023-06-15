using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class EditProfilePage : ContentPage
{
	public EditProfilePage(ProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}