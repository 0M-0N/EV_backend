using System;

namespace GoHireNow.Database
{
    public partial class UserYoutubes
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int IsDeleted { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}
