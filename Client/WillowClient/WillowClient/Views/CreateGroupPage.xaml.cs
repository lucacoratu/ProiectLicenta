using WillowClient.ViewModel;
using WillowClient.Model;

namespace WillowClient.Views;

public partial class CreateGroupPage : ContentPage
{
	public CreateGroupPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void createGroupCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        List<FriendStatusModel> current = new();
        foreach (var element in e.CurrentSelection)
        {
            current.Add(element as FriendStatusModel);
        }
        var vm = BindingContext as MainViewModel;
        if (vm != null)
            vm.CreateGroupSelectionChanged(current);
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        //Clear the data after creating the group
        this.createGroupCollectionView.SelectedItems = null;
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        var vm = BindingContext as MainViewModel;
        vm.LoadData();
    }
}