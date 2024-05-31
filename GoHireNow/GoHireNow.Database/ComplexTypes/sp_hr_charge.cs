using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.ComplexTypes
{
  public class sp_hr_charge
  {
    [Key]
    public int id { get; set; }
    public string clientname { get; set; }
    public string companyId { get; set; }
    public int contractId { get; set; }
    public decimal maxhours { get; set; }
    public decimal used { get; set; }
    public decimal available { get; set; }
    public decimal amount { get; set; }
  }
}