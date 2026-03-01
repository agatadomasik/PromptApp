using PromptApp.Data;
using PromptApp.Model;

public class PromptService
{
    private readonly PromptAppDbContext _dbContext;

    public PromptService(PromptAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Prompt> AddAsync(Prompt prompt)
    {
        _dbContext.Prompts.Add(prompt);
        await _dbContext.SaveChangesAsync();
        return prompt;
    }

    public async Task<Prompt?> GetByIdAsync(Guid id)
        => await _dbContext.Prompts.FindAsync(id);
}