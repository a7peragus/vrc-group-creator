namespace VrcGroupCreator.Models;

public class GroupTemplate
{
    public string NamePrefix { get; set; } = string.Empty;
    public string ShortCodePrefix { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Privacy { get; set; } = "default"; // default, private
    public string JoinState { get; set; } = "open"; // closed, invite, open, request
    public string RoleTemplate { get; set; } = "default"; // default, managedFree, managedInvite, managedRequest
}
