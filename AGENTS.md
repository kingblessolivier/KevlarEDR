# KevlarEDR project context

Read `PROJECT_MEMORY.md` before planning or implementing changes.

Start with one Windows computer. Preserve the future company-management roadmap
without implementing the entire enterprise platform in the first release.

Distinguish agreed scope, proposed designs, and verified implemented behavior.
Update project memory when the user makes or changes a substantive decision.


# Task Management & TODO Guidelines
- When viewing, querying, adding, updating, or deleting project tasks or TODOs, ALWAYS use the TODO MCP tools (`todo_get_tasks`, `todo_add_tasks`, `todo_update_tasks`, `todo_delete_tasks`, `todo_clear_category`, `todo_move_category`) on server `todo-mcp` or `todo-extension`.
- NEVER edit the `.todo` file directly with file modification tools.
