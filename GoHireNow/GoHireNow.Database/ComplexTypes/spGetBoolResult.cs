using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetBoolResult
    {
        [Key]
        public bool Result { get; set; }
    }
}
