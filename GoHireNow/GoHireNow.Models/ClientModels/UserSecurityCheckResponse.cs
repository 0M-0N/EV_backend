using System;

namespace GoHireNow.Models.ClientModels
{
  public class UserSecurityCheckResponse
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string CompanyId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateDate { get; set; }
  }
}