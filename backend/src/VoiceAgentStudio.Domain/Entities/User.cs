using VoiceAgentStudio.Domain.Common;
namespace VoiceAgentStudio.Domain.Entities;
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Operator"; // Admin, Supervisor, Operator
    public bool IsActive { get; set; } = true;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
