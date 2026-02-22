/**
 * Storage module for Task Management Application
 * Provides localStorage abstraction layer with error handling
 */

const Storage = (function() {
  'use strict';

  const TASKS_KEY = 'tasks';
  const METADATA_KEY = 'app_metadata';
  const APP_VERSION = '1.0.0';

  /**
   * Custom error class for storage operations
   */
  class StorageError extends Error {
    constructor(code, message, details = null) {
      super(message);
      this.code = code;
      this.name = 'StorageError';
      this.details = details;
    }
  }

  /**
   * Check if localStorage is available
   * @returns {boolean} True if localStorage is available
   */
  function isAvailable() {
    try {
      const testKey = '__storage_test__';
      localStorage.setItem(testKey, testKey);
      localStorage.removeItem(testKey);
      return true;
    } catch (e) {
      return false;
    }
  }

  /**
   * Get all tasks from storage
   * @returns {Promise<Array>} Array of tasks
   * @throws {StorageError} If storage access fails
   */
  async function getAllTasks() {
    try {
      if (!isAvailable()) {
        throw new StorageError('STORAGE_ERROR', 'localStorage is not available');
      }

      const data = localStorage.getItem(TASKS_KEY);
      
      if (data === null) {
        return [];
      }

      const tasks = JSON.parse(data);
      
      if (!Array.isArray(tasks)) {
        // Data corruption - reset to empty array
        console.warn('Data corruption detected: tasks is not an array');
        await saveTasks([]);
        return [];
      }

      // Validate each task has required fields
      const validTasks = tasks.filter(task => task && task.id && task.title);
      
      if (validTasks.length !== tasks.length) {
        console.warn('Some invalid tasks were filtered out');
        await saveTasks(validTasks);
      }

      return validTasks;
    } catch (error) {
      if (error instanceof StorageError) {
        throw error;
      }
      
      if (error.name === 'QuotaExceededError') {
        throw new StorageError(
          'STORAGE_QUOTA_EXCEEDED',
          'Storage quota exceeded. Please delete some tasks'
        );
      }
      
      throw new StorageError(
        'STORAGE_ERROR',
        `Failed to access local storage: ${error.message}`,
        error
      );
    }
  }

  /**
   * Save tasks to storage
   * @param {Array} tasks - Array of tasks to save
   * @returns {Promise<boolean>} True if successful
   * @throws {StorageError} If storage fails
   */
  async function saveTasks(tasks) {
    try {
      if (!isAvailable()) {
        throw new StorageError('STORAGE_ERROR', 'localStorage is not available');
      }

      if (!Array.isArray(tasks)) {
        throw new StorageError('STORAGE_ERROR', 'Tasks must be an array');
      }

      const data = JSON.stringify(tasks);
      localStorage.setItem(TASKS_KEY, data);

      // Update metadata
      await updateMetadata({ taskCount: tasks.length });

      return true;
    } catch (error) {
      if (error instanceof StorageError) {
        throw error;
      }
      
      if (error.name === 'QuotaExceededError') {
        throw new StorageError(
          'STORAGE_QUOTA_EXCEEDED',
          'Storage quota exceeded. Please delete some tasks'
        );
      }
      
      throw new StorageError(
        'STORAGE_ERROR',
        `Failed to save tasks: ${error.message}`,
        error
      );
    }
  }

  /**
   * Get a single task by ID
   * @param {string} id - Task ID
   * @returns {Promise<Object|null>} Task object or null
   */
  async function getTask(id) {
    const tasks = await getAllTasks();
    return tasks.find(task => task.id === id) || null;
  }

  /**
   * Add a new task
   * @param {Object} task - Task object to add
   * @returns {Promise<Object>} Added task
   */
  async function addTask(task) {
    const tasks = await getAllTasks();
    tasks.push(task);
    await saveTasks(tasks);
    return task;
  }

  /**
   * Update an existing task
   * @param {string} id - Task ID
   * @param {Object} updates - Fields to update
   * @returns {Promise<Object>} Updated task
   * @throws {StorageError} If task not found
   */
  async function updateTask(id, updates) {
    const tasks = await getAllTasks();
    const index = tasks.findIndex(task => task.id === id);
    
    if (index === -1) {
      throw new StorageError(
        'TASK_NOT_FOUND',
        `Task with ID '${id}' not found`
      );
    }

    tasks[index] = { ...tasks[index], ...updates };
    await saveTasks(tasks);
    return tasks[index];
  }

  /**
   * Delete a task
   * @param {string} id - Task ID
   * @returns {Promise<boolean>} True if deleted
   */
  async function deleteTask(id) {
    const tasks = await getAllTasks();
    const index = tasks.findIndex(task => task.id === id);
    
    if (index === -1) {
      throw new StorageError(
        'TASK_NOT_FOUND',
        `Task with ID '${id}' not found`
      );
    }

    tasks.splice(index, 1);
    await saveTasks(tasks);
    return true;
  }

  /**
   * Get app metadata
   * @returns {Promise<Object>} Metadata object
   */
  async function getMetadata() {
    try {
      const data = localStorage.getItem(METADATA_KEY);
      
      if (data === null) {
        const defaultMetadata = {
          version: APP_VERSION,
          lastBackup: null,
          taskCount: 0
        };
        await saveMetadata(defaultMetadata);
        return defaultMetadata;
      }

      return JSON.parse(data);
    } catch (error) {
      // Return default on error
      return {
        version: APP_VERSION,
        lastBackup: null,
        taskCount: 0
      };
    }
  }

  /**
   * Save app metadata
   * @param {Object} metadata - Metadata to save
   * @returns {Promise<boolean>} True if successful
   */
  async function saveMetadata(metadata) {
    try {
      localStorage.setItem(METADATA_KEY, JSON.stringify(metadata));
      return true;
    } catch (error) {
      console.error('Failed to save metadata:', error);
      return false;
    }
  }

  /**
   * Update metadata fields
   * @param {Object} updates - Fields to update
   * @returns {Promise<Object>} Updated metadata
   */
  async function updateMetadata(updates) {
    const metadata = await getMetadata();
    const updated = { ...metadata, ...updates };
    await saveMetadata(updated);
    return updated;
  }

  /**
   * Export all data as JSON string
   * @returns {Promise<string>} JSON string of all data
   */
  async function exportData() {
    const tasks = await getAllTasks();
    const metadata = await getMetadata();
    
    return JSON.stringify({
      tasks,
      metadata,
      exportedAt: new Date().toISOString()
    }, null, 2);
  }

  /**
   * Import data from JSON string
   * @param {string} jsonString - JSON string to import
   * @returns {Promise<boolean>} True if successful
   * @throws {StorageError} If import fails
   */
  async function importData(jsonString) {
    try {
      const data = JSON.parse(jsonString);
      
      if (!data.tasks || !Array.isArray(data.tasks)) {
        throw new StorageError(
          'DATA_CORRUPTION',
          'Invalid import data: tasks array not found'
        );
      }

      // Validate each task
      const validTasks = data.tasks.filter(task => 
        task && typeof task.id === 'string' && typeof task.title === 'string'
      );

      if (validTasks.length === 0 && data.tasks.length > 0) {
        throw new StorageError(
          'DATA_CORRUPTION',
          'No valid tasks found in import data'
        );
      }

      await saveTasks(validTasks);
      return true;
    } catch (error) {
      if (error instanceof StorageError) {
        throw error;
      }
      throw new StorageError(
        'DATA_CORRUPTION',
        `Failed to import data: ${error.message}`
      );
    }
  }

  /**
   * Clear all tasks (for testing/reset)
   * @returns {Promise<boolean>} True if successful
   */
  async function clearAll() {
    return await saveTasks([]);
  }

  /**
   * Get storage usage information
   * @returns {Promise<Object>} Storage info
   */
  async function getStorageInfo() {
    try {
      const tasks = await getAllTasks();
      const data = JSON.stringify(tasks);
      const sizeBytes = new Blob([data]).size;
      
      return {
        taskCount: tasks.length,
        sizeBytes,
        sizeKB: (sizeBytes / 1024).toFixed(2),
        available: isAvailable()
      };
    } catch (error) {
      return {
        taskCount: 0,
        sizeBytes: 0,
        sizeKB: '0',
        available: isAvailable()
      };
    }
  }

  // Public API
  return {
    StorageError,
    isAvailable,
    getAllTasks,
    saveTasks,
    getTask,
    addTask,
    updateTask,
    deleteTask,
    getMetadata,
    saveMetadata,
    updateMetadata,
    exportData,
    importData,
    clearAll,
    getStorageInfo,
    TASKS_KEY,
    METADATA_KEY,
    APP_VERSION
  };
})();

// Export for Node.js/CommonJS environments
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Storage;
}
