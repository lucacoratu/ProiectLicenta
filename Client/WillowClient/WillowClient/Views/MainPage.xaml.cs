#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

using WillowClient.Model;
using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MainPage : ContentPage
{
    const int WindowWidth = 900;
    const int WindowHeight = 700;
    private const uint AnimationDuration = 800u;
    private bool MenuOpened = false;
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            var mauiWindow = handler.VirtualView;
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
#endif
        });

    }

    async void MenuClicked(System.Object sender, System.EventArgs e)
    {
        if (!this.MenuOpened)
        {
            _ = ContentGrid.TranslateTo(-this.Width * 0.25, 0, AnimationDuration, Easing.CubicIn);
            await ContentGrid.ScaleTo(1, AnimationDuration);
            _ = ContentGrid.FadeTo(1, AnimationDuration);
            this.MenuOpened = true;
        }
    }

    async void CloseMenu(System.Object sender, System.EventArgs e)
    {
        if (this.MenuOpened)
        {
            _ = ContentGrid.FadeTo(1, AnimationDuration);
            _ = ContentGrid.ScaleTo(1, AnimationDuration);
            await ContentGrid.TranslateTo(0, 0, AnimationDuration, Easing.CubicIn);
            this.MenuOpened = false;
        }
    }

    private void PageLoaded(object sender, EventArgs e)
    {
        var vm = BindingContext as MainViewModel;
        vm.LoadData();
    }

    private void createGroupCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        List<FriendModel> current = new();
        foreach(var element in e.CurrentSelection)
        {
            current.Add(element as FriendModel);
        }
        var vm = BindingContext as MainViewModel;
        if(vm != null)
            vm.CreateGroupSelectionChanged(current);
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        //Clear the data after creating the group
        this.createGroupCollectionView.SelectedItems = null;
    }
}

