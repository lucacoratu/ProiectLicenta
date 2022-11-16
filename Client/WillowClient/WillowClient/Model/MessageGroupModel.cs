using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class MessageGroupModel : List<MessageModel>
    {
        public string Name { get; set; }

        public MessageGroupModel(string name, List<MessageModel> messages) : base(messages)
        {
            Name = name;
        }
    }
}
