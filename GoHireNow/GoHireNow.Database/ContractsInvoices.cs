using System;

namespace GoHireNow.Database
{
  public partial class ContractsInvoices
  {
    public int Id { get; set; }
    public int ContractId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? PayoutDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal Hours { get; set; }
    public decimal Amount { get; set; }
    public decimal PayoutCommission { get; set; }
    public int PayoutStatusId { get; set; }
    public int StatusId { get; set; }
    public int? InvoiceType { get; set; }
    public int? PayoutId { get; set; }
    public int IsDeleted { get; set; }
    public int? SecuredId { get; set; }
    public string Comment { get; set; }
    public virtual Contracts Contract { get; set; }
  }
}