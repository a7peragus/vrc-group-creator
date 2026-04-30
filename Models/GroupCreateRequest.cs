namespace VrcGroupCreator.Models;

public class GroupCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string RoleTemplate { get; set; } = "default";
    public string Description { get; set; } = string.Empty;
    public string JoinState { get; set; } = "open";
    public string Privacy { get; set; } = "default";
}
