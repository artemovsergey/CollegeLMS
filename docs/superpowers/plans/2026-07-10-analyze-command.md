# /analyze Command Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create `/analyze` custom opencode command that performs comprehensive project analysis across 9 dimensions.

**Architecture:** Single skill file at `.opencode/skills/analyze/SKILL.md` with YAML frontmatter + markdown procedure + todos. Command entry in `opencode.json` with minimal echo template. When user types `/analyze`, the agent loads the skill and executes the checklist.

**Tech Stack:** opencode skills system, opencode.json custom commands

## Global Constraints

- Skill file must follow existing skill format: YAML frontmatter (`name`, `description`) + markdown body
- Command template uses `!echo` syntax following existing pattern (`push`, `voice_google`)
- All analysis performed by agent reading files, running commands — no external scripts
- Output is Markdown to console with ✅/❌/⚠️ emoji status indicators
- Skill must include todos for each section via `todowrite`

---

### Task 1: Create analyze skill file

**Files:**
- Create: `.opencode/skills/analyze/SKILL.md`

**Interfaces:**
- Produces: Skill file loaded by `skill("analyze")` or triggered by `/analyze` command

- [ ] **Step 1: Create `.opencode/skills/analyze/` directory**

```bash
New-Item -ItemType Directory -Path ".opencode/skills/analyze" -Force
```

- [ ] **Step 2: Write SKILL.md with frontmatter**

```markdown
---
name: analyze
description: Run comprehensive project analysis — spec, user stories, plugins, MCP, skills, readiness, phases, agents, models
---

# analyze — Project Analyzer

Run full project health check and output a Markdown report with ✅/❌/⚠️ status indicators.
```

- [ ] **Step 3: Write Section 1 — Technical Specification check**

Design: Check `docs/spec/task.md` exists, read it, compare against actual project services. Read `docs/spec/userstories.md` to cross-reference.

```markdown
## 1. 📋 Technical Specification

- [ ] Read `docs/spec/task.md` — record line count and section count
- [ ] Check 5 core services documented: SiteService, AuthService, ScheduleService, LearningService, TestingService
- [ ] Check stack requirements: Postgres, .NET, Next.js, Docker, Nginx
- [ ] Check `CollegeLMS.API/Program.cs` for service registration alignment
```

- [ ] **Step 4: Write Section 2 — User Stories check**

Design: Read `docs/spec/userstories.md`, parse UC status markers, count P0/P1/P2/P3, compare with actual controllers.

```markdown
## 2. 📖 User Stories

- [ ] Read `docs/spec/userstories.md` — count total UC and status breakdown
- [ ] Get actual controllers: `Get-ChildItem CollegeLMS.API/Controllers/ -Name`
- [ ] Map UC priorities to implementation status: P0 implemented count vs total
```

- [ ] **Step 5: Write Section 3 — Plugins check**

```markdown
## 3. 🔌 Plugins

- [ ] Read `opencode.json` — extract plugin entries
- [ ] Verify superpowers plugin source URL is accessible
- [ ] Verify superpowers process skills accessible (`~/.cache/opencode/packages/superpowers/...`)
```

- [ ] **Step 6: Write Section 4 — MCP Servers check**

```markdown
## 4. 🔗 MCP Servers

- [ ] Read `opencode.json` MCP section
- [ ] Check Playwright: enabled, command valid, env vars
- [ ] Check GitHub: enabled, command valid, GITHUB_TOKEN set
```

- [ ] **Step 7: Write Section 5 — Skills check**

```markdown
## 5. 🛠️ Skills

- [ ] List `.opencode/skills/` — count skills, verify each has SKILL.md
- [ ] List `.agents/skills/` — count skills, verify each has SKILL.md
- [ ] Cross-check AGENTS.md skill references against actual files
```

- [ ] **Step 8: Write Section 6 — Development Readiness check**

```markdown
## 6. 🚦 Development Readiness

- [ ] Run `dotnet build CollegeLMS.API/CollegeLMS.csproj` — check exit code
- [ ] Run `dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj` — check exit code
- [ ] Run `npm run lint` in `frontend/` — check exit code
- [ ] Check git status: `git status --porcelain`
```

- [ ] **Step 9: Write Section 7 — Development Phases check**

```markdown
## 7. 📊 Development Phases

- [ ] Check git log for "phase" commits: `git log --oneline --grep="phase" -20`
- [ ] Check migrations: `Get-ChildItem CollegeLMS.API/Migrations/`
- [ ] Map phase status per feature
```

- [ ] **Step 10: Write Section 8 — Agents check**

```markdown
## 8. 🤖 Agents

- [ ] Read `opencode.json` — `agent.subagent` section
- [ ] List all agents with model and description
- [ ] Verify agent descriptions match actual roles
```

- [ ] **Step 11: Write Section 9 — Models check**

```markdown
## 9. 🧠 Models

- [ ] Extract model names from all agent configs
- [ ] Check consistency — all agents use same model
- [ ] Verify model availability
```

- [ ] **Step 12: Write Summary section**

```markdown
## 📈 Summary

| Section | Status |
|---------|--------|
| 📋 Technical Specification | ... |
| 📖 User Stories | ... |
| 🔌 Plugins | ... |
| 🔗 MCP Servers | ... |
| 🛠️ Skills | ... |
| 🚦 Development Readiness | ... |
| 📊 Development Phases | ... |
| 🤖 Agents | ... |
| 🧠 Models | ... |
```

- [ ] **Step 13: Add full skill header instructions**

The complete header should instruct the agent how to load and execute:

```markdown
# analyze — Project Analyzer

Load this skill when the user invokes `/analyze` or asks for project analysis.

## How to use

1. Create todos for all 9 sections using `todowrite`
2. For each section, execute the listed checks (read files, run commands)
3. Collect evidence and output Markdown with ✅/❌/⚠️
4. Print the report directly to console
```

- [ ] **Step 14: Commit**

```bash
git add .opencode/skills/analyze/SKILL.md
git commit -m "feature: /analyze skill — 9-dimension project analysis"
```

---

### Task 2: Add /analyze command to opencode.json

**Files:**
- Modify: `opencode.json` — add command entry

- [ ] **Step 1: Read current opencode.json**

```bash
Get-Content opencode.json
```

- [ ] **Step 2: Add analyze command entry**

Edit to add after `voice_google`:

```json
    "analyze": {
        "template": "!echo '🔍 Starting project analysis...'",
        "description": "Run full project analysis — spec, user stories, plugins, MCP, skills, readiness, phases, agents, models"
    }
```

- [ ] **Step 3: Validate JSON**

```bash
Get-Content opencode.json | ConvertFrom-Json
```

- [ ] **Step 4: Commit**

```bash
git add opencode.json
git commit -m "feature: /analyze custom command in opencode.json"
```

---

### Task 3: Verify

**Files:** No changes

- [ ] **Step 1: Verify skill loads**

```bash
Test-Path .opencode/skills/analyze/SKILL.md
```
Expected: True

- [ ] **Step 2: Verify opencode.json is valid JSON**

```bash
Get-Content opencode.json | ConvertFrom-Json
```
Expected: No errors, output shows all config

- [ ] **Step 3: Test /analyze flow**

The skill is now ready. When user types `/analyze`, the command template echoes the start message, then the agent loads this skill and performs the analysis.
