using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class SearchJobRequest
    {
        public string Keyword { get; set; }
        public string SkillIds { get; set; }
        public int? CountryId { get; set; }
        public int? WorkerTypeId { get; set; }
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
        public int page { get; set; }
        public int size { get; set; }
    }
}
