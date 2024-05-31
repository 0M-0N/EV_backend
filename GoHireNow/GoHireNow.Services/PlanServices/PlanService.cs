using GoHireNow.Database;
using GoHireNow.Service.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GoHireNow.Service.PlanServices
{
    public class PlanService : IPlanService
    {
        public List<Database.GlobalPlans> GetAllPlans()
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.GlobalPlans.ToList();
            }
        }
    }
}
