using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class GlobalUpgrades
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int? ProductId { get; set; }
        public Boolean isActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
