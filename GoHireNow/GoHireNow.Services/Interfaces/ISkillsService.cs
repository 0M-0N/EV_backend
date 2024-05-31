using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface ISkillsService
    {
        List<SkillResponse> GetAllSkills();
        GlobalSkills GetSkill(int skillId);
        List<string> GetSkillNamesByJobSkills(int[] skills);
        Task<bool> AddUserSkills(string userId, int[] skills);
        Task<SkillResponse[]> GetUserSkills(string userId);
        int GetUserSkillsCount(string userId);
        Task<bool> RemoveUserSkills(string userId);
    }
}
