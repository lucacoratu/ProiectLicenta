using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class MessageGroupModel : ObservableCollection<MessageModel>, INotifyPropertyChanged
    {
        private string name;
        public string Name 
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
                NotifyPropertyChanged();
            }
        }

        public MessageGroupModel(string name, List<MessageModel> messages) : base(messages)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
