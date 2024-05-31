using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetGlobalGroupByCountry
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Page { get; set; }
        public string Email { get; set; }
    }
}
