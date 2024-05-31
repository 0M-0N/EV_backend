using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class TransactionsType
    {
        public TransactionsType()
        {
            Transactions = new HashSet<Transactions>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Transactions> Transactions { get; set; }
    }
}
