using System;
using System.Collections.Generic;
using HarmonyLib;
using LabFusion.Network;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionGuard.FusionGuardMod), "Fusion Anti Cheat", "0.1.0", "FusionGuard")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionGuard
{
    public sealed class FusionGuardMod : MelonMod
    {
        internal static MelonPreferences_Entry<bool> EnforceAvatarAllowlist;
        internal static MelonPreferences_Entry<bool> BlockAvatarChanges;
        internal static MelonPreferences_Entry<string> AllowedAvatarBarcodes;
        private bool menuOpen;

        public override void OnInitializeMelon()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("FusionGuard");
            EnforceAvatarAllowlist = category.CreateEntry("enforce_avatar_allowlist", false);
            BlockAvatarChanges = category.CreateEntry("block_avatar_changes", false);
            AllowedAvatarBarcodes = category.CreateEntry("allowed_avatar_barcodes", string.Empty);
            HarmonyInstance.PatchAll(typeof(FusionGuardMod).Assembly);
            LoggerInstance.Msg("Fusion Anti Cheat is active. Avatar policy is host-side only.");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                menuOpen = !menuOpen;
            }
        }

        public override void OnGUI()
        {
            if (!menuOpen)
            {
                return;
            }

            GUI.Box(new Rect(24f, 24f, 420f, 210f), "Fusion Anti Cheat");
            GUI.Label(new Rect(40f, 64f, 380f, 24f), "Host avatar protection");
            bool allowlist = GUI.Toggle(new Rect(40f, 98f, 380f, 24f), EnforceAvatarAllowlist.Value, "Enforce avatar allowlist");
            if (allowlist != EnforceAvatarAllowlist.Value)
            {
                EnforceAvatarAllowlist.Value = allowlist;
                MelonPreferences.Save();
            }

            bool blockChanges = GUI.Toggle(new Rect(40f, 128f, 380f, 24f), BlockAvatarChanges.Value, "Block avatar changes");
            if (blockChanges != BlockAvatarChanges.Value)
            {
                BlockAvatarChanges.Value = blockChanges;
                MelonPreferences.Save();
            }

            GUI.Label(new Rect(40f, 158f, 380f, 24f), "Allowlist: " + (string.IsNullOrWhiteSpace(AllowedAvatarBarcodes.Value) ? "empty" : "configured"));
            GUI.Label(new Rect(40f, 188f, 380f, 24f), "Press F8 to close");
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
