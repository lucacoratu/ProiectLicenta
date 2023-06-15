using InputKit.Shared.Validations;
using WillowClient.ViewModel;
using WillowClient.Model;

namespace WillowClient.Views;

public partial class AddFriendPage : ContentPage
{
	public AddFriendPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		this.entryAddFriend.Validations.Add(new RequiredValidation());
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		var vm = BindingContext as MainViewModel;
		vm.GetFriendRequestRecommendations();
    }

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		List<FriendRecommendationModel> currentSelectedItems = new();
		foreach(var selectedItem in e.CurrentSelection) {
			currentSelectedItems.Add(selectedItem as FriendRecommendationModel);
		}
		//Update the selected items in the view model
		var vm = BindingContext as MainViewModel;
		vm.FriendRecommendationSelectionChanged(currentSelectedItems);
    }

    private void searchRecommendations_TextChanged(object sender, TextChangedEventArgs e) {
		var vm = BindingContext as MainViewModel;
		vm.SearchbarFriendRecommendationsTextChanged(e.NewTextValue);
    }
}