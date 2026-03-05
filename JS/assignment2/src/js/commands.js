/**
 * Command Handlers for Task Management Application
 * Parses and executes user commands
 */

const Commands = (function() {
  'use strict';

  /**
   * Parse a command string into command name and arguments
   * @param {string} input - Raw command input
   * @returns {Object} Parsed command
   */
  function parseCommand(input) {
    if (!input || typeof input !== 'string') {
      return { command: null, args: {}, raw: input };
    }

    const trimmed = input.trim();
    if (trimmed === '') {
      return { command: null, args: {}, raw: input };
    }

    // What these next lines do:
    // First split text into tokens while respecting quoted values.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const tokens = tokenize(trimmed);
    
    if (tokens.length === 0) {
      return { command: null, args: {}, raw: input };
    }

    const command = tokens[0].toLowerCase();
    const args = parseArgs(tokens.slice(1));

    return { command, args, raw: input };
  }

  /**
   * Tokenize command input handling quotes
   * @param {string} input - Input string
   * @returns {string[]} Array of tokens
   */
  function tokenize(input) {
    const tokens = [];
    let current = '';
    let inQuote = false;
    let quoteChar = '';

    for (let i = 0; i < input.length; i++) {
      const char = input[i];

      if ((char === '"' || char === "'") && !inQuote) {
        inQuote = true;
        quoteChar = char;
      } else if (char === quoteChar && inQuote) {
        inQuote = false;
        quoteChar = '';
      } else if (char === ' ' && !inQuote) {
        if (current.length > 0) {
          tokens.push(current);
          current = '';
        }
      } else {
        current += char;
      }
    }

    if (current.length > 0) {
      tokens.push(current);
    }

    return tokens;
  }

  /**
   * Parse argument tokens into options object
   * @param {string[]} tokens - Argument tokens
   * @returns {Object} Parsed arguments
   */
  function parseArgs(tokens) {
    const args = {};
    let i = 0;

    while (i < tokens.length) {
      const token = tokens[i];

      // What these next lines do:
      // Long flags: --status pending
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Long flags: --status pending.
      if (token.startsWith('--')) {
        const flag = token.substring(2);
        
        // What these next lines do:
        // Check if next token is a value
        // Why this matters in this project:
        // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check if next token is a value.
        if (i + 1 < tokens.length && !tokens[i + 1].startsWith('-')) {
          args[flag] = tokens[i + 1];
          i += 2;
        } else {
          args[flag] = true;
          i++;
        }
      } 
      // What these next lines do:
      // Short flags: -s pending or grouped like -abc
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Short flags: -s pending or grouped like -abc.
      else if (token.startsWith('-') && !token.startsWith('--')) {
        const flag = token.substring(1);
        
        // What these next lines do:
        // Multiple short flags in one (-abc = -a -b -c)
        // Why this matters in this project:
        // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Multiple short flags in one (-abc = -a -b -c).
        if (flag.length > 1) {
          for (const f of flag) {
            // What these next lines do:
            // Check if next token is a value for the last flag
            // Why this matters in this project:
            // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check if next token is a value for the last flag.
            if (f === flag.slice(-1) && i + 1 < tokens.length && !tokens[i + 1].startsWith('-')) {
              args[f] = tokens[i + 1];
              i += 2;
            } else {
              args[f] = true;
              i++;
            }
          }
        } else {
          // What these next lines do:
          // Single short flag
          // Why this matters in this project:
          // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Single short flag.
          if (i + 1 < tokens.length && !tokens[i + 1].startsWith('-')) {
            args[flag] = tokens[i + 1];
            i += 2;
          } else {
            args[flag] = true;
            i++;
          }
        }
      } 
      // What these next lines do:
      // Handle positional arguments
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Handle positional arguments.
      else {
        if (args._ === undefined) {
          args._ = [];
        }
        args._.push(token);
        i++;
      }
    }

    // What these next lines do:
    // Provide common positional fallbacks.
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Provide common positional fallbacks.
    args.title = args.title || args._?.[0] || null;
    args.id = args.id || args._?.[0] || null;
    args.query = args.query || args._?.[0] || null;

    // What these next lines do:
    // Map shorthand aliases to full field names.
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Map shorthand aliases to full field names.
    if (args.d) args.description = args.description || args.d;
    if (args.s) args.status = args.status || args.s;
    if (args.p) args.priority = args.priority || args.p;
    if (args.t) args.tags = args.tags || args.t;
    if (args.due) args.dueDate = args.dueDate || args.due;
    if (args['due-date']) args.dueDate = args.dueDate || args['due-date'];
    if (args['due-before']) args.dueBefore = args.dueBefore || args['due-before'];
    if (args['due-after']) args.dueAfter = args.dueAfter || args['due-after'];
    if (args.f) args.format = args.format || args.f;
    if (args.h) args.help = args.help || args.h;

    return args;
  }

  /**
   * Execute a parsed command
   * @param {Object} parsed - Parsed command object
   * @returns {Promise<Object>} Command result
   */
  async function executeCommand(parsed) {
    const { command, args } = parsed;

    if (!command) {
      return { success: false, output: Formatters.formatWarning('No command entered. Type "help" for available commands.') };
    }

    try {
      // What these next lines do:
      // Route command name to its handler.
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Route command name to its handler.
      switch (command) {
        case 'add':
          return await handleAdd(args);
        case 'list':
          return await handleList(args);
        case 'update':
          return await handleUpdate(args);
        case 'delete':
          return await handleDelete(args);
        case 'filter':
          return await handleFilter(args);
        case 'search':
          return await handleSearch(args);
        case 'help':
          return handleHelp(args);
        case 'export':
          return await handleExport(args);
        case 'import':
          return await handleImport(args);
        case 'clear':
          return await handleClear(args);
        default:
          return { 
            success: false, 
            output: Formatters.formatError(new Error(`Unknown command: ${command}`)) 
          };
      }
    } catch (error) {
      return { 
        success: false, 
        output: Formatters.formatError(error) 
      };
    }
  }

  /**
   * Handle add command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleAdd(args) {
    if (!args.title) {
      return {
        success: false,
        output: Formatters.formatError(new Error('Task title is required. Usage: add "Title" [options]'))
      };
    }

    const taskData = {
      title: args.title,
      description: args.description,
      status: args.status,
      priority: args.priority,
      dueDate: args.dueDate,
      tags: args.tags
    };

    // What these next lines do:
    // Parse tags if they're a string
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Parse tags if they're a string.
    if (typeof taskData.tags === 'string') {
      taskData.tags = Utils.parseTags(taskData.tags);
    }

    const task = await TaskManager.createTask(taskData, { force: args.force });
    
    return {
      success: true,
      output: Formatters.formatSuccess(`Task created: "${task.title}"`) + 
             `\n${Formatters.formatTaskDetail(task)}`
    };
  }

  /**
   * Handle list command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleList(args) {
    // What these next lines do:
    // Build query only from provided filter arguments.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const query = {};
    
    if (args.status) query.status = args.status;
    if (args.priority) query.priority = args.priority;
    if (args.tags) query.tags = args.tags;
    if (args.dueBefore) query.dueBefore = args.dueBefore;
    if (args.dueAfter) query.dueAfter = args.dueAfter;
    if (args.dueDate) query.dueDate = args.dueDate;

    const tasks = Object.keys(query).length > 0 
      ? await TaskManager.findTasks(query)
      : await TaskManager.getAllTasks();

    // What these next lines do:
    // Persistent filter is managed in TaskManager.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const filter = TaskManager.getFilter();
    if (filter) {
      // What these next lines do:
      // Note: tasks already include filter if applied
      // Why this matters in this project:
      // Applying this filter here ensures users only see tasks matching the chosen criteria.
    }

    const format = args.format || 'table';
    
    let output;
    switch (format) {
      case 'json':
        output = Formatters.formatAsJson(tasks, { pretty: true });
        break;
      case 'list':
        output = Formatters.formatAsList(tasks);
        break;
      case 'table':
      default:
        output = Formatters.formatAsTable(tasks, { showId: true, showDescription: true });
    }

    if (filter) {
      output = Formatters.formatInfo('Showing filtered results (use "filter" to set, "filter --clear" to remove)') + '\n' + output;
    }

    return { success: true, output };
  }

  /**
   * Handle update command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleUpdate(args) {
    if (!args.id) {
      return {
        success: false,
        output: Formatters.formatError(new Error('Task ID is required. Usage: update <id> [field value]'))
      };
    }

    // What these next lines do:
    // Check if we have field-value pairs
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const updates = {};
    
    // What these next lines do:
    // Handle positional arguments: update <id> <field> <value>
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    if (args._ && args._.length >= 2) {
      const field = args._[0];
      const value = args._[1];
      
      if (field === 'title') updates.title = value;
      else if (field === 'description') updates.description = value;
      else if (field === 'status') updates.status = value;
      else if (field === 'priority') updates.priority = value;
      else if (field === 'due-date') updates.dueDate = value;
      else if (field === 'tags') updates.tags = Utils.parseTags(value);
      else {
        return {
          success: false,
          output: Formatters.formatError(new Error(`Unknown field: ${field}`))
        };
      }
    } else {
      // What these next lines do:
      // Use named arguments
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Use named arguments.
      if (args.title !== undefined && args.title !== null) updates.title = args.title;
      if (args.description !== undefined) updates.description = args.description;
      if (args.status !== undefined) updates.status = args.status;
      if (args.priority !== undefined) updates.priority = args.priority;
      if (args.dueDate !== undefined) updates.dueDate = args.dueDate;
      if (args.tags !== undefined) {
        updates.tags = typeof args.tags === 'string' ? Utils.parseTags(args.tags) : args.tags;
      }
    }

    if (Object.keys(updates).length === 0) {
      return {
        success: false,
        output: Formatters.formatError(new Error('No updates specified. Usage: update <id> [field value]'))
      };
    }

    const task = await TaskManager.updateTask(args.id, updates, { force: args.force });
    
    return {
      success: true,
      output: Formatters.formatSuccess(`Task updated: "${task.title}"`) +
             `\n${Formatters.formatTaskDetail(task)}`
    };
  }

  /**
   * Handle delete command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleDelete(args) {
    if (!args.id) {
      return {
        success: false,
        output: Formatters.formatError(new Error('Task ID is required. Usage: delete <id>'))
      };
    }

    // What these next lines do:
    // Note: In a real CLI, we'd prompt for confirmation
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    // What these next lines do:
    // For this web app, we skip confirmation unless --force is not used
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const task = await TaskManager.getTask(args.id);
    
    if (!task) {
      return {
        success: false,
        output: Formatters.formatError(new Error(`Task with ID '${args.id}' not found`))
      };
    }

    await TaskManager.deleteTask(args.id);

    return {
      success: true,
      output: Formatters.formatSuccess(`Task deleted: "${task.title}"`)
    };
  }

  /**
   * Handle filter command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleFilter(args) {
    // What these next lines do:
    // Check for clear filter
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    if (args.clear) {
      TaskManager.clearFilter();
      return {
        success: true,
        output: Formatters.formatSuccess('Filter cleared')
      };
    }

    // What these next lines do:
    // Build filter from arguments
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const filter = {};
    
    if (args.status) filter.status = args.status;
    if (args.priority) filter.priority = args.priority;
    if (args.tags) filter.tags = args.tags;
    if (args.dueBefore) filter.dueBefore = args.dueBefore;
    if (args.dueAfter) filter.dueAfter = args.dueAfter;
    if (args.dueDate) filter.dueDate = args.dueDate;

    if (Object.keys(filter).length === 0) {
      const currentFilter = TaskManager.getFilter();
      if (currentFilter) {
        return {
          success: true,
          output: `Current filter: ${JSON.stringify(currentFilter)}\n` +
                  Formatters.formatInfo('Use "filter --clear" to remove the filter')
        };
      }
      return {
        success: true,
        output: Formatters.formatInfo('No filter set. Usage: filter [filters]')
      };
    }

    TaskManager.setFilter(filter);

    return {
      success: true,
      output: Formatters.formatSuccess(`Filter set: ${JSON.stringify(filter)}`)
    };
  }

  /**
   * Handle search command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleSearch(args) {
    if (!args.query) {
      return {
        success: false,
        output: Formatters.formatError(new Error('Search query is required. Usage: search "query"'))
      };
    }

    const options = {
      fields: args.fields ? args.fields.split(',') : ['title', 'description', 'tags'],
      caseSensitive: args['case-sensitive'] || false
    };

    const tasks = await TaskManager.searchTasks(args.query, options);

    if (tasks.length === 0) {
      return {
        success: true,
        output: Formatters.formatInfo(`No tasks found matching "${args.query}"`)
      };
    }

    const format = args.format || 'table';
    let output;
    
    switch (format) {
      case 'json':
        output = Formatters.formatAsJson(tasks);
        break;
      case 'list':
        output = Formatters.formatAsList(tasks);
        break;
      case 'table':
      default:
        output = Formatters.formatAsTable(tasks);
    }

    return {
      success: true,
      output: Formatters.formatInfo(`Found ${tasks.length} task(s) matching "${args.query}"`) + '\n' + output
    };
  }

  /**
   * Handle help command
   * @param {Object} args - Command arguments
   * @returns {Object} Command result
   */
  function handleHelp(args) {
    const command = args._?.[0] || args.command;
    
    return {
      success: true,
      output: Formatters.formatHelp(command)
    };
  }

  /**
   * Handle export command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleExport(args) {
    const data = await TaskManager.exportTasks();
    
    if (args.download) {
      return {
        success: true,
        output: data,
        download: true,
        filename: `tasks-export-${Utils.formatDate(new Date())}.json`
      };
    }

    return {
      success: true,
      output: Formatters.formatSuccess('Tasks exported:') + '\n' + Formatters.formatAsJson(JSON.parse(data).tasks)
    };
  }

  /**
   * Handle import command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleImport(args) {
    // What these next lines do:
    // In a web app, this would typically be handled through file upload
    // Why this matters in this project:
    // Returning this value here defines the output contract of the helper and keeps callers predictable.
    // What these next lines do:
    // For now, we'll return instructions
    // Why this matters in this project:
    // Returning this value here defines the output contract of the helper and keeps callers predictable.
    return {
      success: true,
      output: Formatters.formatInfo('To import tasks, use the file input or paste JSON data.')
    };
  }

  /**
   * Handle clear command
   * @param {Object} args - Command arguments
   * @returns {Promise<Object>} Command result
   */
  async function handleClear(args) {
    // What these next lines do:
    // Confirm is handled by UI
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Confirm is handled by UI.
    await TaskManager.clearAllTasks();
    
    return {
      success: true,
      output: Formatters.formatSuccess('All tasks cleared')
    };
  }

  // What these next lines do:
  // Public API
  // Why this matters in this project:
  // Returning this value here defines the output contract of the helper and keeps callers predictable.
  return {
    parseCommand,
    executeCommand,
    tokenize,
    parseArgs
  };
})();

// What these next lines do:
// Export for Node.js/CommonJS environments
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Export for Node.js/CommonJS environments.
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Commands;
}
