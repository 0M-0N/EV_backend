using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spIsCapableClient
    {
        [Key]
        public long Id { get; set; }
        public bool IsCapable { get; set; }
        public string Message { get; set; }
        public string Stat { get; set; }
    }
}
