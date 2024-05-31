using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{

    public class spGetRecentJobsLastDayWithMatchingSkills
    {
        [Key]
        public int JobId { get; set; }
        public decimal JobSalary { get; set; }
        public long Id { get; set; }
        public string ClientName { get; set; }
        public string WorkerId { get; set; }
        public string WorkerEmail { get; set; }
        public string JobType { get; set; }
        public string JobTitle { get; set; }
        public string JobSkills { get; set; }
        public string ClientProfilePicture { get; set; }
    }
}
