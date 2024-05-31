using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserPictures
    {
        public int Id { get; set; }
        public string PictureFile { get; set; }
        public string UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
