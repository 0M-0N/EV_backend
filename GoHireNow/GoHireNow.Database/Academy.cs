using System;

namespace GoHireNow.Database
{
  public partial class Academy
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string CoverImg { get; set; }
    public DateTime CreatedDate { get; set; }
    public int IsDeleted { get; set; }
  }
}
