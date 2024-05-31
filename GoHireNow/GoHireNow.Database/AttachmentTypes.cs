using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class AttachmentTypes
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? CreateDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
