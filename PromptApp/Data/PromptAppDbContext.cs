using PromptApp.Model;
using Microsoft.EntityFrameworkCore;
using Prompt = PromptApp.Model.Prompt;

namespace PromptApp.Data
{
    public class PromptAppDbContext : DbContext
    {
        public PromptAppDbContext(DbContextOptions<PromptAppDbContext> options) : base(options) { }
        public DbSet<Prompt> Prompts { get; set; }
    }
}
