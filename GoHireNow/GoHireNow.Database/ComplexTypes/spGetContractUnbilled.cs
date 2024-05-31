using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetContractUnbilled
  {
    [Key]
    public decimal unbilledhours { get; set; }
  }
}