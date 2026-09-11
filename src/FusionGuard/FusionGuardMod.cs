using System;
using System.Collections.Generic;
using HarmonyLib;
using LabFusion.Network;
using MelonLoader;

[assembly: MelonInfo(typeof(FusionGuard.FusionGuardMod), "FusionGuard", "0.1.0", "FusionGuard")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionGuard
{
    public sealed class FusionGuardMod : MelonMod
    {
        internal static MelonPreferences_Entry<bool> EnforceAvatarAllowlist;
        internal static MelonPreferences_Entry<bool> BlockAvatarChanges;
        internal static MelonPreferences_Entry<string> AllowedAvatarBarcodes;

        public override void OnInitializeMelon()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("FusionGuard");
            EnforceAvatarAllowlist = category.CreateEntry("enforce_avatar_allowlist", false);
            BlockAvatarChanges = category.CreateEntry("block_avatar_changes", false);
            AllowedAvatarBarcodes = category.CreateEntry("allowed_avatar_barcodes", string.Empty);
            HarmonyInstance.PatchAll(typeof(FusionGuardMod).Assembly);
            LoggerInstance.Msg("Avatar policy is active. Blocking is host-side only.");
        }
    }

    [HarmonyPatch(typeof(PlayerRepAvatarMessage), "OnHandleMessage")]
    internal static class AvatarMessagePatch
    {
        private static readonly Dictionary<byte, string> InitialAvatarByPlayer = new Dictionary<byte, string>();

        private static bool Prefix(ReceivedMessage received)
        {
            if (!NetworkInfo.IsHost || !received.Sender.HasValue)
            {
                return true;
            }

            PlayerRepAvatarData data;
            try
            {
                data = received.ReadData<PlayerRepAvatarData>();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("[FusionGuard] Blocked unreadable avatar message: " + exception.GetType().Name);
                return false;
            }

            string barcode = data.Barcode;
            if (string.IsNullOrWhiteSpace(barcode))
            {
                MelonLogger.Warning("[FusionGuard] Blocked empty avatar barcode from player " + received.Sender.Value + ".");
                return false;
            }

            if (FusionGuardMod.EnforceAvatarAllowlist.Value && !IsAllowed(barcode))
            {
                MelonLogger.Warning("[FusionGuard] Blocked avatar not on allowlist from player " + received.Sender.Value + ": " + barcode);
                return false;
            }

            if (!FusionGuardMod.BlockAvatarChanges.Value)
            {
                return true;
            }

            byte sender = received.Sender.Value;
            string initialAvatar;
            if (!InitialAvatarByPlayer.TryGetValue(sender, out initialAvatar))
            {
                InitialAvatarByPlayer.Add(sender, barcode);
                return true;
            }

            if (string.Equals(initialAvatar, barcode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            MelonLogger.Warning("[FusionGuard] Blocked avatar change from player " + sender + ": " + barcode);
            return false;
        }

        private static bool IsAllowed(string barcode)
        {
            string[] allowedBarcodes = FusionGuardMod.AllowedAvatarBarcodes.Value.Split(',');
            foreach (string allowedBarcode in allowedBarcodes)
            {
                if (string.Equals(allowedBarcode.Trim(), barcode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
