---
timestamp: 2026-02-23T22:31:48.456Z
prompt:
<explicit_instructions type="opsx-new.md">
Start a new change using the experimental artifact-driven approach.

**Input**: The argument after `/opsx:new` is the change name (kebab-case), OR a description of what the user wants to build.

**Steps**

1. **If no input provided, ask what they want to build**

   Use the **AskUserQuestion tool** (open-ended, no preset options) to ask:
   > "What change do you want to work on? Describe what you want to build or fix."

   From their description, derive a kebab-case name (e.g., "add user authentication" → `add-user-auth`).

   **IMPORTANT**: Do NOT proceed without understanding what the user wants to build.

2. **Determine the workflow schema**

   Use the default schema (omit `--schema`) unless the user explicitly requests a different workflow.

   **Use a different schema only if the user mentions:**
   - A specific schema name → use `--schema <name>`
   - "show workflows" or "what workflows" → run `openspec schemas --json` and let them choose

   **Otherwise**: Omit `--schema` to use the default.

3. **Create the change directory**
   ```bash
   openspec new change "<name>"
   ```
   Add `--schema <name>` only if the user requested a specific workflow.
   This creates a scaffolded change at `openspec/changes/<name>/` with the selected schema.

4. **Show the artifact status**
   ```bash
   openspec status --change "<name>"
   ```
   This shows which artifacts need to be created and which are ready (dependencies satisfied).

5. **Get instructions for the first artifact**
   The first artifact depends on the schema. Check the status output to find the first artifact with status "ready".
   ```bash
   openspec instructions <first-artifact-id> --change "<name>"
   ```
   This outputs the template and context for creating the first artifact.

6. **STOP and wait for user direction**

**Output**

After completing the steps, summarize:
- Change name and location
- Schema/workflow being used and its artifact sequence
- Current status (0/N artifacts complete)
- The template for the first artifact
- Prompt: "Ready to create the first artifact? Run `/opsx:continue` or just describe what this change is about and I'll draft it."

**Guardrails**
- Do NOT create any artifacts yet - just show the instructions
- Do NOT advance beyond showing the first artifact template
- If the name is invalid (not kebab-case), ask for a valid name
- If a change with that name already exists, suggest using `/opsx:continue` instead
- Pass --schema if using a non-default workflow
</explicit_instructions>
<task> You are working on a browser-based task management utility. Create a implementation plan to do the next stuff. You previously built this application using pure JavaScript and HTML, and that implementation is located in the parent folder under assignment1. Your task now is to migrate this task management application to TypeScript and add new features. First, review all the code and files in the assignment1 folder to understand the existing functionality, then copy the relevant files into assignment2 for your new implementation. You must not modify or alter anything in assignment1. For this phase, you need to design and define TypeScript interfaces that represent the data structures and relationships for a typical task management application. Focus on creating comprehensive type definitions for entities such as tasks, projects, categories, tags, users, comments, attachments, priorities, and any other relevant domain objects. Define the relationships between these entities using appropriate TypeScript types such as interfaces, enums, and union types. Include properties for all necessary fields like IDs, names, descriptions, dates, status values, and nested objects. Do not write any implementation code yet, only the TypeScript interfaces and type definitions that would support the task management features. Follow the config.yaml and create the file ai-prompts.md and put this prompt in there. Follow opsx guidelines.
</task>
---
---
timestamp: 2026-02-23T22:38:15.921Z
prompt:
Create TypeScript interfaces (no implementation code) that model the full domain of a typical task management app. First review the previous JavaScript implementation in the parent folder (assignment1) to align with existing features. Define only interfaces/type aliases/enums for core entities and their relationships: Task (id, title, description, status, priority, dueDate, createdAt, updatedAt, completedAt, recurrence, dependencies, checklist/subtasks, tags/labels, comments, attachments, reminders), Project/Board/List grouping, User/Assignee, Tag/Label, Status, Priority, Comment, ChecklistItem/Subtask, Dependency (with type), Recurrence pattern, Reminder, Attachment, Audit fields. Model one-to-many and many-to-many relations (e.g., tasks ↔ tags, tasks ↔ users), and use appropriate enum/string literal types for constrained values. Focus solely on the type/interface definitions that capture data shape and relationships; do not include any runtime logic or implementation. keep in mind the ospx guidelines
---
timestamp: 2026-02-23T22:56:44.872Z
prompt:
create the proposals for the next ai as ospx wants you to. dont write ant code yet
---
timestamp: 2026-02-23T23:00:49.074Z
prompt:
do it
---
timestamp: 2026-02-23T23:02:27.229Z
prompt:
go to the next step
---
timestamp: 2026-02-23T23:11:55.955Z
prompt:
go to the next step
---
timestamp: 2026-02-23T23:12:49.098Z
prompt:
go to the next step

---
timestamp: 2026-02-23T22:41:52.482Z
prompt:
assignment2 is going to be a task management app. Implement te actual assignment requirements.  Requirements:
    - Full TypeScript conversion with strict mode
    - Custom type definitions for all entities
    - Generic utility functions (at least 3)
    - Add: recurring tasks, task dependencies, statistics
    search, sorting
    - Category +-< Task >-+ Priority relationships
