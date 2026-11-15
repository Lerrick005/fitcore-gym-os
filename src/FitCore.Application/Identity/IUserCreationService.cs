using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Identity;

/// <summary>
/// Narrow user creation for wizard first-Owner setup (PRS §10 step 8, §11).
/// Full user management (add/disable/reset) arrives in Step 6.
/// </summary>
public interface IUserCreationService
{
    /// <summary>
    /// Creates the first Owner user. Fails with "identity.owner.exists" if an Owner user already exists.
    /// Auto-creates the Owner role if missing.
    /// </summary>
    Task<Result<User>> CreateFirstOwnerAsync(
        string username,
        string displayName,
        string password,
        CancellationToken ct = default);
}
