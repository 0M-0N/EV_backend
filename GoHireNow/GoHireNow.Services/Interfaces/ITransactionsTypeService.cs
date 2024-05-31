using System.Collections.Generic;

namespace GoHireNow.Service.Interfaces
{
    public interface ITransactionsTypeService
    {
        List<Database.TransactionsType> GetAllTransactionsTypes();
    }
}
