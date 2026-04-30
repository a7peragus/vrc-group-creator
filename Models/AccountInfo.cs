namespace VrcGroupCreator.Models;

public class AccountInfo
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AuthCookie { get; set; } = string.Empty;
    public string? TwoFactorCookie { get; set; }
    // DPAPI-encrypted, base64-encoded credentials for silent re-login after IP change / cookie expiry
    public string? EncryptedUsername { get; set; }
    public string? EncryptedPassword { get; set; }
}
