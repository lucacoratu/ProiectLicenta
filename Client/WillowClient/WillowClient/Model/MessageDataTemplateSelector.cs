using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class MessageDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ReceivedMessageTemplate { get; set; }
        public DataTemplate SentMessageTemplate { get; set; }

       protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
       {
            MessageModel message = item as MessageModel;
            if (message.Owner == MessageOwner.CurrentUser)
                return SentMessageTemplate;
            else
                return ReceivedMessageTemplate;
       }
    }
}
