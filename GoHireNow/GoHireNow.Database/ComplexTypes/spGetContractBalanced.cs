using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetContractBalanced
  {
    [Key]
    public decimal contractbalance { get; set; }
  }
}