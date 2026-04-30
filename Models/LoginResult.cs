using System.Collections.Generic;

namespace VrcGroupCreator.Models;

public class LoginResult
{
    public bool Success { get; set; }
    public bool Requires2FA { get; set; }
    public List<string>? TwoFactorTypes { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? AuthCookie { get; set; }
    public string? TwoFactorCookie { get; set; }
    // Held in memory only — never written to disk as plaintext
    public string? Username { get; set; }
    public string? Password { get; set; }
}
