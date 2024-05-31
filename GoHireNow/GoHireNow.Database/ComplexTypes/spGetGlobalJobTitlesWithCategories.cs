using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetGlobalJobTitlesWithCategories
    {
        public int JobCategoryId { get; set; }

        public int JobId { get; set; }

        public long Id { get; set; }

        public string JobCategoryName { get; set; }

        public string FriendlyUrl { get; set; }

        public string JobName { get; set; }
    }

}
