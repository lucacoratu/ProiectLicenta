using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileMainPage : TabbedPage
{
	public MobileMainPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

		//Create the pages
		this.Children.Add(new MobileGroupsPage(vm));
		this.Children.Add(new MobileFriendsPage(vm));
		this.Children.Add(new MobileSettingsPage());
	}

    private void TabbedPage_Loaded(object sender, EventArgs e)
    {
        var vm = BindingContext as MainViewModel;
        vm.LoadData();
    }
}