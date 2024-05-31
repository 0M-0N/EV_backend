using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.ClientModels
{
    public class JobsListModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string Applicants { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
    }
}
