using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.ViewModel {
    public partial class CallFeedbackViewModel : BaseViewModel {

        [ObservableProperty]
        private string feedbackText;

        public CallFeedbackViewModel() {

        }

        //Function for generating the text
        private void GenerateFeedbackText() {
            Random rnd = new Random();
            int choice = rnd.Next(0, 2);
            switch (choice) {
                case 0:
                    FeedbackText = "How was the audio quality?";
                    break;
                case 1:
                    FeedbackText = "How was the video quality?";
                    break;
                default:
                    FeedbackText = "How was the audio quality?";
                    break;
            }

        }

        public void LoadData() {
            GenerateFeedbackText();
        }
    }
}
