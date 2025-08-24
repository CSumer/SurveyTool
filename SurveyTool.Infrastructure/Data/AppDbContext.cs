using Microsoft.EntityFrameworkCore;
using SurveyTool.Core.Domain;

namespace SurveyTool.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<AnswerOption> Options => Set<AnswerOption>();
        public DbSet<SurveyResponse> Responses => Set<SurveyResponse>();
        public DbSet<ResponseItem> ResponseItems => Set<ResponseItem>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Survey>().HasMany(s => s.Questions).WithOne().HasForeignKey(q => q.SurveyId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Question>().HasMany(q => q.Options).WithOne().HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Question>().HasOne<Question>().WithMany().HasForeignKey(q => q.ParentQuestionId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            b.Entity<SurveyResponse>().HasMany(r => r.Items).WithOne().HasForeignKey(i => i.SurveyResponseId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<SurveyResponse>().HasOne<Survey>().WithMany().HasForeignKey(r => r.SurveyId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}