using System;

namespace GoHireNow.Database
{
  public partial class Emails
  {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string ToUserId { get; set; }
    public int IsDeleted { get; set; }
    public int Type { get; set; }
    public DateTime CreatedDate { get; set; }
  }
}
