using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IEmailRecurringJobService
    {
        void SendCandiatesToClient();
        void SendJobsToWorker();
        void SendApplicantsToClients();
        void SendClientMessageToPostJob();
        void SendWorkersMessageToCompleteProfile();
    }
}
