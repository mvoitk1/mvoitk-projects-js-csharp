/**
 * Output Formatting Module for Task Management Application
 * Provides different output formats: table, list, JSON
 */

const Formatters = (function() {
  'use strict';

  // Color codes for CLI-style output (HTML spans)
  const COLORS = {
    reset: '</span>',
    bold: '<span style="font-weight: bold;">',
    dim: '<span style="opacity: 0.7;">',
    
    // Status colors
    statusPending: '<span style="color: #f59e0b;">',
    statusInProgress: '<span style="color: #3b82f6;">',
    statusCompleted: '<span style="color: #10b981;">',
    statusCancelled: '<span style="color: #6b7280;">',
    
    // Priority colors
    priorityLow: '<span style="color: #9ca3af;">',
    priorityMedium: '<span style="color: #3b82f6;">',
    priorityHigh: '<span style="color: #f59e0b;">',
    priorityUrgent: '<span style="color: #ef4444;">',
    
    // Other colors
    error: '<span style="color: #ef4444;">',
    success: '<span style="color: #10b981;">',
    info: '<span style="color: #3b82f6;">',
    warning: '<span style="color: #f59e0b;">',
    id: '<span style="color: #8b5cf6;">',
    tag: '<span style="color: #06b6d4;">',
    date: '<span style="color: #6b7280;">'
  };

  /**
   * Format status with color
   * @param {string} status - Task status
   * @returns {string} Formatted status
   */
  function formatStatus(status) {
    const colorMap = {
      'pending': COLORS.statusPending,
      'in-progress': COLORS.statusInProgress,
      'completed': COLORS.statusCompleted,
      'cancelled': COLORS.statusCancelled
    };
    
    const color = colorMap[status] || COLORS.reset;
    return `${color}${status}${COLORS.reset}`;
  }

  /**
   * Format priority with color
   * @param {string} priority - Task priority
   * @returns {string} Formatted priority
   */
  function formatPriority(priority) {
    const colorMap = {
      'low': COLORS.priorityLow,
      'medium': COLORS.priorityMedium,
      'high': COLORS.priorityHigh,
      'urgent': COLORS.priorityUrgent
    };
    
    const color = colorMap[priority] || COLORS.reset;
    return `${color}${priority}${COLORS.reset}`;
  }

  /**
   * Format tags as colored spans
   * @param {string[]} tags - Array of tags
   * @returns {string} Formatted tags
   */
  function formatTags(tags) {
    if (!tags || !Array.isArray(tags) || tags.length === 0) {
      return '<span style="color: #6b7280;">-</span>';
    }
    
    return tags.map(tag => `${COLORS.tag}#${tag}${COLORS.reset}`).join(' ');
  }

  /**
   * Format ID for display
   * @param {string} id - Task ID
   * @returns {string} Formatted ID
   */
  function formatId(id) {
    return `${COLORS.id}${id}${COLORS.reset}`;
  }

  /**
   * Format a single task as a table row
   * @param {Object} task - Task object
   * @param {Object} options - Display options
   * @returns {string} Formatted task row
   */
  function formatTaskRow(task, options = {}) {
    const { showId = true, showDescription = false } = options;
    
    const id = showId ? formatId(truncate(task.id, 8)) : '';
    const title = Utils.escapeHtml(task.title);
    const status = formatStatus(task.status);
    const priority = formatPriority(task.priority);
    const dueDate = task.dueDate ? formatDueDate(task.dueDate) : '<span style="color: #6b7280;">-</span>';
    const tags = formatTags(task.tags);
    
    let row = `<div class="task-row">`;
    if (showId) {
      row += `<span class="task-id">${id}</span> `;
    }
    row += `<span class="task-title">${title}</span> `;
    row += `<span class="task-status">${status}</span> `;
    row += `<span class="task-priority">${priority}</span> `;
    row += `<span class="task-due">${dueDate}</span> `;
    row += `<span class="task-tags">${tags}</span>`;
    row += `</div>`;
    
    if (showDescription && task.description) {
      row += `<div class="task-description">${Utils.escapeHtml(truncate(task.description, 100))}</div>`;
    }
    
    return row;
  }

  /**
   * Format due date with relative indicator
   * @param {string} dueDate - Due date in YYYY-MM-DD format
   * @returns {string} Formatted due date
   */
  function formatDueDate(dueDate) {
    if (!dueDate) return '-';
    
    const days = Utils.getDaysUntil(dueDate);
    
    if (days === null) {
      return COLORS.date + dueDate + COLORS.reset;
    }
    
    if (days < 0) {
      return `${COLORS.error}${dueDate} (overdue)${COLORS.reset}`;
    } else if (days === 0) {
      return `${COLORS.warning}${dueDate} (today)${COLORS.reset}`;
    } else if (days === 1) {
      return `${COLORS.warning}${dueDate} (tomorrow)${COLORS.reset}`;
    } else if (days <= 7) {
      return `${COLORS.info}${dueDate} (${days}d)${COLORS.reset}`;
    }
    
    return COLORS.date + dueDate + COLORS.reset;
  }

  /**
   * Truncate a string to specified length
   * @param {string} text - Text to truncate
   * @param {number} maxLength - Maximum length
   * @returns {string} Truncated text
   */
  function truncate(text, maxLength) {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength - 3) + '...';
  }

  /**
   * Format tasks as a table
   * @param {Object[]} tasks - Array of tasks
   * @param {Object} options - Display options
   * @returns {string} HTML table
   */
  function formatAsTable(tasks, options = {}) {
    if (!tasks || tasks.length === 0) {
      return `<div class="no-results">${COLORS.dim}No tasks found${COLORS.reset}</div>`;
    }

    let html = `<div class="task-table">`;
    html += `<div class="table-header">`;
    html += `<span class="col-id">ID</span> `;
    html += `<span class="col-title">Title</span> `;
    html += `<span class="col-status">Status</span> `;
    html += `<span class="col-priority">Priority</span> `;
    html += `<span class="col-due">Due</span> `;
    html += `<span class="col-tags">Tags</span>`;
    html += `</div>`;

    tasks.forEach(task => {
      html += formatTaskRow(task, options);
    });

    html += `</div>`;
    html += `<div class="table-footer">${COLORS.dim}${tasks.length} task(s)${COLORS.reset}</div>`;

    return html;
  }

  /**
   * Format tasks as a compact list
   * @param {Object[]} tasks - Array of tasks
   * @param {Object} options - Display options
   * @returns {string} HTML list
   */
  function formatAsList(tasks, options = {}) {
    if (!tasks || tasks.length === 0) {
      return `<div class="no-results">${COLORS.dim}No tasks found${COLORS.reset}</div>`;
    }

    let html = `<ul class="task-list">`;
    
    tasks.forEach((task, index) => {
      const statusIcon = getStatusIcon(task.status);
      const priorityIcon = getPriorityIcon(task.priority);
      const dueDate = task.dueDate ? formatDueDate(task.dueDate) : '';
      const tags = task.tags && task.tags.length > 0 ? ` [${task.tags.join(', ')}]` : '';
      
      html += `<li class="task-item">`;
      html += `<span class="task-number">${index + 1}.</span> `;
      html += `<span class="task-status-icon">${statusIcon}</span> `;
      html += `<span class="task-title">${Utils.escapeHtml(task.title)}</span> `;
      html += `<span class="task-priority-icon">${priorityIcon}</span> `;
      if (dueDate) {
        html += `<span class="task-due">${dueDate}</span>`;
      }
      if (tags) {
        html += `<span class="task-tags">${COLORS.tag}${tags}${COLORS.reset}</span>`;
      }
      html += ` <span class="task-id">${formatId(truncate(task.id, 8))}</span>`;
      html += `</li>`;
    });

    html += `</ul>`;
    html += `<div class="list-footer">${COLORS.dim}${tasks.length} task(s)${COLORS.reset}</div>`;

    return html;
  }

  /**
   * Format tasks as JSON
   * @param {Object[]} tasks - Array of tasks
   * @param {Object} options - Display options
   * @returns {string} JSON string
   */
  function formatAsJson(tasks, options = {}) {
    const { pretty = true } = options;
    
    if (!tasks || tasks.length === 0) {
      return pretty ? '[]' : '[]';
    }

    return pretty ? JSON.stringify(tasks, null, 2) : JSON.stringify(tasks);
  }

  /**
   * Get status icon
   * @param {string} status - Task status
   * @returns {string} Status icon
   */
  function getStatusIcon(status) {
    const icons = {
      'pending': '○',
      'in-progress': '◐',
      'completed': '●',
      'cancelled': '✕'
    };
    return icons[status] || '?';
  }

  /**
   * Get priority icon
   * @param {string} priority - Task priority
   * @returns {string} Priority icon
   */
  function getPriorityIcon(priority) {
    const icons = {
      'low': '↓',
      'medium': '↔',
      'high': '↑',
      'urgent': '⚡'
    };
    return icons[priority] || '?';
  }

  /**
   * Format a single task detail view
   * @param {Object} task - Task object
   * @returns {string} Formatted task details
   */
  function formatTaskDetail(task) {
    if (!task) {
      return `${COLORS.error}Task not found${COLORS.reset}`;
    }

    let html = `<div class="task-detail">`;
    html += `<div class="detail-header">`;
    html += `${COLORS.bold}Task Details${COLORS.reset}`;
    html += `</div>`;
    
    html += `<div class="detail-row"><span class="detail-label">ID:</span> ${formatId(task.id)}</div>`;
    html += `<div class="detail-row"><span class="detail-label">Title:</span> ${Utils.escapeHtml(task.title)}</div>`;
    
    if (task.description) {
      html += `<div class="detail-row"><span class="detail-label">Description:</span> ${Utils.escapeHtml(task.description)}</div>`;
    }
    
    html += `<div class="detail-row"><span class="detail-label">Status:</span> ${formatStatus(task.status)}</div>`;
    html += `<div class="detail-row"><span class="detail-label">Priority:</span> ${formatPriority(task.priority)}</div>`;
    
    if (task.dueDate) {
      html += `<div class="detail-row"><span class="detail-label">Due Date:</span> ${formatDueDate(task.dueDate)}</div>`;
    }
    
    if (task.tags && task.tags.length > 0) {
      html += `<div class="detail-row"><span class="detail-label">Tags:</span> ${formatTags(task.tags)}</div>`;
    }
    
    html += `<div class="detail-row"><span class="detail-label">Created:</span> ${COLORS.date}${Utils.formatTimestamp(task.createdAt)}${COLORS.reset}</div>`;
    html += `<div class="detail-row"><span class="detail-label">Updated:</span> ${COLORS.date}${Utils.formatTimestamp(task.updatedAt)}${COLORS.reset}</div>`;
    
    html += `</div>`;

    return html;
  }

  /**
   * Format error message
   * @param {Error} error - Error object
   * @returns {string} Formatted error
   */
  function formatError(error) {
    const message = error.message || 'An unknown error occurred';
    const code = error.code || 'ERROR';
    
    return `<div class="error-message">${COLORS.error}Error: ${message}${COLORS.reset}</div>`;
  }

  /**
   * Format success message
   * @param {string} message - Success message
   * @returns {string} Formatted message
   */
  function formatSuccess(message) {
    return `<div class="success-message">${COLORS.success}✓ ${message}${COLORS.reset}</div>`;
  }

  /**
   * Format info message
   * @param {string} message - Info message
   * @returns {string} Formatted message
   */
  function formatInfo(message) {
    return `<div class="info-message">${COLORS.info}ℹ ${message}${COLORS.reset}</div>`;
  }

  /**
   * Format warning message
   * @param {string} message - Warning message
   * @returns {string} Formatted message
   */
  function formatWarning(message) {
    return `<div class="warning-message">${COLORS.warning}⚠ ${message}${COLORS.reset}</div>`;
  }

  /**
   * Format command output
   * @param {string} command - Command that was executed
   * @param {string} output - Command output
   * @returns {string} Formatted command output
   */
  function formatCommandOutput(command, output) {
    const timestamp = new Date().toLocaleTimeString();
    return `<div class="command-output">`;
    if (command) {
      output = `<div class="command-header">${COLORS.dim}${timestamp} > ${command}${COLORS.reset}</div>` + output;
    }
    output += `</div>`;
    return output;
  }

  /**
   * Get help text for a command
   * @param {string} command - Command name
   * @returns {string} Help text
   */
  function formatHelp(command) {
    const helpTexts = {
      'add': `
${COLORS.bold}add${COLORS.reset} "Task Title" [options]
  Create a new task

  Options:
    -d, --description "Description"  Task description
    -s, --status STATUS              Status: pending, in-progress, completed, cancelled
    -p, --priority PRIORITY         Priority: low, medium, high, urgent
    --due-date YYYY-MM-DD           Due date
    -t, --tag TAG1 TAG2...          Tags (space or comma separated)

  Examples:
    add "Buy groceries" --priority high --due 2024-02-25 --tags shopping,home
    add "Fix bug" -p urgent -t bug,urgent
`,
      'list': `
${COLORS.bold}list${COLORS.reset} [filters]
  List all tasks or filter by criteria

  Filters:
    --status STATUS              Filter by status
    --priority PRIORITY          Filter by priority
    --tag TAG1 TAG2...           Filter by tags (any match)
    --due-before YYYY-MM-DD     Due before date
    --due-after YYYY-MM-DD      Due after date
    --format FORMAT              Format: table, list, json

  Examples:
    list --status pending --priority high
    list --tag shopping
    list --due-before 2024-02-28
`,
      'update': `
${COLORS.bold}update${COLORS.reset} <id> [field value] [options]
  Update an existing task

  Fields:
    title "New Title"
    description "New Description"
    status STATUS
    priority PRIORITY
    due-date YYYY-MM-DD
    tags TAG1 TAG2...

  Examples:
    update abc-123 status completed
    update def-456 priority urgent
`,
      'delete': `
${COLORS.bold}delete${COLORS.reset} <id> [--force]
  Delete a task

  Options:
    --force    Skip confirmation prompt

  Examples:
    delete abc-123
    delete def-456 --force
`,
      'filter': `
${COLORS.bold}filter${COLORS.reset} [filters]
  Set persistent filter for list command

  Same filters as list command
`,
      'search': `
${COLORS.bold}search${COLORS.reset} <query> [options]
  Search tasks by title, description, or tags

  Options:
    --fields FIELD1,FIELD2     Fields to search: title, description, tags
    --case-sensitive            Case sensitive search

  Examples:
    search "groceries"
    search "bug" --fields title,description
`,
      'help': `
${COLORS.bold}help${COLORS.reset} [command]
  Show help for a command or list all commands

  Available commands:
    add, list, update, delete, filter, search, help, export, import
`
    };

    if (command && helpTexts[command]) {
      return `<pre class="help-text">${helpTexts[command]}</pre>`;
    }

    // General help
    let help = `<div class="help-list">`;
    help += `${COLORS.bold}Available Commands:${COLORS.reset}\n\n`;
    help += `${COLORS.info}add${COLORS.reset}       - Create a new task\n`;
    help += `${COLORS.info}list${COLORS.reset}      - List tasks with optional filters\n`;
    help += `${COLORS.info}update${COLORS.reset}    - Update an existing task\n`;
    help += `${COLORS.info}delete${COLORS.reset}    - Delete a task\n`;
    help += `${COLORS.info}filter${COLORS.reset}    - Set persistent filter\n`;
    help += `${COLORS.info}search${COLORS.reset}    - Search tasks\n`;
    help += `${COLORS.info}export${COLORS.reset}    - Export tasks to JSON\n`;
    help += `${COLORS.info}import${COLORS.reset}    - Import tasks from JSON\n`;
    help += `${COLORS.info}help${COLORS.reset}      - Show this help\n`;
    help += `\n${COLORS.dim}Type "help <command>" for detailed usage${COLORS.reset}`;
    help += `</div>`;

    return help;
  }

  // Public API
  return {
    COLORS,
    formatStatus,
    formatPriority,
    formatTags,
    formatId,
    formatDueDate,
    formatTaskRow,
    formatAsTable,
    formatAsList,
    formatAsJson,
    formatTaskDetail,
    formatError,
    formatSuccess,
    formatInfo,
    formatWarning,
    formatCommandOutput,
    formatHelp,
    truncate,
    getStatusIcon,
    getPriorityIcon
  };
})();

// Export for Node.js/CommonJS environments
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Formatters;
}
