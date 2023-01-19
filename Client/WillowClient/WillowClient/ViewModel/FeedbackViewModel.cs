using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WillowClient.Model;
using WillowClient.Services;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(HexID), "hexID")]
    [QueryProperty(nameof(Session), "session")]
    public partial class FeedbackViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string session;

        [ObservableProperty]
        private string hexID;

        [ObservableProperty]
        private ObservableCollection<FeedbackServiceModel> feedbackServiceModels = new();

        FeedbackService feedbackService;

        public FeedbackViewModel(FeedbackService feedbackService)
        {
            this.feedbackService = feedbackService;
            this.PopulateCollectionView();
        }

        public void PopulateCollectionView()
        {
            this.feedbackServiceModels.Add(new FeedbackServiceModel { ImageSource = "feedback_account.png", Description = "Account" });
            this.feedbackServiceModels.Add(new FeedbackServiceModel { ImageSource = "feedback_chat.png", Description = "Chat" });
            this.feedbackServiceModels.Add(new FeedbackServiceModel { ImageSource = "feedback_voice.png", Description = "Voice" });
            this.feedbackServiceModels.Add(new FeedbackServiceModel { ImageSource = "feedback_video.png", Description = "Video" });
        }

        [RelayCommand]
        async Task ExitReportABug()
        {
            _ = await Shell.Current.Navigation.PopAsync();
        }
        
    }
}
