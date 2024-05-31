using System;

namespace GoHireNow.Database
{
  public partial class UserNotifications
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string CompanyId { get; set; }
    public int CustomId { get; set; }
    public string CustomName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int IsDelerte { get; set; }
  }
}
