using GoHireNow.Models.CommonModels.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Models.ClientModels
{
    public class ApplicantFilterRequest
    {
        public string[] KeywordIn { get; set; }
        public string[] KeywordOut { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public string MinimumWord { get; set; }
        public int[] SkillsIn { get; set; }
        public int[] SkillsOut { get; set; }
    }
}
