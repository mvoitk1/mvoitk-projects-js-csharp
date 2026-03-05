/**
 * TaskManager - Core task management logic
 * Implements CRUD operations and task queries
 */

const TaskManager = (function() {
  'use strict';

  /**
   * TaskManager class for managing tasks
   */
  class TaskManagerClass {
    constructor() {
      this.tasks = [];
      this.persistentFilter = null;
      this.initialized = false;
    }

    /**
     * Initialize the task manager and load tasks from storage
     * @returns {Promise<void>}
     */
    async init() {
      if (this.initialized) return;
      
      try {
        this.tasks = await Storage.getAllTasks();
        this.initialized = true;
      } catch (error) {
        console.error('Failed to initialize TaskManager:', error);
        this.tasks = [];
        this.initialized = true;
      }
    }

    /**
     * Create a new task
     * @param {Object} taskData - Task data (partial)
     * @param {Object} options - Validation options
     * @returns {Promise<Object>} Created task
     * @throws {ValidationError} If validation fails
     */
    async createTask(taskData, options = {}) {
      // Validate task data
      const validation = Validator.validateTask(taskData, options);
      if (!validation.valid) {
        throw validation.errors[0];
      }

      const timestamp = Utils.getCurrentTimestamp();
      
      const task = {
        id: Utils.generateUUID(),
        title: validation.data.title,
        description: validation.data.description || '',
        status: validation.data.status,
        priority: validation.data.priority,
        dueDate: validation.data.dueDate,
        tags: validation.data.tags || [],
        checklist: this.normalizeChecklist(taskData.checklist),
        createdAt: timestamp,
        updatedAt: timestamp
      };

      try {
        await Storage.addTask(task);
        this.tasks.push(task);
        return task;
      } catch (error) {
        throw error;
      }
    }

    /**
     * Get a task by ID
     * @param {string} id - Task ID
     * @returns {Promise<Object|null>} Task or null
     */
    async getTask(id) {
      // Try to find in memory first
      let task = this.tasks.find(t => t.id === id);
      
      if (!task) {
        // Try storage
        try {
          task = await Storage.getTask(id);
        } catch (error) {
          return null;
        }
      }
      
      return task;
    }

    /**
     * Get all tasks
     * @returns {Promise<Object[]>} All tasks sorted by createdAt descending
     */
    async getAllTasks() {
      // Ensure tasks are loaded
      await this.init();
      
      // Sort by createdAt descending (newest first)
      return [...this.tasks].sort((a, b) => {
        return new Date(b.createdAt) - new Date(a.createdAt);
      });
    }

    /**
     * Find tasks matching query
     * @param {Object} query - Query object with filters
     * @returns {Promise<Object[]>} Matching tasks
     */
    async findTasks(query = {}) {
      await this.init();
      
      let results = [...this.tasks];
      
      // Filter by status
      if (query.status) {
        const status = query.status.toLowerCase();
        results = results.filter(task => task.status === status);
      }
      
      // Filter by priority
      if (query.priority) {
        const priority = query.priority.toLowerCase();
        results = results.filter(task => task.priority === priority);
      }
      
      // Filter by tags (any match)
      if (query.tags && query.tags.length > 0) {
        const tags = Array.isArray(query.tags) ? query.tags : Utils.parseTags(query.tags);
        const normalizedTags = tags.map(t => t.toLowerCase());
        results = results.filter(task => 
          task.tags && task.tags.some(tag => normalizedTags.includes(tag.toLowerCase()))
        );
      }
      
      // Filter by due date range
      if (query.dueBefore) {
        results = results.filter(task => {
          if (!task.dueDate) return false;
          return task.dueDate <= query.dueBefore;
        });
      }
      
      if (query.dueAfter) {
        results = results.filter(task => {
          if (!task.dueDate) return false;
          return task.dueDate >= query.dueAfter;
        });
      }
      
      // Filter by exact due date
      if (query.dueDate) {
        results = results.filter(task => task.dueDate === query.dueDate);
      }
      
      // Sort by createdAt descending
      results.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
      
      return results;
    }

    /**
     * Update a task
     * @param {string} id - Task ID
     * @param {Object} updates - Fields to update
     * @param {Object} options - Validation options
     * @returns {Promise<Object>} Updated task
     * @throws {ValidationError} If task not found or validation fails
     */
    async updateTask(id, updates, options = {}) {
      // Check if task exists
      const task = this.tasks.find(t => t.id === id);
      if (!task) {
        throw new Validator.ValidationError(
          'TASK_NOT_FOUND',
          `Task with ID '${id}' not found`,
          'id'
        );
      }

      // Merge existing task with updates for validation
      const mergedData = {
        title: updates.title !== undefined ? updates.title : task.title,
        description: updates.description !== undefined ? updates.description : task.description,
        status: updates.status !== undefined ? updates.status : task.status,
        priority: updates.priority !== undefined ? updates.priority : task.priority,
        dueDate: updates.dueDate !== undefined ? updates.dueDate : task.dueDate,
        tags: updates.tags !== undefined ? updates.tags : task.tags
      };

      // Validate merged data
      const validation = Validator.validateTask(mergedData, options);
      if (!validation.valid) {
        throw validation.errors[0];
      }

      const updatedData = {
        ...updates,
        checklist: updates.checklist !== undefined
          ? this.normalizeChecklist(updates.checklist)
          : task.checklist,
        updatedAt: Utils.getCurrentTimestamp()
      };

      // Handle status transition - update timestamp if status changed
      if (updates.status && updates.status !== task.status) {
        updatedData.updatedAt = Utils.getCurrentTimestamp();
      }

      try {
        const updatedTask = await Storage.updateTask(id, updatedData);
        
        // Update in-memory task
        const index = this.tasks.findIndex(t => t.id === id);
        if (index !== -1) {
          this.tasks[index] = { ...task, ...updatedTask };
        }
        
        return updatedTask;
      } catch (error) {
        if (error.code === 'TASK_NOT_FOUND') {
          throw new Validator.ValidationError(
            'TASK_NOT_FOUND',
            `Task with ID '${id}' not found`,
            'id'
          );
        }
        throw error;
      }
    }

    /**
     * Normalize checklist payload into a stable stored shape.
     * @param {Array} checklist - Checklist data
     * @returns {Array<{id: string, text: string, completed: boolean}>}
     */
    normalizeChecklist(checklist) {
      if (!Array.isArray(checklist)) {
        return [];
      }

      return checklist
        .filter(item => item && typeof item.text === 'string' && item.text.trim().length > 0)
        .map(item => ({
          id: item.id || Utils.generateUUID(),
          text: item.text.trim(),
          completed: Boolean(item.completed)
        }));
    }

    /**
     * Delete a task
     * @param {string} id - Task ID
     * @returns {Promise<boolean>} True if deleted
     * @throws {ValidationError} If task not found
     */
    async deleteTask(id) {
      // Check if task exists
      const task = this.tasks.find(t => t.id === id);
      if (!task) {
        throw new Validator.ValidationError(
          'TASK_NOT_FOUND',
          `Task with ID '${id}' not found`,
          'id'
        );
      }

      try {
        await Storage.deleteTask(id);
        
        // Remove from in-memory array
        const index = this.tasks.findIndex(t => t.id === id);
        if (index !== -1) {
          this.tasks.splice(index, 1);
        }
        
        return true;
      } catch (error) {
        if (error.code === 'TASK_NOT_FOUND') {
          throw new Validator.ValidationError(
            'TASK_NOT_FOUND',
            `Task with ID '${id}' not found`,
            'id'
          );
        }
        throw error;
      }
    }

    /**
     * Search tasks by query string
     * @param {string} searchQuery - Search query
     * @param {Object} options - Search options
     * @returns {Promise<Object[]>} Matching tasks
     */
    async searchTasks(searchQuery, options = {}) {
      await this.init();
      
      const { fields = ['title', 'description', 'tags'], caseSensitive = false } = options;
      
      if (!searchQuery || searchQuery.trim() === '') {
        return [];
      }

      const query = caseSensitive ? searchQuery : searchQuery.toLowerCase();
      
      let results = this.tasks.filter(task => {
        // Search in title
        if (fields.includes('title')) {
          const title = caseSensitive ? task.title : task.title.toLowerCase();
          if (title.includes(query)) return true;
        }
        
        // Search in description
        if (fields.includes('description') && task.description) {
          const desc = caseSensitive ? task.description : task.description.toLowerCase();
          if (desc.includes(query)) return true;
        }
        
        // Search in tags
        if (fields.includes('tags') && task.tags) {
          const tags = caseSensitive ? task.tags : task.tags.map(t => t.toLowerCase());
          if (tags.some(tag => tag.includes(query))) return true;
        }
        
        return false;
      });
      
      // Sort by relevance (exact matches first, then by createdAt)
      results.sort((a, b) => {
        const aExact = a.title.toLowerCase() === query;
        const bExact = b.title.toLowerCase() === query;
        if (aExact && !bExact) return -1;
        if (!aExact && bExact) return 1;
        return new Date(b.createdAt) - new Date(a.createdAt);
      });
      
      return results;
    }

    /**
     * Set persistent filter
     * @param {Object} filter - Filter object
     */
    setFilter(filter) {
      this.persistentFilter = filter;
    }

    /**
     * Get persistent filter
     * @returns {Object|null} Current filter
     */
    getFilter() {
      return this.persistentFilter;
    }

    /**
     * Clear persistent filter
     */
    clearFilter() {
      this.persistentFilter = null;
    }

    /**
     * Get tasks with persistent filter applied
     * @returns {Promise<Object[]>} Filtered tasks
     */
    async getFilteredTasks() {
      if (this.persistentFilter) {
        return await this.findTasks(this.persistentFilter);
      }
      return await this.getAllTasks();
    }

    /**
     * Get task count
     * @returns {number} Number of tasks
     */
    getTaskCount() {
      return this.tasks.length;
    }

    /**
     * Get tasks grouped by status
     * @returns {Object} Tasks grouped by status
     */
    async getTasksByStatus() {
      await this.init();
      
      const grouped = {
        'pending': [],
        'in-progress': [],
        'completed': [],
        'cancelled': []
      };
      
      this.tasks.forEach(task => {
        if (grouped[task.status]) {
          grouped[task.status].push(task);
        }
      });
      
      return grouped;
    }

    /**
     * Get tasks grouped by priority
     * @returns {Object} Tasks grouped by priority
     */
    async getTasksByPriority() {
      await this.init();
      
      const grouped = {
        'urgent': [],
        'high': [],
        'medium': [],
        'low': []
      };
      
      this.tasks.forEach(task => {
        if (grouped[task.priority]) {
          grouped[task.priority].push(task);
        }
      });
      
      return grouped;
    }

    /**
     * Get overdue tasks
     * @returns {Promise<Object[]>} Overdue tasks
     */
    async getOverdueTasks() {
      await this.init();
      
      const today = Utils.formatDate(new Date());
      
      return this.tasks.filter(task => {
        if (!task.dueDate) return false;
        if (task.status === 'completed' || task.status === 'cancelled') return false;
        return task.dueDate < today;
      });
    }

    /**
     * Get tasks due today
     * @returns {Promise<Object[]>} Tasks due today
     */
    async getTasksDueToday() {
      await this.init();
      
      const today = Utils.formatDate(new Date());
      
      return this.tasks.filter(task => {
        return task.dueDate === today;
      });
    }

    /**
     * Export all tasks
     * @returns {Promise<string>} JSON string of tasks
     */
    async exportTasks() {
      return await Storage.exportData();
    }

    /**
     * Import tasks from JSON
     * @param {string} jsonString - JSON string
     * @returns {Promise<number>} Number of imported tasks
     */
    async importTasks(jsonString) {
      const beforeCount = this.tasks.length;
      await Storage.importData(jsonString);
      this.tasks = await Storage.getAllTasks();
      return this.tasks.length - beforeCount;
    }

    /**
     * Clear all tasks
     * @returns {Promise<void>}
     */
    async clearAllTasks() {
      await Storage.clearAll();
      this.tasks = [];
    }

    /**
     * Reload tasks from storage
     * @returns {Promise<void>}
     */
    async reload() {
      this.tasks = await Storage.getAllTasks();
    }
  }

  // Return singleton instance
  return new TaskManagerClass();
})();

// Export for Node.js/CommonJS environments
if (typeof module !== 'undefined' && module.exports) {
  module.exports = TaskManager;
}
