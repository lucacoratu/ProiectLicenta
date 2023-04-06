using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(ImageFilename), "imageFilename")]
    public partial class ZoomImageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string imageFilename;

        [RelayCommand]
        async Task GoBack() {
            _ = await Shell.Current.Navigation.PopAsync(true);
        }
    }
}
