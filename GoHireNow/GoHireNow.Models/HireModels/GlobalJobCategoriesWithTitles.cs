using System.Collections.Generic;

namespace GoHireNow.Models.HireModels
{
    public class GlobalJobCategoriesListModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<GlobalJobTitleListModel> JobTitles { get; set; }
    }
}
