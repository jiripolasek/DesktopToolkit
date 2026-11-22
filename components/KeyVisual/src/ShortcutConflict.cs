// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Identifies an existing shortcut assignment and optionally offers to replace it.</summary>
public sealed class ShortcutConflict
{
    /// <summary>Gets the stable identifier of the assignment.</summary>
    public string OwnerId { get; }

    /// <summary>Gets the name displayed to the user.</summary>
    public string OwnerName { get; }

    /// <summary>Gets the synchronous replacement callback, if one was provided.</summary>
    public Func<bool>? TryReplace { get; }

    /// <summary>Gets an asynchronous callback that atomically transfers the shortcut and returns whether it succeeded.</summary>
    /// <remarks>When set, this callback takes precedence over <see cref="TryReplace"/>.</remarks>
    public Func<Task<bool>>? TryReplaceAsync { get; init; }

    internal bool CanReplace => this.TryReplaceAsync is not null || this.TryReplace is not null;

    /// <summary>Creates a conflict for an assignment owned by the caller.</summary>
    /// <param name="ownerId">A stable, unique identifier for the existing assignment.</param>
    /// <param name="ownerName">The name displayed to the user.</param>
    /// <param name="tryReplace">An optional callback that atomically transfers the shortcut and returns whether it succeeded.</param>
    public ShortcutConflict(string ownerId, string ownerName, Func<bool>? tryReplace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        this.OwnerId = ownerId;
        this.OwnerName = ownerName;
        this.TryReplace = tryReplace;
    }
}
