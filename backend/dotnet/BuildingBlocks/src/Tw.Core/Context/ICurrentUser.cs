using System.Security.Claims;

namespace Tw.Core.Context;

/// <summary>
/// Exposes claims-based identity information for the current execution context.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets whether the current execution context has an authenticated identity.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the stable user identifier when one is present in the current identity.
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// Gets the user name claim value when available.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the given name claim value when available.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the surname claim value when available.
    /// </summary>
    string? SurName { get; }

    /// <summary>
    /// Gets the email claim value when available.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets whether the current identity reports its email address as verified.
    /// </summary>
    bool EmailVerified { get; }

    /// <summary>
    /// Gets the phone number claim value when available.
    /// </summary>
    string? PhoneNumber { get; }

    /// <summary>
    /// Gets whether the current identity reports its phone number as verified.
    /// </summary>
    bool PhoneNumberVerified { get; }

    /// <summary>
    /// Gets role claim values as an identity-context convenience; this is not an authorization boundary.
    /// </summary>
    string[] Roles { get; }

    /// <summary>
    /// Finds the first claim matching the supplied claim type.
    /// </summary>
    /// <param name="claimType">The claim type to search for.</param>
    /// <returns>The first matching claim, or <see langword="null"/> when no matching claim exists.</returns>
    Claim? FindClaim(string claimType);

    /// <summary>
    /// Finds all claims matching the supplied claim type.
    /// </summary>
    /// <param name="claimType">The claim type to search for.</param>
    /// <returns>All matching claims, or an empty array when no matching claims exist.</returns>
    Claim[] FindClaims(string claimType);

    /// <summary>
    /// Gets every claim available from the current identity context.
    /// </summary>
    /// <returns>All claims visible to the current identity context.</returns>
    Claim[] GetAllClaims();

    /// <summary>
    /// Checks whether the current identity context includes the supplied role name as a convenience only.
    /// </summary>
    /// <param name="roleName">The role name to check in the current identity context.</param>
    /// <returns><see langword="true"/> when the role is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method is not an authorization boundary and must not replace policy or permission checks.</remarks>
    bool IsInRole(string roleName);
}
