using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileGroupsPage : ContentPage
{
	public MobileGroupsPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void TextField_TextChanged(object sender, TextChangedEventArgs e) {
        string newText = e.NewTextValue;
        var vm = BindingContext as MainViewModel;
        vm.SearchbarGroupsTextChanged(newText);
    }
}