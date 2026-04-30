namespace VrcGroupCreator.Models;

public class VrcGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int MemberCount { get; set; }

    public string FullShortCode => string.IsNullOrEmpty(Discriminator) ? ShortCode : $"{ShortCode}.{Discriminator}";
}
