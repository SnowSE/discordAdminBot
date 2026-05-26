namespace Web.Models;

public record RoleAssignment(GuildRole Role, List<GuildMember> Members);
