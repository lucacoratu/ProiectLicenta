﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public partial class CommonGroupModel : ObservableObject {
        public int RoomId { get; set; }
        public string GroupName { get; set; }   
	    public int CreatorId { get; set; }
	    public string CreationDate { get; set; }  
	    public List<int> Participants { get; set; }
	    public List<string> ParticipantNames { get; set; }  
    }
}
