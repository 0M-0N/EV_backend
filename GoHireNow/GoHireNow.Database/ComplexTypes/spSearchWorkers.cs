using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spSearchWorkers
    {
        public DateTime? LastLoginTime { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int TotalWorkers { get; set; }
        [Key]
        public int UserUniqueId { get; set; }

        public int? CountryId { get; set; }

        public int? UserType { get; set; }

        public string UserId { get; set; }
        public string WorkerSkills { get; set; }

        public string UserSalary { get; set; }

        public string UserAvailiblity { get; set; }

        public string UserTitle { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string ProfilePicture { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
    }
}