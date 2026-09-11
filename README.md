# Fusion Anti Cheat

Server-side anti-cheat source for LabFusion / BONELAB.

## Avatar enforcement

The standalone source in `src/FusionGuard` enforces the avatar policy on the host through `PlayerRepAvatarMessage`. It blocks only the received avatar message; it does not kick or ban players.

The mod creates these entries in `UserData/MelonPreferences.cfg`:

```ini
# Block a player avatar change after the first accepted avatar message.
block_avatar_changes=False

# Only allow avatars listed in allowed_avatar_barcodes. An empty list blocks all remote avatars.
enforce_avatar_allowlist=False
allowed_avatar_barcodes=
```

To allow selected avatars, put their exact LabFusion barcode/ID values in the comma-separated list:

```ini
enforce_avatar_allowlist=True
allowed_avatar_barcodes=author.avatar.one,author.avatar.two
```

Enable these settings only after recording the exact avatar IDs used by your players. A rejected avatar message is blocked; the player is neither kicked nor banned.

## Security

No Discord webhook URL is included in this repository. Configure Discord only in your private local config if you need it, and never commit that config.
