using InputKit.Shared.Validations;
using WillowClient.ViewModel;

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
}