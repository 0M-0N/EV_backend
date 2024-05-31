using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.ContractModels
{
    public class UpdateHoursModel
    {
        public decimal hours{ get; set; }
        public int ContractId { get; set; }
    }
}
