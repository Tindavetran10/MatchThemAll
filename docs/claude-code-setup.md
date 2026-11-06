# Claude Code setup (MatchThemAll) — reuse for a new project

How Claude Code is wired into this Unity project, layer by layer, and a checklist for a new project.
Snapshot taken 2026-11-04.

Most of the setup lives in your user folder (`C:\Users\gadet\.claude\`), so **it already applies to every new
project**. Only the project layer (section 2) and memory (section 5) have to be redone per project.

| Layer | Where | Per project? |
|---|---|---|
| Global settings, plugins, hooks, skills | `~/.claude/settings.json`, `~/.claude/hooks/`, `~/.claude/skills/` | No, already done |
| MCP servers (Unity, Blender) | `~/.claude.json`, `%APPDATA%\Claude\claude_desktop_config.json` | Server: no. Unity package: yes |
| Project instructions | `<project>/CLAUDE.md`, `<project>/.claude/settings.local.json` | Yes |
| Knowledge graph | `<project>/graphify-out/` | Yes (one command) |
| Memory | `~/.claude/projects/<project-path>/memory/` | Starts empty, Claude fills it |
| Decision log / Obsidian vault | `<project>/docs/`, `<project>/.obsidian/` | Yes |

---

## 1. Global layer (`~/.claude/`)

### 1.1 `settings.json`
What it holds:

- **`env`**: `ANTHROPIC_BASE_URL` (a local proxy at `http://localhost:20128/v1`), `ANTHROPIC_API_KEY`,
  `ANTHROPIC_CUSTOM_MODEL_OPTION` / `_NAME` ("tingade-combo"), `CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC=1`.
  The key is a secret. Never copy this file into a repository.
- **`model`**: `tingade-combo`. **`effortLevel`**: `high`. **`theme`**: `dark`.
- **`hooks`**: the graphify reminder (1.3).
- **`enabledPlugins`** and **`extraKnownMarketplaces`**: see 1.2.

### 1.2 Plugins
All installed at user scope, so every project gets them.

| Plugin | Marketplace (source) | What it gives you |
|---|---|---|
| `superpowers` | `claude-plugins-official` | Process skills: brainstorming → writing-plans → executing-plans, systematic-debugging, TDD, verification-before-completion, worktrees, code review. Saves specs and plans to `docs/superpowers/specs` and `docs/superpowers/plans`. |
| `code-review` | `claude-plugins-official` | `/code-review` on a diff or PR |
| `claude-code-setup` | `claude-plugins-official` | `claude-automation-recommender`: suggests hooks, skills and MCP servers for a codebase |
| `unity` | `unity-agent-plugin` (GitHub `Unity-Technologies/unity-agent-plugin`) | Unity's official skills: `unity:ui-ugui`, `unity:urp-postprocessing`, `unity:new-unity-project`, `unity:optimize-*`, IAP, LevelPlay ads, localization… |
| `ponytail` | `ponytail` (GitHub `DietrichGebert/ponytail`) | "Lazy senior dev" mode (minimal code, YAGNI), plus `/ponytail-review`, `/ponytail-audit` and `/ponytail-debt` |
| `andrej-karpathy-skills` | `karpathy-skills` (GitHub `forrestchang/andrej-karpathy-skills`) | `karpathy-guidelines`: think first, keep it simple, make surgical changes, define goals you can verify. Sections 1–4 of CLAUDE.md are adapted from it. |
| `obsidian` | `obsidian-skills` (git `https://github.com/kepano/obsidian-skills.git`) | Obsidian markdown, canvas, bases, CLI, defuddle |

To reinstall on a new machine, run these in a terminal, from an interactive `claude` session or the shell:

```bash
claude plugin marketplace add Unity-Technologies/unity-agent-plugin
```
```bash
claude plugin marketplace add DietrichGebert/ponytail
```
```bash
claude plugin marketplace add forrestchang/andrej-karpathy-skills
```
```bash
claude plugin marketplace add https://github.com/kepano/obsidian-skills.git
```
```bash
claude plugin install superpowers@claude-plugins-official
```
```bash
claude plugin install code-review@claude-plugins-official
```
```bash
claude plugin install claude-code-setup@claude-plugins-official
```
```bash
claude plugin install unity@unity-agent-plugin
```
```bash
claude plugin install ponytail@ponytail
```
```bash
claude plugin install andrej-karpathy-skills@karpathy-skills
```
```bash
claude plugin install obsidian@obsidian-skills
```

Optional: ponytail prints a reminder to add its statusline badge to `settings.json`:
`"statusLine": { "type": "command", "command": "powershell -ExecutionPolicy Bypass -File \"C:\\Users\\gadet\\.claude\\plugins\\cache\\ponytail\\ponytail\\4.7.0\\hooks\\ponytail-statusline.ps1\"" }`

### 1.3 Hook: `~/.claude/hooks/graphify-reminder.js`
This is a `PreToolUse` hook on `Bash|Read|Glob`. If the project has `graphify-out/graph.json` and Claude is about to
grep or read a source file, the hook adds a "run `graphify query` first" note to the tool call. It never blocks the
call, and in a project without a graph it does nothing. Registered in `settings.json`:

```json
"hooks": { "PreToolUse": [ { "matcher": "Bash|Read|Glob",
  "hooks": [ { "type": "command", "command": "node \"$HOME/.claude/hooks/graphify-reminder.js\"" } ] } ] }
```
It needs Node.js on PATH.

### 1.4 Global skill: graphify
- The CLI is at `C:\Users\gadet\.local\bin\graphify` (v0.9.62). `graphify install` puts the skill into
  `~/.claude/skills/graphify/`. If you also use Antigravity/Gemini, run `graphify install --platform antigravity`,
  because the Gemini copy there is still 0.8.44.
- `~/.claude/CLAUDE.md` (global instructions) only says: when the user types `/graphify`, invoke the graphify skill.

---

## 2. Project layer

### 2.1 `CLAUDE.md` (project root, committed)
This file does most of the work. Keep this outline in a new project and rewrite the project-specific parts:

1. **Think before coding** · 2. **Simplicity first** · 3. **Surgical changes** · 4. **Goal-driven execution**.
   These four are generic (from karpathy-guidelines) and can be copied as they are. Keep the rule "always check the Unity console before
   saying done, and add new pitfalls to this file".
5. **graphify rules**: query before you grep, and run `graphify update .` after code changes.
6. **God nodes**: the most-connected classes, copied from `graphify-out/GRAPH_REPORT.md`. Regenerate per project.
7. **Architecture rules**: EventBus, namespaces, save system, ZLinq, plus the **Known Pitfalls** list. The pitfalls
   are lessons from real mistakes, and Claude adds to them each time something goes wrong. In a new project, carry
   over the generic Unity/MCP ones (MCP can't delete assets, `Image` alias, no `??` on Unity objects, prefab vs
   instance references, repeated objects as prefabs, overlay UI vs 3D, verify the real state path, Flush the save).
8. **Community map** and 9. **Bridge nodes**: from the graph report.
10. **Obsidian**: the vault is the project root, and decisions go to `docs/decisions/`.
11. **Unity tooling**: the MCP tool table ("use MCP tools before writing custom code").
12. **Quick commands**: the graphify CLI.

### 2.2 `.claude/settings.local.json` (git-ignored)
This holds per-project permissions, so these tools never ask:
```json
{ "permissions": { "allow": [
  "WebSearch",
  "Read(//e/Unity Projects/MatchThemAll - Copy/**)",
  "mcp__unity-mcp__Unity_ReadConsole",
  "mcp__unity-mcp__Unity_ManageEditor",
  "mcp__unity-mcp__Unity_RunCommand",
  "Bash(claude plugin *)" ] } }
```
Change the `Read(...)` path for the new project. You can also run the `/fewer-permission-prompts` skill after a few
sessions; it builds this list from your history.

### 2.3 `.gitignore`
`/.claude` is ignored ("contains API keys and settings"). Keep `CLAUDE.md`, `docs/` and `graphify-out/` committed.

### 2.4 Other agents' rule files (only if you use those tools)
`.agents/rules/` (Antigravity) holds copies of the graphify and ponytail rules. `.kilo/` and `.qodo/` are other tools'
folders. Claude Code reads none of them.

---

## 3. MCP servers

| Server | Configured in | How it runs | Unity side |
|---|---|---|---|
| **`unity-mcp`** (official Unity, the main one used) | `~/.claude.json` → `mcpServers` (user scope) | `C:\Users\gadet\.unity\relay\relay_win.exe --mcp` | Package `com.unity.ai.assistant` (2.19.0-pre.2). The relay is installed by the AI Assistant package. |
| **`unityMCP`** (Coplay "MCP for Unity") | `%APPDATA%\Claude\claude_desktop_config.json` (desktop app) | `uvx --from mcpforunityserver==10.3.0 mcp-for-unity --transport stdio` (uvx from `AppData\Local\hermes\bin`) | Embedded package `Packages/Coplay` |
| **Blender** | Desktop app connector | Blender add-on (Blender must be open) | — |

New project:
- **Unity AI Assistant / official MCP:** add `com.unity.ai.assistant` in Package Manager and open the project in Unity. The
  `unity-mcp` entry is user-scope, so it works in any project as soon as Unity is running with the package.
- **Coplay MCP for Unity (optional):** copy `Packages/Coplay` into the new project, or add it through Package
  Manager. The desktop config already exists.
- The two Unity servers overlap. This project uses `unity-mcp` (CLAUDE.md section 11 tells Claude to). Install
  Coplay only if you need one of its tools (`run_tests`, `manage_build`, `unity_reflect`…).

---

## 4. graphify (project knowledge graph)

```bash
graphify .
```
Run it once in the new project root (or `/graphify` in Claude), then `graphify update .` after code changes
(AST only, no API cost). It writes `graphify-out/` (`graph.json`, `GRAPH_REPORT.md`, `graph.html`), which feeds
CLAUDE.md sections 6, 8 and 9 and turns on the reminder hook.

---

## 5. Memory

- Location: `~/.claude/projects/<project path with separators as dashes>/memory/`, e.g.
  `E--Unity-Projects-MatchThemAll---Copy`. A new project starts with an empty folder automatically.
- `MEMORY.md` is the index, one line per memory, and is loaded every session. Each memory is its own file with
  frontmatter (`name`, `description`, `type: user|feedback|project|reference`) and **Why / How to apply** lines.
- What goes there: facts the code doesn't show. This project has two: Addressables level loading (and its rebuild
  rule), and the Asset Store licensing rule (no third-party packs, `_AssetGen` stays dev-only).
- What doesn't: code structure, past fixes and git history (they're in the repo), and pitfalls, which go in
  CLAUDE.md so they're committed and shared.
- Tidy it with the `anthropic-skills:consolidate-memory` skill.
- **Split used here:** CLAUDE.md is for rules and pitfalls (committed). Memory is for your personal or business
  context (not in the repo). `docs/decisions/` records why something was built a certain way (committed, readable
  in Obsidian).

---

## 6. Docs and Obsidian

- `.obsidian/` sits at the project root, so the whole repo is a vault. `graphify-out/GRAPH_REPORT.md` links
  communities with `[[wikilinks]]`.
- `docs/decisions/YYYY-MM-DD-topic.md` is one short record per architectural decision (Context, Decisions,
  Tuning knobs).
- `docs/superpowers/specs` and `docs/superpowers/plans` hold the output of the brainstorming and writing-plans skills.

---

## 7. Workflow habits that paid off

- **Plan before building:** superpowers:brainstorming → plan mode → approve → implement in checkpoints.
- **Verify in the editor:**
  - Check the console through MCP after every change.
  - Test the real gameplay path in Play mode (set up state with real gameplay or save calls, never force an object
    active).
  - Measure numbers instead of eyeballing, and render captures through `Camera.main` into a RenderTexture.
- **Every mistake goes into Known Pitfalls** so it doesn't happen again.
- **Commits:** only when you ask, on a feature branch, with a `Co-Authored-By` line, and leaving out churn files
  (the TMP font atlas, the PrimeTween demo metas, `.idea`).

---

## 8. New-project checklist

1. Install Unity and open the new project. Add `com.unity.ai.assistant` (and, optionally, copy `Packages/Coplay`).
2. Install Node.js and the graphify CLI. On the same machine, plugins, hooks and MCP servers are already set up.
   On a new machine, copy `~/.claude/settings.json` (re-enter your own key), `~/.claude/hooks/`, the
   `unity-mcp` entry in `~/.claude.json` and the desktop config, then run the commands in 1.2.
3. `git init`, and add `/.claude` to `.gitignore`.
4. Copy this project's `CLAUDE.md`. Keep sections 1–5 and the generic pitfalls; empty out the project-specific
   parts (6–9, architecture details).
5. Run `graphify .`, then fill sections 6, 8 and 9 from `GRAPH_REPORT.md`.
6. Create `.claude/settings.local.json` with the permission list from 2.2 (update the path).
7. Create `docs/decisions/`. Open the folder as an Obsidian vault if you want the graph view.
8. Start Claude and tell it the business context that applies (e.g. "this is sold on the Asset Store") so it
   saves a memory.
9. Optional: `claude-code-setup:claude-automation-recommender` for project-specific hook and skill ideas.
