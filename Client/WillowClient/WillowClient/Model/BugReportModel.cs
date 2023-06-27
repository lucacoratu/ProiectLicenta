using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class BugReportModel {
        public int ID { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string ReportedBy {get; set; }
        public string Timestamp { get; set; }
    }
}
