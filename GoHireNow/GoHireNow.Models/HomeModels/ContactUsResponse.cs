using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.HomeModels
{
    public class ContactUsResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
    }
}
