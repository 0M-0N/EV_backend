using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class SearchWorkerRequest
    {
        public string Keyword { get; set; }
        public string SkillIds { get; set; }
        public int? CountryId { get; set; }
        public int? WorkerTypeId { get; set; }
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
        public int page { get; set; }
        public int size { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public decimal MinRating { get; set; }
        public int? Id { get; set; }
    }
}
