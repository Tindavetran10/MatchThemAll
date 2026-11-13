// Draws numbered highlight boxes on the raw editor captures with ffmpeg.
//   node docs/user-guide-shots/annotate.mjs            → all shots in spec.json
//   node docs/user-guide-shots/annotate.mjs te-overview → one shot
// spec.json: { "<out name>": { "src": "<raw name>", "crop": [x,y,w,h]?, "boxes": [[x,y,w,h,label?], ...] } }
// Box coordinates are in the cropped image's pixels. Labels default to 1, 2, 3…
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const raw = path.join(here, "raw");
const out = path.join(here, "..", "..", "Assets", "MatchThemAllTemplate", "Documentation", "UserGuide", "Images", "tools");
fs.mkdirSync(out, { recursive: true });

const spec = JSON.parse(fs.readFileSync(path.join(here, "spec.json"), "utf8"));
const font = "C\\:/Windows/Fonts/arialbd.ttf";
const only = process.argv[2];

for (const [name, s] of Object.entries(spec)) {
  if (only && name !== only) continue;
  const f = [];
  if (s.crop) f.push(`crop=${s.crop[2]}:${s.crop[3]}:${s.crop[0]}:${s.crop[1]}`);
  // side "left": rows start at the image edge, so pad a margin and put the badges there, beside each row,
  // instead of on top of the row's label.
  const pad = s.side === "left" ? 36 : 0;
  if (pad) f.push(`pad=iw+${pad}:ih:${pad}:0:color=0x1E1E1E`);
  (s.boxes || []).forEach((b, i) => {
    const [x0, y, w, h] = b, x = x0 + pad, label = b[4] ?? String(i + 1);
    f.push(`drawbox=x=${x}:y=${y}:w=${w}:h=${h}:color=0xFF8A00@1:t=3`);
    // number badge: beside the row (side "left") or on the box's top-left corner (kept inside the image)
    const bw = 14 + 8 * label.length;
    const bx = pad ? Math.max(0, x - bw - 4) : Math.max(0, x - 6);
    const by = pad ? Math.max(0, y + Math.min(0, Math.round((h - 22) / 2))) : Math.max(0, y - 12);
    f.push(`drawbox=x=${bx}:y=${by}:w=${bw}:h=22:color=0xFF8A00@1:t=fill`);
    f.push(`drawtext=fontfile='${font}':text='${label}':x=${bx + 7}:y=${by + 4}:fontsize=15:fontcolor=white`);
  });
  const args = ["-loglevel", "error", "-y", "-i", path.join(raw, `${s.src ?? name}.png`)];
  if (f.length) args.push("-vf", f.join(","));
  args.push(path.join(out, `${name}.png`));
  execFileSync("ffmpeg", args, { stdio: "inherit" });
  console.log("ok", name);
}
