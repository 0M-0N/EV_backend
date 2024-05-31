using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.HireModels
{
   public class HireWorkerRequest
    {
        public int jobTitleId { get; set; }
        public int page { get; set; }
        public int size { get; set; }
    }
}
