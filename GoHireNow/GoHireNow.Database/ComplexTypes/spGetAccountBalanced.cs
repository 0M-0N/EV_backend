using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetAccountBalanced
  {
    [Key]
    public decimal amount { get; set; }
  }
}