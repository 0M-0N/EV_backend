using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.AccountModels
{
    public class WorkerUpdateRequestModel
    {
        public WorkerUpdateRequestModel()
        {
            //Portfolios = new List<string>();
            //Skills = new List<int>();
        }
        public string ProfileName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int[] Skills { get; set; }
        public string Salary { get; set; }
        public int Availiblity { get; set; }
        public string Educations { get; set; }
        public string Experience { get; set; }
        public string[] Portfolios { get; set; }
        public string SkypeLink { get; set; }
        public string FacebookLink { get; set; }
        public string LinkedInLink { get; set; }
        public int CountryId { get; set; }
    }
}
