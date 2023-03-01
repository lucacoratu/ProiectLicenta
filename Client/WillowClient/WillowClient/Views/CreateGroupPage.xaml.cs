using WillowClient.ViewModel;
using WillowClient.Model;
using InputKit.Shared.Validations;

namespace WillowClient.Views;

public partial class CreateGroupPage : ContentPage
{
	public CreateGroupPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
        this.groupNameTextField.Validations.Add(new RequiredValidation());
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
        if(this.groupNameTextField.Text == null || this.groupNameTextField.Text.Length == 0)
            this.groupNameTextField.DisplayValidation();
        this.groupNameTextField.IsEnabled = false;
        this.groupNameTextField.IsEnabled = true;
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        var vm = BindingContext as MainViewModel;
        vm.LoadData();
    }

    private void SearchInputTextChanged(object sender, TextChangedEventArgs e) {
        string newText = e.NewTextValue;
        var vm = BindingContext as MainViewModel;
        vm.SearchbarCreateGroupTextChanged(newText);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) {
        var vm = BindingContext as MainViewModel;
        vm.SelectImageForNewGroup(this.groupPicture);
    }

    private void groupPicture_Clicked(object sender, EventArgs e) {
        var vm = BindingContext as MainViewModel;
        vm.SelectImageForNewGroup(this.groupPicture);
    }

    private void ContentPage_Unloaded(object sender, EventArgs e) {
        this.groupPicture.Source = ImageSource.FromFile("add_picture.png");
    }
}