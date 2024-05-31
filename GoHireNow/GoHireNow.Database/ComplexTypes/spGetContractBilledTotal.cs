using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetContractBilledTotal
  {
    [Key]
    public decimal billedTotal { get; set; }
  }
}