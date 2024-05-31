using System.Collections.Generic;

namespace GoHireNow.Service.Interfaces
{
    public interface IPlanService
    {
        List<Database.GlobalPlans> GetAllPlans();
    }
}
