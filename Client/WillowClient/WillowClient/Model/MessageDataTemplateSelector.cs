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
        public DataTemplate ReceivedPhotoMessageTemplate { get; set; }
        public DataTemplate ReceivedUndownloadedPhotoMessageTemplate { get; set; }
        public DataTemplate SentMessageTemplate { get; set; }
        public DataTemplate SentPhotoMessageTemplate { get; set; }
        public DataTemplate SentUndowloadedPhotoMessageTemplate { get; set; }
        public DataTemplate SentUndisplayableMessageTemplate { get; set; }

       protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
       {
            MessageModel message = item as MessageModel;
            if (message.Owner == MessageOwner.CurrentUser) {
                switch (message.MessageType) {
                    case MessageType.Text:
                        return SentMessageTemplate;
                    case MessageType.Photo:
                        if(message.IsDownloaded)
                            return SentPhotoMessageTemplate;
                        return SentUndowloadedPhotoMessageTemplate;
                    case MessageType.File:
                        return SentUndisplayableMessageTemplate;
                    default:
                        return SentMessageTemplate;
                }
            }
            else {
                switch (message.MessageType) {
                    case MessageType.Text:
                        return ReceivedMessageTemplate;
                    case MessageType.Photo:
                        if(message.IsDownloaded)
                            return ReceivedPhotoMessageTemplate;
                        return ReceivedUndownloadedPhotoMessageTemplate;
                    default:
                        return ReceivedMessageTemplate;
                }
            }
       }
    }
}
