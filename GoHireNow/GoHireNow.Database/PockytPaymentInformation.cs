using System;

namespace GoHireNow.Database
{
  public partial class PockytPaymentInformation
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string CustomerNo { get; set; }
    public string VaultId { get; set; }
    public decimal Amount { get; set; }
    public string Vendor { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int IsDeleted { get; set; }
  }
}
