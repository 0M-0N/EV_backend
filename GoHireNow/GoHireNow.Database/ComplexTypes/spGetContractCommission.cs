using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class spGetContractCommission
  {
    [Key]
    public decimal curr { get; set; }
    public decimal next { get; set; }
    public decimal amount { get; set; }
  }
}