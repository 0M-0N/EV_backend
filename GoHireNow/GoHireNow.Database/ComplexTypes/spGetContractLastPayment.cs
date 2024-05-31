using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetContractLastPayment
  {
    [Key]
    public decimal lastpayment { get; set; }
  }
}