using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.ViewModel {
    public partial class InformationViewModel : BaseViewModel {
        public InformationViewModel() {

        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }
    }
}
