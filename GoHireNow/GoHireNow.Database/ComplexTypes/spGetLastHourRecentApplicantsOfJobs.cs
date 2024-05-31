using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{

    public class spGetLastHourRecentApplicantsOfJobs
    {
        [Key]
        public int TotalJobApplications { get; set; }

        public int JobId { get; set; }

        public long Id { get; set; }

        public string ClientId { get; set; }
        public string WorkerName { get; set; }

        public string ProfilePicture { get; set; }

        public string WorkerId { get; set; }
        public string JobTitle { get; set; }

        public string ClientEmail { get; set; }

        public string WorkerTitle { get; set; }

        public string UserSkills { get; set; }
        public string CoverLetter { get; set; }
        public string SkilliNameList { get; set; }
        public string rating { get; set; }


    }
}
