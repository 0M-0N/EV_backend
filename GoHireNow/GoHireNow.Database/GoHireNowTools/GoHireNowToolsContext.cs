using GoHireNow.Database.GoHireNowTools.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHireNow.Database.GoHireNowTools
{
    public partial class GoHireNowToolsContext : DbContext
    {
        public GoHireNowToolsContext()
        {
        }

        public GoHireNowToolsContext(DbContextOptions<GoHireNowToolsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<mailer_sender> mailer_sender { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=15.235.86.108; Database=gohirenow_tools;User Id=db_tools_emails; Password=ft7uuuuuy6GH!?", options => options.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");
        }
    }
}
