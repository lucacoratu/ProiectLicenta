using WillowClient.Model;
using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class EditStatusPage : ContentPage
{
	public EditStatusPage(ProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		var vm = BindingContext as ProfileViewModel;
		vm.CreateDefaultStatusModels();
        vm.GetLastSavedStatus();
    }

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        DefaultStatusModel statusChosen = null;
        foreach (var element in e.CurrentSelection)
            statusChosen = element as DefaultStatusModel;

        var vm = BindingContext as ProfileViewModel;
        if (vm != null) {
            vm.StatusSelectionChanged(statusChosen);
            statusChosen.IsSelected = true;
        }
    }
}