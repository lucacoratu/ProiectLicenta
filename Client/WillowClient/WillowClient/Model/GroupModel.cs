using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public partial class GroupModel
    {
        public int RoomId { get; set; }
	    public int CreatorId { get; set; }
	    public string GroupName { get; set; }
        public List<int> Participants { get; set; }
    }
}
