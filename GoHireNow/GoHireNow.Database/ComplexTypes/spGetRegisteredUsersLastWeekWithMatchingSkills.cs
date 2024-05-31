using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{

    public class spGetRegisteredUsersLastWeekWithMatchingSkills
    {
        [Key]
        public long Id { get; set; }
        public string ClientId { get; set; }
        public string WorkerId { get; set; }
        public string WorkerName { get; set; }
        public string WorkerTitle { get; set; }
        public DateTime? WorkerLastLoginTime { get; set; }
        public string WorkerSkills { get; set; }
        public string WorkerAvailability { get; set; }
        public string WorkerSalary { get; set; }
        public string ClientEmail { get; set; }
        public string WorkerProfilePicture { get; set; }
    }
}
