using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.GlobalJobsModels
{
    public class GlobalJobRequest
    {
        public int jobTitleId { get; set; }
        public int page { get; set; }
        public int size { get; set; }
    }
}
