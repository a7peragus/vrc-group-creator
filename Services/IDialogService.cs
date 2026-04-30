using System.Threading.Tasks;

namespace VrcGroupCreator.Services;

public interface IDialogService
{
    Task<bool> ConfirmDeleteGroupAsync(string groupName, string discriminator);
    Task<bool> ConfirmUnlockProtectionAsync(string groupName, string fullShortCode);
    Task<bool> ConfirmBulkDeleteAsync(int count);
}
