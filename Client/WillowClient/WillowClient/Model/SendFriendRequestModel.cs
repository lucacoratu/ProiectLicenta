using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class SendFriendRequestModel
    {
        //This is the receiver of the friend request
        public int accountId { get; set; }

        //This is the sender of the friend request
        public int friendId { get; set; }
    }
}
