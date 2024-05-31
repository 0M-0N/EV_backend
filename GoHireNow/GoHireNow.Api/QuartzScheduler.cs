using GoHireNow.Api.Controllers;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.CommonServices;
using Quartz;
using System.Threading.Tasks;

namespace GoHireNow.Api
{
    public class WorkerHourJob : IJob
    {
        ContractController _contractController;

        public WorkerHourJob(ContractController contractController)
        {
            _contractController = contractController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _contractController.WorkerHourEmailAction();
            return Task.CompletedTask;
        }
    }

    public class ReleaseJob : IJob
    {
        PaymentController _paymentController;

        public ReleaseJob(PaymentController paymentController)
        {
            _paymentController = paymentController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _paymentController.ReleaseAction();
            return Task.CompletedTask;
        }
    }

    public class ActionJob : IJob
    {
        PaymentController _paymentController;

        public ActionJob(PaymentController paymentController)
        {
            _paymentController = paymentController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _paymentController.PaymentAction();
            return Task.CompletedTask;
        }
    }

    public class HRWorkerHoursJob : IJob
    {
        PaymentController _paymentController;

        public HRWorkerHoursJob(PaymentController paymentController)
        {
            _paymentController = paymentController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _paymentController.HRWorkerHoursAction();
            return Task.CompletedTask;
        }
    }

    public class AutoWithdrawJob : IJob
    {
        PaymentController _paymentController;

        public AutoWithdrawJob(PaymentController paymentController)
        {
            _paymentController = paymentController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _paymentController.AutoWithdrawAction();
            return Task.CompletedTask;
        }
    }

    public class AutoChargeForHRJob : IJob
    {
        PaymentController _paymentController;

        public AutoChargeForHRJob(PaymentController paymentController)
        {
            _paymentController = paymentController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _paymentController.AutoChargeForHRAction();
            return Task.CompletedTask;
        }
    }

    public class SendSecondEmailJob : IJob
    {
        private readonly ContractController _contractController;

        public SendSecondEmailJob(ContractController contractController)
        {
            _contractController = contractController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            string userId = context.JobDetail.JobDataMap.GetString("UserId");
            int id = context.JobDetail.JobDataMap.GetInt("ReferenceId");
            int type = context.JobDetail.JobDataMap.GetInt("Type");
            _contractController.SendSecondEmail(userId, id, type);
            return Task.CompletedTask;
        }
    }

    public class SendMeetingLinkJob : IJob
    {
        private readonly MessagesController _messagesController;

        public SendMeetingLinkJob(MessagesController messagesController)
        {
            _messagesController = messagesController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var interviewId = context.JobDetail.JobDataMap.GetInt("InterviewId");
            _messagesController.SendMeetingLink(interviewId);
            return Task.CompletedTask;
        }
    }

    public class PendingContractJob : IJob
    {
        ContractController _contractController;

        public PendingContractJob(ContractController contractController)
        {
            _contractController = contractController;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _contractController.PendingContractAction();
            return Task.CompletedTask;
        }
    }
}
