using System.ComponentModel.DataAnnotations;

namespace PromptApp.Model;
public class Prompt
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Content { get; set; } = default!;
    [Required]
    public string State { get; set; } = "Pending";
    public string? Result { get; set; }
}