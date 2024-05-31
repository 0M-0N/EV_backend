using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetJobTitleRelatedJobs
    {
        public DateTime? LastLoginTime { get; set; }

        public DateTime? RegistrationDate { get; set; }

        public int Id { get; set; }

        public int JobTypeId { get; set; }

        public int JobStatusId { get; set; }

        public int SalaryTypeId { get; set; }

        public int UserUniqueId { get; set; }

        public int? CountryId { get; set; }

        public DateTime? ActiveDate { get; set; }

        public decimal Salary { get; set; }

        public string JobBigTitle { get; set; }

        public string JobDescripton { get; set; }

        public string JobTitle { get; set; }

        public string CompanyName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ProfilePicture { get; set; }

        public string ClientId { get; set; }

        public string JobSkills { get; set; }
    }

}
