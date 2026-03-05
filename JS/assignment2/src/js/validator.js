/**
 * Input Validation Module for Task Management Application
 * Implements all validation rules and error codes
 */

const Validator = (function() {
  'use strict';

  // What these next lines do:
  // Stable error codes so UI/CLI can react predictably.
  // Why this matters in this project:
  // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
  const ERROR_CODES = {
    // What these next lines do:
    // Validation Errors
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Validation Errors.
    VALIDATION_TITLE_REQUIRED: 'VALIDATION_TITLE_REQUIRED',
    VALIDATION_TITLE_LENGTH: 'VALIDATION_TITLE_LENGTH',
    VALIDATION_TITLE_INVALID: 'VALIDATION_TITLE_INVALID',
    VALIDATION_DESCRIPTION_LENGTH: 'VALIDATION_DESCRIPTION_LENGTH',
    VALIDATION_STATUS_INVALID: 'VALIDATION_STATUS_INVALID',
    VALIDATION_PRIORITY_INVALID: 'VALIDATION_PRIORITY_INVALID',
    VALIDATION_DUE_DATE_FORMAT: 'VALIDATION_DUE_DATE_FORMAT',
    VALIDATION_DUE_DATE_INVALID: 'VALIDATION_DUE_DATE_INVALID',
    VALIDATION_DUE_DATE_PAST: 'VALIDATION_DUE_DATE_PAST',
    VALIDATION_TAG_INVALID: 'VALIDATION_TAG_INVALID',
    VALIDATION_TAG_LENGTH: 'VALIDATION_TAG_LENGTH',
    VALIDATION_TAG_COUNT: 'VALIDATION_TAG_COUNT',
    
    // What these next lines do:
    // Operation Errors
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Operation Errors.
    TASK_NOT_FOUND: 'TASK_NOT_FOUND',
    STORAGE_ERROR: 'STORAGE_ERROR',
    COMMAND_INVALID: 'COMMAND_INVALID',
    COMMAND_ARGS_INVALID: 'COMMAND_ARGS_INVALID',
    
    // What these next lines do:
    // System Errors
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: System Errors.
    STORAGE_QUOTA_EXCEEDED: 'STORAGE_QUOTA_EXCEEDED',
    DATA_CORRUPTION: 'DATA_CORRUPTION'
  };

  // What these next lines do:
  // Allowed enum-like values.
  // Why this matters in this project:
  // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
  const ALLOWED_STATUSES = ['pending', 'in-progress', 'completed', 'cancelled'];
  const ALLOWED_PRIORITIES = ['low', 'medium', 'high', 'urgent'];

  // What these next lines do:
  // Regex patterns for accepted inputs.
  // Why this matters in this project:
  // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
  const PATTERNS = {
    title: /^[\w\s\.,!\-?()\[\]'":;]+$/,
    tag: /^[\w\-]+$/,
    uuid: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
  };

  // What these next lines do:
  // Length limits used by field validators.
  // Why this matters in this project:
  // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
  const LIMITS = {
    title: { min: 1, max: 100 },
    description: { max: 1000 },
    tag: { min: 1, max: 20 },
    tags: { max: 10 }
  };

  /**
   * Custom error class for validation
   */
  class ValidationError extends Error {
    constructor(code, message, field = null) {
      super(message);
      this.code = code;
      this.name = 'ValidationError';
      this.field = field;
    }
  }

  /**
   * Get error message for error code
   * @param {string} code - Error code
   * @param {Object} params - Parameters for message formatting
   * @returns {string} Formatted error message
   */
  function getErrorMessage(code, params = {}) {
    const messages = {
      'VALIDATION_TITLE_REQUIRED': 'Task title is required',
      'VALIDATION_TITLE_LENGTH': 'Task title must be 1-100 characters',
      'VALIDATION_TITLE_INVALID': 'Task title contains invalid characters',
      'VALIDATION_DESCRIPTION_LENGTH': 'Task description must be 0-1000 characters',
      'VALIDATION_STATUS_INVALID': 'Status must be: pending, in-progress, completed, or cancelled',
      'VALIDATION_PRIORITY_INVALID': 'Priority must be: low, medium, high, or urgent',
      'VALIDATION_DUE_DATE_FORMAT': 'Due date must be in YYYY-MM-DD format',
      'VALIDATION_DUE_DATE_INVALID': 'Due date is not a valid date',
      'VALIDATION_DUE_DATE_PAST': 'Due date is in the past. Use --force to override',
      'VALIDATION_TAG_INVALID': 'Tags must contain only alphanumeric characters and hyphens',
      'VALIDATION_TAG_LENGTH': 'Each tag must be 1-20 characters',
      'VALIDATION_TAG_COUNT': 'Maximum 10 tags allowed per task',
      'TASK_NOT_FOUND': `Task with ID '${params.id}' not found`,
      'STORAGE_ERROR': `Failed to access local storage: ${params.details || 'unknown error'}`,
      'COMMAND_INVALID': `Invalid command: ${params.command || 'unknown'}`,
      'COMMAND_ARGS_INVALID': `Invalid arguments for command: ${params.command || 'unknown'}`,
      'STORAGE_QUOTA_EXCEEDED': 'Storage quota exceeded. Please delete some tasks',
      'DATA_CORRUPTION': 'Stored data is corrupted. Data has been reset'
    };

    return messages[code] || 'Unknown validation error';
  }

  /**
   * Validate task title
   * @param {string} title - Title to validate
   * @returns {Object} Validation result
   */
  function validateTitle(title) {
    const trimmed = Utils.trimText(title);
    
    if (trimmed.length === 0) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_TITLE_REQUIRED,
          getErrorMessage('VALIDATION_TITLE_REQUIRED'),
          'title'
        )
      };
    }

    if (trimmed.length < LIMITS.title.min || trimmed.length > LIMITS.title.max) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_TITLE_LENGTH,
          getErrorMessage('VALIDATION_TITLE_LENGTH'),
          'title'
        )
      };
    }

    // What these next lines do:
    // Check for invalid characters (but allow common punctuation)
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check for invalid characters (but allow common punctuation).
    if (!PATTERNS.title.test(trimmed)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_TITLE_INVALID,
          getErrorMessage('VALIDATION_TITLE_INVALID'),
          'title'
        )
      };
    }

    return { valid: true, value: trimmed };
  }

  /**
   * Validate task description
   * @param {string} description - Description to validate
   * @returns {Object} Validation result
   */
  function validateDescription(description) {
    if (description === null || description === undefined || description === '') {
      return { valid: true, value: '' };
    }

    // What these next lines do:
    // Strip HTML tags
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const sanitized = Utils.stripHtmlTags(description);
    const trimmed = Utils.trimText(sanitized);

    if (trimmed.length > LIMITS.description.max) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_DESCRIPTION_LENGTH,
          getErrorMessage('VALIDATION_DESCRIPTION_LENGTH'),
          'description'
        )
      };
    }

    return { valid: true, value: trimmed };
  }

  /**
   * Validate task status
   * @param {string} status - Status to validate
   * @returns {Object} Validation result
   */
  function validateStatus(status) {
    if (!status) {
      // What these next lines do:
      // Default status
      // Why this matters in this project:
      // Returning this value here defines the output contract of the helper and keeps callers predictable.
      return { valid: true, value: 'pending' };
    }

    const normalized = Utils.trimText(status).toLowerCase();

    if (!ALLOWED_STATUSES.includes(normalized)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_STATUS_INVALID,
          getErrorMessage('VALIDATION_STATUS_INVALID'),
          'status'
        )
      };
    }

    return { valid: true, value: normalized };
  }

  /**
   * Validate task priority
   * @param {string} priority - Priority to validate
   * @returns {Object} Validation result
   */
  function validatePriority(priority) {
    if (!priority) {
      // What these next lines do:
      // Default priority
      // Why this matters in this project:
      // Returning this value here defines the output contract of the helper and keeps callers predictable.
      return { valid: true, value: 'medium' };
    }

    const normalized = Utils.trimText(priority).toLowerCase();

    if (!ALLOWED_PRIORITIES.includes(normalized)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_PRIORITY_INVALID,
          getErrorMessage('VALIDATION_PRIORITY_INVALID'),
          'priority'
        )
      };
    }

    return { valid: true, value: normalized };
  }

  /**
   * Validate due date
   * @param {string} dueDate - Due date to validate
   * @param {Object} options - Validation options
   * @returns {Object} Validation result
   */
  function validateDueDate(dueDate, options = {}) {
    const { allowPast = false, force = false } = options;

    if (!dueDate || dueDate === '') {
      return { valid: true, value: null };
    }

    const trimmed = Utils.trimText(dueDate);

    // What these next lines do:
    // Check format
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
    if (!dateRegex.test(trimmed)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_DUE_DATE_FORMAT,
          getErrorMessage('VALIDATION_DUE_DATE_FORMAT'),
          'dueDate'
        )
      };
    }

    // What these next lines do:
    // Check valid date
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const date = Utils.parseDate(trimmed);
    if (!date) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_DUE_DATE_INVALID,
          getErrorMessage('VALIDATION_DUE_DATE_INVALID'),
          'dueDate'
        )
      };
    }

    // What these next lines do:
    // By default we block past due dates unless caller passes force=true.
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    if (!force && Utils.isDateInPast(trimmed)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_DUE_DATE_PAST,
          getErrorMessage('VALIDATION_DUE_DATE_PAST'),
          'dueDate'
        )
      };
    }

    return { valid: true, value: trimmed };
  }

  /**
   * Validate tags
   * @param {string[]|string} tags - Tags to validate
   * @returns {Object} Validation result
   */
  function validateTags(tags) {
    // What these next lines do:
    // Handle string input (comma or space separated)
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Handle string input (comma or space separated).
    let tagArray = tags;
    if (typeof tags === 'string') {
      tagArray = Utils.parseTags(tags);
    }

    if (!Array.isArray(tagArray)) {
      tagArray = [];
    }

    // What these next lines do:
    // Remove duplicates and empty values
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const uniqueTags = [...new Set(tagArray.map(t => Utils.trimText(t)))].filter(t => t.length > 0);

    // What these next lines do:
    // Check count
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check count.
    if (uniqueTags.length > LIMITS.tags.max) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.VALIDATION_TAG_COUNT,
          getErrorMessage('VALIDATION_TAG_COUNT'),
          'tags'
        )
      };
    }

    // What these next lines do:
    // Validate each tag
    // Why this matters in this project:
    // Validation here stops bad input early so broken data does not spread to storage or UI.
    for (const tag of uniqueTags) {
      // What these next lines do:
      // Check length
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check length.
      if (tag.length < LIMITS.tag.min || tag.length > LIMITS.tag.max) {
        return {
          valid: false,
          error: new ValidationError(
            ERROR_CODES.VALIDATION_TAG_LENGTH,
            getErrorMessage('VALIDATION_TAG_LENGTH'),
            'tags'
          )
        };
      }

      // What these next lines do:
      // Check pattern
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Check pattern.
      if (!PATTERNS.tag.test(tag)) {
        return {
          valid: false,
          error: new ValidationError(
            ERROR_CODES.VALIDATION_TAG_INVALID,
            getErrorMessage('VALIDATION_TAG_INVALID'),
            'tags'
          )
        };
      }
    }

    return { valid: true, value: uniqueTags };
  }

  /**
   * Validate a complete task object
   * @param {Object} taskData - Task data to validate
   * @param {Object} options - Validation options
   * @returns {Object} Validation result with validated data
   */
  function validateTask(taskData, options = {}) {
    // What these next lines do:
    // Collect all field errors so caller gets full feedback at once.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const errors = [];
    const validated = {};

    // What these next lines do:
    // Validate title (required)
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const titleResult = validateTitle(taskData.title);
    if (!titleResult.valid) {
      errors.push(titleResult.error);
    } else {
      validated.title = titleResult.value;
    }

    // What these next lines do:
    // Validate description (optional)
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const descResult = validateDescription(taskData.description);
    if (!descResult.valid) {
      errors.push(descResult.error);
    } else {
      validated.description = descResult.value;
    }

    // What these next lines do:
    // Validate status
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const statusResult = validateStatus(taskData.status);
    if (!statusResult.valid) {
      errors.push(statusResult.error);
    } else {
      validated.status = statusResult.value;
    }

    // What these next lines do:
    // Validate priority
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const priorityResult = validatePriority(taskData.priority);
    if (!priorityResult.valid) {
      errors.push(priorityResult.error);
    } else {
      validated.priority = priorityResult.value;
    }

    // What these next lines do:
    // Validate due date
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const dueDateResult = validateDueDate(taskData.dueDate, options);
    if (!dueDateResult.valid) {
      errors.push(dueDateResult.error);
    } else {
      validated.dueDate = dueDateResult.value;
    }

    // What these next lines do:
    // Validate tags
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const tagsResult = validateTags(taskData.tags);
    if (!tagsResult.valid) {
      errors.push(tagsResult.error);
    } else {
      validated.tags = tagsResult.value;
    }

    if (errors.length > 0) {
      return { valid: false, errors };
    }

    return { valid: true, data: validated };
  }

  /**
   * Validate task ID format
   * @param {string} id - ID to validate
   * @returns {boolean} True if valid UUID format
   */
  function validateId(id) {
    if (!id || typeof id !== 'string') {
      return false;
    }
    return PATTERNS.uuid.test(id);
  }

  /**
   * Validate filter/query options
   * @param {Object} query - Query object to validate
   * @returns {Object} Validation result
   */
  function validateQuery(query) {
    // What these next lines do:
    // Query validation is lighter but still checks known field formats.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const errors = [];

    if (query.status) {
      const statusResult = validateStatus(query.status);
      if (!statusResult.valid) {
        errors.push(statusResult.error);
      }
    }

    if (query.priority) {
      const priorityResult = validatePriority(query.priority);
      if (!priorityResult.valid) {
        errors.push(priorityResult.error);
      }
    }

    if (query.dueDate) {
      const dueDateResult = validateDueDate(query.dueDate, { allowPast: true, force: true });
      if (!dueDateResult.valid) {
        errors.push(dueDateResult.error);
      }
    }

    if (query.dueBefore) {
      const dueDateResult = validateDueDate(query.dueBefore, { allowPast: true, force: true });
      if (!dueDateResult.valid) {
        errors.push(dueDateResult.error);
      }
    }

    if (query.dueAfter) {
      const dueDateResult = validateDueDate(query.dueAfter, { allowPast: true, force: true });
      if (!dueDateResult.valid) {
        errors.push(dueDateResult.error);
      }
    }

    // What these next lines do:
    // Tags are validated differently in filters (any tag is acceptable)
    // Why this matters in this project:
    // Validation here stops bad input early so broken data does not spread to storage or UI.
    if (query.tags) {
      const tagsResult = validateTags(query.tags);
      if (!tagsResult.valid) {
        errors.push(tagsResult.error);
      }
    }

    if (errors.length > 0) {
      return { valid: false, errors };
    }

    return { valid: true };
  }

  /**
   * Validate command arguments
   * @param {string} command - Command name
   * @param {Object} args - Arguments to validate
   * @returns {Object} Validation result
   */
  function validateCommand(command, args) {
    // What these next lines do:
    // Command-level validation catches missing required arguments early.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const errors = [];
    const validCommands = ['add', 'list', 'update', 'delete', 'filter', 'search', 'help', 'export', 'import', 'clear'];

    if (!validCommands.includes(command)) {
      return {
        valid: false,
        error: new ValidationError(
          ERROR_CODES.COMMAND_INVALID,
          getErrorMessage('COMMAND_INVALID', { command }),
          'command'
        )
      };
    }

    // What these next lines do:
    // Add command requires title
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Add command requires title.
    if (command === 'add') {
      if (!args.title || Utils.trimText(args.title).length === 0) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Add command requires a title',
          'title'
        ));
      }
    }

    // What these next lines do:
    // Update command requires ID
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    if (command === 'update') {
      if (!args.id || !validateId(args.id)) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Update command requires a valid task ID',
          'id'
        ));
      }
    }

    // What these next lines do:
    // Delete command requires ID
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Delete command requires ID.
    if (command === 'delete') {
      if (!args.id || !validateId(args.id)) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Delete command requires a valid task ID',
          'id'
        ));
      }
    }

    // What these next lines do:
    // Search command requires query
    // Why this matters in this project:
    // Search behavior directly affects discoverability, so this logic must be predictable.
    if (command === 'search') {
      if (!args.query || Utils.trimText(args.query).length === 0) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Search command requires a search query',
          'query'
        ));
      }
    }

    if (errors.length > 0) {
      return { valid: false, errors };
    }

    return { valid: true };
  }

  // What these next lines do:
  // Public API
  // Why this matters in this project:
  // Returning this value here defines the output contract of the helper and keeps callers predictable.
  return {
    ValidationError,
    ERROR_CODES,
    getErrorMessage,
    validateTitle,
    validateDescription,
    validateStatus,
    validatePriority,
    validateDueDate,
    validateTags,
    validateTask,
    validateId,
    validateQuery,
    validateCommand,
    ALLOWED_STATUSES,
    ALLOWED_PRIORITIES
  };
})();

// What these next lines do:
// Export for Node.js/CommonJS environments
// Why this matters in this project:
// This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Export for Node.js/CommonJS environments.
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Validator;
}
