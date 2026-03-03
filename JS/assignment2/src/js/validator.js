/**
 * Input Validation Module for Task Management Application
 * Implements all validation rules and error codes
 */

const Validator = (function() {
  'use strict';

  // Error code definitions
  const ERROR_CODES = {
    // Validation Errors
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
    
    // Operation Errors
    TASK_NOT_FOUND: 'TASK_NOT_FOUND',
    STORAGE_ERROR: 'STORAGE_ERROR',
    COMMAND_INVALID: 'COMMAND_INVALID',
    COMMAND_ARGS_INVALID: 'COMMAND_ARGS_INVALID',
    
    // System Errors
    STORAGE_QUOTA_EXCEEDED: 'STORAGE_QUOTA_EXCEEDED',
    DATA_CORRUPTION: 'DATA_CORRUPTION'
  };

  // Allowed values
  const ALLOWED_STATUSES = ['pending', 'in-progress', 'completed', 'cancelled'];
  const ALLOWED_PRIORITIES = ['low', 'medium', 'high', 'urgent'];

  // Validation patterns
  const PATTERNS = {
    title: /^[\w\s\.,!\-?()\[\]'":;]+$/,
    tag: /^[\w\-]+$/,
    uuid: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
  };

  // Length limits
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

    // Check for invalid characters (but allow common punctuation)
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

    // Strip HTML tags
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
      // Default status
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
      // Default priority
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

    // Check format
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

    // Check valid date
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

    // Check if in past (unless force flag is set)
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
    // Handle string input (comma or space separated)
    let tagArray = tags;
    if (typeof tags === 'string') {
      tagArray = Utils.parseTags(tags);
    }

    if (!Array.isArray(tagArray)) {
      tagArray = [];
    }

    // Remove duplicates and empty values
    const uniqueTags = [...new Set(tagArray.map(t => Utils.trimText(t)))].filter(t => t.length > 0);

    // Check count
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

    // Validate each tag
    for (const tag of uniqueTags) {
      // Check length
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

      // Check pattern
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
    const errors = [];
    const validated = {};

    // Validate title (required)
    const titleResult = validateTitle(taskData.title);
    if (!titleResult.valid) {
      errors.push(titleResult.error);
    } else {
      validated.title = titleResult.value;
    }

    // Validate description (optional)
    const descResult = validateDescription(taskData.description);
    if (!descResult.valid) {
      errors.push(descResult.error);
    } else {
      validated.description = descResult.value;
    }

    // Validate status
    const statusResult = validateStatus(taskData.status);
    if (!statusResult.valid) {
      errors.push(statusResult.error);
    } else {
      validated.status = statusResult.value;
    }

    // Validate priority
    const priorityResult = validatePriority(taskData.priority);
    if (!priorityResult.valid) {
      errors.push(priorityResult.error);
    } else {
      validated.priority = priorityResult.value;
    }

    // Validate due date
    const dueDateResult = validateDueDate(taskData.dueDate, options);
    if (!dueDateResult.valid) {
      errors.push(dueDateResult.error);
    } else {
      validated.dueDate = dueDateResult.value;
    }

    // Validate tags
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

    // Tags are validated differently in filters (any tag is acceptable)
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

    // Add command requires title
    if (command === 'add') {
      if (!args.title || Utils.trimText(args.title).length === 0) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Add command requires a title',
          'title'
        ));
      }
    }

    // Update command requires ID
    if (command === 'update') {
      if (!args.id || !validateId(args.id)) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Update command requires a valid task ID',
          'id'
        ));
      }
    }

    // Delete command requires ID
    if (command === 'delete') {
      if (!args.id || !validateId(args.id)) {
        errors.push(new ValidationError(
          'COMMAND_ARGS_INVALID',
          'Delete command requires a valid task ID',
          'id'
        ));
      }
    }

    // Search command requires query
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

  // Public API
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

// Export for Node.js/CommonJS environments
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Validator;
}
