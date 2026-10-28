using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Dev-only (not exported): repoints template references from third-party assets to our replacements.
// swap-map.txt lines: "Assets/old/path.png => Assets/new/path.png" ("#" = comment).
// Sub-assets (font atlas/material, FBX meshes/materials) are remapped by type, then by name.
static class AssetGuidSwap
{
    const string MapPath = "Assets/_AssetGen/swap-map.txt";
    const string Root = "Assets/MatchThemAllTemplate";
    static readonly Regex Yaml = new(@"\.(prefab|unity|asset|mat|controller|anim|overrideController|spriteatlas|mixer)$");
    static readonly Regex Ref = new(@"fileID: (-?\d+), guid: ([0-9a-f]{32})");

    [MenuItem("Tools/AssetGen/Apply Swap Map")]
    static void Apply()
    {
        var guids = new Dictionary<string, string>();
        var subs = new Dictionary<string, string>(); // "oldGuid:oldId" -> "fileID: newId, guid: newGuid"
        foreach (var raw in File.ReadAllLines(MapPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var parts = line.Split("=>");
            string oldPath = parts[0].Trim(), newPath = parts[1].Trim();
            string oldGuid = AssetDatabase.AssetPathToGUID(oldPath), newGuid = AssetDatabase.AssetPathToGUID(newPath);
            if (oldGuid == "" || newGuid == "") { Debug.LogError($"[Swap] missing asset: {line}"); continue; }
            guids[oldGuid] = newGuid;
            MapSubAssets(oldPath, newPath, oldGuid, newGuid, subs);
            CopySpriteSettings(oldPath, newPath);
        }

        int changed = 0;
        foreach (var file in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
        {
            if (!Yaml.IsMatch(file)) continue;
            var text = File.ReadAllText(file);
            var swapped = Ref.Replace(text, m =>
            {
                string id = m.Groups[1].Value, guid = m.Groups[2].Value;
                if (subs.TryGetValue(guid + ":" + id, out var sub)) return sub;
                return guids.TryGetValue(guid, out var g) ? $"fileID: {id}, guid: {g}" : m.Value;
            });
            if (swapped == text) continue;
            File.WriteAllText(file, swapped);
            changed++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Swap] {guids.Count} mappings, {subs.Count} sub-assets, {changed} files rewritten.");
    }

    static void MapSubAssets(string oldPath, string newPath, string oldGuid, string newGuid, Dictionary<string, string> subs)
    {
        var news = AssetDatabase.LoadAllAssetsAtPath(newPath).Where(o => o).ToArray();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(oldPath))
        {
            if (!o || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out _, out long oldId)) continue;
            var sameType = news.Where(n => n.GetType() == o.GetType()).ToArray();
            var match = sameType.FirstOrDefault(n => n.name == o.name) ?? (sameType.Length == 1 ? sameType[0] : null);
            if (match && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(match, out _, out long newId))
                subs[oldGuid + ":" + oldId] = $"fileID: {newId}, guid: {newGuid}";
        }
    }

    // Keeps 9-slice borders and scale identical so layouts don't shift.
    static void CopySpriteSettings(string oldPath, string newPath)
    {
        if (AssetImporter.GetAtPath(oldPath) is not TextureImporter src || AssetImporter.GetAtPath(newPath) is not TextureImporter dst) return;
        var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(oldPath);
        var dstTex = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
        float scale = srcTex && dstTex ? (float)dstTex.width / srcTex.width : 1f;
        dst.textureType = TextureImporterType.Sprite;
        dst.spriteImportMode = SpriteImportMode.Single;
        dst.spriteBorder = src.spriteBorder * scale;
        dst.spritePixelsPerUnit = src.spritePixelsPerUnit * scale;
        dst.alphaIsTransparency = true;
        dst.SaveAndReimport();
    }
}
