#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>Writes a ready-to-fill <c>PowerupEffect</c> subclass into Scripts/Runtime/PowerUps/Custom.</summary>
    public static class PowerupMechanicTemplate
    {
        public const string Folder = "Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Custom";

        /// <summary>Creates the script and returns its path, or null if the name is invalid or the file already exists.</summary>
        public static string Create(string name)
        {
            string cls = Regex.Replace(name ?? "", "[^A-Za-z0-9]", "");
            if (cls.Length == 0 || char.IsDigit(cls[0])) return null;
            if (!cls.EndsWith("Effect")) cls += "Effect";
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{cls}.cs";
            if (File.Exists(path)) return null;

            File.WriteAllText(path, $@"using System;
using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{{
    /// <summary>Custom power-up mechanic. Pick it in Match Them All ▸ Power-up Manager ▸ Mechanic.</summary>
    [Serializable]
    public class {cls} : PowerupEffect
    {{
        [Tooltip(""Example setting — shown in the Power-up Manager under Mechanic."")]
        public int amount = 10;

        // Return false to refuse the tap (no charge is used). Example: only while the level timer exists.
        public override bool CanActivate(PowerupContext ctx) => ctx.Timer != null;

        public override void Activate(PowerupContext ctx)
        {{
            // What ctx gives you:
            //   ctx.Items      — every item on the board (List<Item>; pooled items may be inactive)
            //   ctx.Goals      — the level's goals (ItemLevelData[])
            //   ctx.ItemSpots  — the tray (ItemSpotManager), e.g. ctx.ItemSpots.GetRandomOccupiedSpot()
            //   ctx.Timer      — the level timer (TimerManager), e.g. ctx.Timer.AddTime(amount)
            //   ctx.Source     — the tapped slot's transform (animate it, spawn VFX at it)
            //   ctx.ActivateVfx / ctx.EndVfx — the VFX set on this power-up
            //   ctx.SetBusy(true/false) — block other power-ups while a long effect runs
            Debug.Log(""[{cls}] activated"");
        }}
    }}
}}
");
            AssetDatabase.ImportAsset(path);
            return path;
        }
    }
}
#endif
