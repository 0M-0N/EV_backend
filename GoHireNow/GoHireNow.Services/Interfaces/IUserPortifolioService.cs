using GoHireNow.Database;
using System.Collections.Generic;

namespace GoHireNow.Service.Interfaces
{
    public interface IUserPortifolioService
    {
        int AddUserPortifolio(UserPortfolios model);
        bool DeleteUserPortifolios(string userId);
        List<UserPortfolios> GetUserPortifolios(string userId);
    }
}
