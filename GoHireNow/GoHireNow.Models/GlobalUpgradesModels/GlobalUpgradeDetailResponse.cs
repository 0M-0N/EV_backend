using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.GlobalUpgradeModels
{
    public class GlobalUpgradeDetailResponse
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int? ProductId { get; set; }
        public Boolean isActive { get; set; }
        public DateTime? CreatedDate { get; set; }

    }
}
