# Fusion Anti-Cheat

Server-side anti-cheat source for LabFusion / BONELAB.

## Avatar enforcement

The avatar policy is enforced by the host through `PlayerRepAvatarMessage`. It ignores the host, approved operators, and anti-cheat whitelist entries, matching the existing policy rules.

Add these entries to `UserData/FusionGuard.cfg`:

```ini
# Immediately kick a player if their avatar barcode changes after the first avatar message.
kick_on_avatar_change=False

# Only allow avatars listed in allowed_avatar_barcodes. Empty list rejects every non-exempt player.
enforce_avatar_allowlist=False
allowed_avatar_barcodes=
```

To allow selected avatars, put their exact LabFusion barcode/ID values in the comma-separated list:

```ini
enforce_avatar_allowlist=True
allowed_avatar_barcodes=author.avatar.one,author.avatar.two
```

Enable these settings only after recording the exact avatar IDs used by your players. A rejected avatar message is blocked and the sender is kicked; it is not automatically banned.

## Security

No Discord webhook URL is included in this repository. Configure Discord only in your private local config if you need it, and never commit that config.
