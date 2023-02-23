using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class AddBugReportModel
    {
        public string category { get; set; }
        public string description { get; set; }
        public int reportedBy { get; set; }
    }
}
