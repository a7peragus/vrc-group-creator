using System.Collections.Generic;

namespace VrcGroupCreator.Models;

public class AppSettings
{
    public string NamePrefix { get; set; } = string.Empty;
    public string ShortCodePrefix { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int GroupCount { get; set; } = 1;
    public bool EnableDebugConsole { get; set; } = false;
    public bool EnableFileLogging { get; set; } = true;
    public ColorSettings Colors { get; set; } = new();
    public HashSet<string> ProtectedGroupIds { get; set; } = new();
}
