using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class AcceptFriendRequestUpdateModel {
        public int accountID { get; set; }
        public int friendID { get; set; }
        public int roomID { get; set; }
    }
}
