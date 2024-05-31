using GoHireNow.Database;
using GoHireNow.Service.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GoHireNow.Service.TransactionsTypeServices
{
  public class TransactionsTypeService : ITransactionsTypeService
  {
    public List<Database.TransactionsType> GetAllTransactionsTypes()
    {
      using (var _context = new GoHireNowContext())
      {
        return _context.TransactionsType.ToList();
      }
    }
  }
}
