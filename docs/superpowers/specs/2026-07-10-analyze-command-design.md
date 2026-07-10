# /analyze Command — Design Spec

## Purpose
A custom opencode command `/analyze` that performs comprehensive project analysis across 9 dimensions, producing a Markdown report with status indicators (✅/❌/⚠️).

## Scope
- One skill file: `.opencode/skills/analyze/SKILL.md`
- One command entry in `opencode.json`
- No external scripts — all analysis performed by the agent loading the skill

## Non-goals
- Automated fixing of detected issues (report only)
- CI integration (runs locally)
- Persistent report history

## Architecture

```
User: /analyze
  │
  └─> opencode.json command template echoes trigger message
       │
       └─> Agent loads .opencode/skills/analyze/SKILL.md
            │
            ├─> Creates todos for 9 checklist sections
            ├─> Iterates through each section:
            │     read files → run commands → collect evidence
            └─> Outputs Markdown report with ✅/❌/⚠️
```

## 9 Analysis Dimensions

### 1. Technical Specification (task.md)
- **Check**: File exists at `docs/spec/task.md`
- **Check**: Covers all 5 core services (Site, Auth, Schedule, Learning, Testing)
- **Check**: Stack requirements documented (Postgres, .NET, Next.js, Docker)
- **Check**: No contradictions with actual code

### 2. User Stories
- **Check**: File exists at `docs/spec/userstories.md`
- **Check**: All 69 UC documented
- **Check**: P0 stories implemented count vs total
- **Check**: P1/P2/P3 gap analysis

### 3. Plugins
- **Check**: `opencode.json` has plugin entry for superpowers
- **Check**: Plugin source URL accessible
- **Check**: Superpowers process skills available (brainstorming, systematic-debugging, etc.)

### 4. MCP Servers
- **Check**: Playwright server enabled, command valid, env vars ok
- **Check**: GitHub server enabled, GITHUB_TOKEN set
- **Check**: No duplicate or conflicting MCP configs

### 5. Skills
- **Check**: `.opencode/skills/` — all 27 skills have SKILL.md
- **Check**: `.agents/skills/` — all 9 skills have SKILL.md
- **Check**: Superpowers skills accessible from plugin cache
- **Check**: No broken references (missing files, wrong paths)
- **Check**: Skills referenced in AGENTS.md match actual files

### 6. Development Readiness
- **Check**: `dotnet build` exit code 0 (API project)
- **Check**: `dotnet test` exit code 0 (Tests project)
- **Check**: `npm run build` or lint (Frontend)
- **Check**: Docker compose config valid
- **Check**: Git status clean (no uncommitted changes)

### 7. Development Phases
- **Check**: Phase gates G1-G5 status
- **Check**: Which phases complete for each implemented feature
- **Check**: Migration vs code alignment

### 8. Agents
- **Check**: All 5 agents defined in `opencode.json` (`agent.subagent`)
- **Check**: Each has model, description
- **Check**: Agent descriptions match actual responsibilities
- **Check**: No orphaned agent configs

### 9. Models
- **Check**: Model specified in all agent configs
- **Check**: Consistent across all agents
- **Check**: Model available/valid

## Output Format

Report printed directly to console as Markdown:

```markdown
# 🔍 Project Analysis Report — 2026-07-10

## 1. 📋 Technical Specification
✅ task.md exists (178 lines)
✅ Covers all 5 core services
⚠️ TestingService section incomplete

## 2. 📖 User Stories
✅ userstories.md exists (1266 lines, 69 UC)
✅ P0: 31/31 implemented
⚠️ P1: TestingService (0/5), JournalService (0/4)
...
```

## Report Sections
1. 📋 Technical Specification
2. 📖 User Stories
3. 🔌 Plugins
4. 🔗 MCP Servers
5. 🛠️ Skills
6. 🚦 Development Readiness
7. 📊 Development Phases
8. 🤖 Agents
9. 🧠 Models
10. 📈 Summary (overall health score)

## Implementation

### A. Skill file: `.opencode/skills/analyze/SKILL.md`
- Title: Project Analyzer
- Instructions with per-section checklist
- Each section: what to check, which commands to run, what constitutes pass/fail

### B. Command: `opencode.json`
```json
"analyze": {
    "template": "!echo '🔍 Starting project analysis...'",
    "description": "Run full project analysis — spec, user stories, plugins, MCP, skills, readiness, phases, agents, models"
}
```

## Edge Cases
- **No build tools installed**: Report as ❌ with instructions
- **Docker not running**: Skip Docker checks, mark as ⚠️
- **Empty sections**: Report as ❌ "Not implemented"
- **File not found**: Report as ❌ with path
- **Command errors**: Catch stderr, report as ❌
