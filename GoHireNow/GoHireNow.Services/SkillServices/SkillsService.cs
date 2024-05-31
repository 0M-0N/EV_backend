using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GoHireNow.Service.SkillServices
{
    public class SkillsService : ISkillsService
    {

        public List<SkillResponse> GetAllSkills()
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.GlobalSkills.Select(s => new SkillResponse()
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList();
            }
        }

        public GlobalSkills GetSkill(int skillId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.GlobalSkills.FirstOrDefault(x => x.Id == skillId);
            }
        }

        public List<string> GetSkillNamesByJobSkills(int[] skills)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.GlobalSkills.Where(s => skills.Contains(s.Id)).Select(s => s.Name).ToList();
            }
        }

        public async Task<bool> AddUserSkills(string userId, int[] skills)
        {
            var newSkills = MapUserSkills(userId, skills.ToList());
            if (newSkills == null)
                throw new CustomException((int)HttpStatusCode.BadRequest, "Inavlid skillId");

            using (var _context = new GoHireNowContext())
            {
                var existingSkills = _context.UserSkills.Where(x => x.UserId == userId).ToList();
                if (existingSkills.Any())
                {
                    var existingSkillsIds = existingSkills.Select(x => x.SkillId).ToList();
                    var areSameSkills = SameSkills(existingSkillsIds, skills.ToList());

                    if (!areSameSkills)
                    {
                        _context.UserSkills.RemoveRange(existingSkills);
                        _context.UserSkills.AddRange(newSkills);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.UserSkills.AddRange(newSkills);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
        }

        private List<UserSkills> MapUserSkills(string userId, List<int> skillIds)
        {
            var newUserSkills = new List<UserSkills>();
            var validSkillIds = LookupService.GetSkillsById(skillIds);
            foreach (var item in validSkillIds)
            {

                newUserSkills.Add(new UserSkills() { SkillId = item.Id, UserId = userId, CreateDate = DateTime.UtcNow, IsDeleted = false });
            }
            return newUserSkills;
        }

        private bool SameSkills(List<int> oldSkills, List<int> newSkills)
        {
            var distinctA = oldSkills.Except(newSkills).ToList();

            var distinctB = newSkills.Except(oldSkills).ToList();

            if (distinctA.Count() == 0 && distinctB.Count() == 0)
                return true;

            return false;
        }

        public async Task<SkillResponse[]> GetUserSkills(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var skills = await _context.UserSkills.Where(x => x.UserId == userId).ToListAsync();
                return skills.Select(x => new SkillResponse
                {
                    Id = x.SkillId,
                    Name = x.Skill.Name
                }).ToArray();
            }
        }

        public int GetUserSkillsCount(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.UserSkills.Where(x => x.UserId == userId).Count();
            }
        }

        public async Task<bool> RemoveUserSkills(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var existingSkills = _context.UserSkills.Where(x => x.UserId == userId).ToList();
                if (existingSkills.Any())
                {
                    _context.UserSkills.RemoveRange(existingSkills);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
        }

    }
}
