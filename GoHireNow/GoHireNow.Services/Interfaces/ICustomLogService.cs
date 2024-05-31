using System.Threading.Tasks;
using GoHireNow.Models.CommonModels;
using Microsoft.AspNetCore.Http;

namespace GoHireNow.Service.Interfaces
{
    public interface ICustomLogService
    {
        void LogError(LogErrorRequest error);
        void LogSupport(LogSupportRequest request);
        void LogHRSupport(LogSupportRequest request);
        void LogPayout(LogSupportRequest request);
        Task<bool> ValidateFiles(IFormFileCollection files);
    }
}
