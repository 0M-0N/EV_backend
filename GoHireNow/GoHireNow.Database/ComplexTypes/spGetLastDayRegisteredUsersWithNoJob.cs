using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetLastDayRegisteredUsersWithNoJob
    {
        [Key]
        public int TotalJobsPosted { get; set; }

        public string Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

    }
}
