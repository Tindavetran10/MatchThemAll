# App Store & Google Play Publishing Checklist

Ensure maximum stability, performance, and legal compliance before submitting your game build.

---

## 1. Compliance & Asset Verification
- [ ] Ensure all 3D mesh placeholders (Primitive generated) are swapped out for final licensed geometry.
- [ ] Confirm `THIRD_PARTY_NOTICES.txt` reflects all assets and their permissive attribution (MIT, CC-BY, etc).
- [ ] Swap out any generic UI graphics / placeholder colored backgrounds with final PNG sprites.

## 2. Rendering & Optimization
- [ ] Assign the correct URP Asset for target Mobile/Desktop platform in `Project Settings > Graphics`.
- [ ] Adjust `Shadow Distances` and `Cascades` in the assigned `UniversalRenderPipelineAsset`.
- [ ] Clear trailing memory errors by disabling continuous console polling scripts/Gizmo drawers.

## 3. Storage & Initialization
- [ ] Validate `SaveManager/save.json` writes correctly on target device path (`Application.persistentDataPath`).
- [ ] Configure IL2CPP Stripping Level to "Low" or "Minimal" to avoid dropping reflection-based dependencies for generic JSON serializers unless explicitly tested.
- [ ] Build and verify on at least one lowest-tier target device memory footprint.

## 4. Integration
- [ ] Swap mocked IAP `ShopManager.cs` stubs with configured Unity IAP extensions or custom billing callbacks.
- [ ] Register Google Play Services or Apple GameCenter if integrating Leaderboards.
- [ ] Update `Player Settings` (bundle identifier, version code, icons, splash screen, and uncheck 'Development Build').
