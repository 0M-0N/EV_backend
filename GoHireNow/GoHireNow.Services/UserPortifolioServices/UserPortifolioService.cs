using GoHireNow.Database;
using GoHireNow.Service.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GoHireNow.Service.UserPortifolioServices
{
    public class UserPortifolioService : IUserPortifolioService
    {
        public int AddUserPortifolio(UserPortfolios model)
        {
            using (var _context = new GoHireNowContext())
            {
                var entity = _context.UserPortfolios.Add(model);
                _context.SaveChanges();
                return entity.Entity.Id;
            }
        }

        public bool DeleteUserPortifolios(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var portfolios = _context.UserPortfolios.Where(x => x.UserId == userId);
                if (portfolios.Any())
                {
                    _context.UserPortfolios.RemoveRange(portfolios);
                    _context.SaveChanges();
                }
                return true;
            }

        }

        public List<UserPortfolios> GetUserPortifolios(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.UserPortfolios.Where(x => x.UserId == userId).ToList(); ;
            }
        }


    }
}
