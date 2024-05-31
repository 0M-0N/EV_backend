using GoHireNow.Models.CommonModels.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Models.ClientModels
{
    public class PostJobRequest
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public List<int> JobSkillIds { get; set; }
        [Required]
        [Range(1, (int)JobTypeEnum.FreeLance, ErrorMessage = "Invalid jobtypeId")]
        public int JobTypeId { get; set; }
        [Required]
        [Range(1, (int)SalaryTypeEnum.Fixed, ErrorMessage = "Invalid salaryTypeId")]
        public int SalaryTypeId { get; set; }
        [Required]
        public decimal Salary { get; set; }
        public List<string> Attachments { get; set; }
        public bool isEmail { get; set; }
    }
}
