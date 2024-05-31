using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class MatchingJobResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Salary { get; set; }
        public DateTime? ActiveDate { get; set; }
        public List<SkillResponse> Skills { get; set; }
    }
}
