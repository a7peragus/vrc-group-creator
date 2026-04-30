using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using VrcGroupCreator.Models;

namespace VrcGroupCreator.Services;

public class AccountService
{
    private readonly string _accountsPath;
    private List<AccountInfo> _accounts = new();

    public IReadOnlyList<AccountInfo> Accounts => _accounts;

    public AccountService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "VrcGroupCreator");
        Directory.CreateDirectory(appFolder);
        _accountsPath = Path.Combine(appFolder, "accounts.json");
        Load();
    }

    public void SaveAccount(AccountInfo account)
    {
        var existing = _accounts.FirstOrDefault(a => a.UserId == account.UserId);
        if (existing != null)
        {
            _accounts.Remove(existing);
        }
        _accounts.Add(account);
        Save();
    }

    public void RemoveAccount(string userId)
    {
        var existing = _accounts.FirstOrDefault(a => a.UserId == userId);
        if (existing != null)
        {
            _accounts.Remove(existing);
            Save();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_accounts, Formatting.Indented);
            File.WriteAllText(_accountsPath, json);
        }
        catch { }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_accountsPath))
            {
                var json = File.ReadAllText(_accountsPath);
                _accounts = JsonConvert.DeserializeObject<List<AccountInfo>>(json) ?? new List<AccountInfo>();
            }
        }
        catch { }
    }

    /// <summary>Encrypt a plaintext string with Windows DPAPI (current-user scope only).</summary>
    public static string Encrypt(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>Decrypt a DPAPI base64 blob. Returns null if the blob is invalid or belongs to another user.</summary>
    public static string? Decrypt(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return null;
        try
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch { return null; }
    }
}
