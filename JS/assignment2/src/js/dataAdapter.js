/**
 * Thin data adapter contract between UI (App) and data layer.
 * Keeps current behavior via TaskManager while allowing DAL swap later.
 */
const TaskDataAdapter = (function() {
  'use strict';

  /**
   * @typedef {Object} ITaskDataAdapter
   * @property {() => Promise<void>} init
   * @property {() => Promise<Object[]>} getAllTasks
   * @property {(id: string) => Promise<Object|null>} getTask
   * @property {(taskData: Object) => Promise<Object>} createTask
   * @property {(id: string, updates: Object) => Promise<Object>} updateTask
   * @property {(id: string) => Promise<boolean|void>} deleteTask
   */

  /** @type {ITaskDataAdapter|null} */
  let activeAdapter = null;

  function validateAdapter(candidate) {
    const required = [
      'init',
      'getAllTasks',
      'getTask',
      'createTask',
      'updateTask',
      'deleteTask'
    ];

    if (!candidate || typeof candidate !== 'object') {
      throw new Error('TaskDataAdapter: adapter must be an object');
    }

    required.forEach(method => {
      if (typeof candidate[method] !== 'function') {
        throw new Error(`TaskDataAdapter: missing method "${method}"`);
      }
    });

    return candidate;
  }

  function createLegacyAdapter(taskManager) {
    validateAdapter(taskManager);
    return {
      init: () => taskManager.init(),
      getAllTasks: () => taskManager.getAllTasks(),
      getTask: (id) => taskManager.getTask(id),
      createTask: (taskData) => taskManager.createTask(taskData),
      updateTask: (id, updates) => taskManager.updateTask(id, updates),
      deleteTask: (id) => taskManager.deleteTask(id)
    };
  }

  function ensureDefaultAdapter() {
    if (activeAdapter) return;

    if (typeof TaskManager !== 'undefined') {
      activeAdapter = createLegacyAdapter(TaskManager);
    }
  }

  function setAdapter(adapter) {
    activeAdapter = validateAdapter(adapter);
  }

  function getAdapter() {
    ensureDefaultAdapter();

    if (!activeAdapter) {
      throw new Error('TaskDataAdapter: no adapter configured');
    }

    return activeAdapter;
  }

  return {
    setAdapter,
    getAdapter,
    createLegacyAdapter
  };
})();
