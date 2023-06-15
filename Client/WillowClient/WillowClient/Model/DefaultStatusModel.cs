using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class DefaultStatusModel : ObservableObject
    {
        private string text;

        public string Text {
            get => text;
            set => SetProperty(ref text, value);
        }

        private bool isSelected;
        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public bool IsLineVisible { get; set; } 
    }
}
