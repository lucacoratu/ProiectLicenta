using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileFriendsPage : ContentPage
{
	public MobileFriendsPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void TextField_TextChanged(object sender, TextChangedEventArgs e) {
        string newText = e.NewTextValue;
        var vm = BindingContext as MainViewModel;
        vm.SearchbarFriendsTextChanged(newText);
    }
}