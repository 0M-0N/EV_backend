using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class StripePayments
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public string CardId { get; set; }
        public string PaymentMethodId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsDeleted { get; set; }

    }
}
