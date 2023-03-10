using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class ReactionModel {
        public int Id { get; set; } 
        public int SenderId { get; set; }
        public string Emoji { get; set; }
        public string ReactionDate { get; set; }
    }
}
