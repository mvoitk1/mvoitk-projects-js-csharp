// @ts-nocheck
import TaskManager from './taskManager';

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

  // What these next lines do:
  // Safety check: adapter must expose all methods App expects.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Safety check: adapter must expose all methods App expects.
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

  // What these next lines do:
  // Wrap old TaskManager API so UI can call a stable adapter interface.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Wrap old TaskManager API so UI can call a stable adapter interface.
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

  // What these next lines do:
  // Lazy default: if app did not set a custom adapter, use TaskManager.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Lazy default: if app did not set a custom adapter, use TaskManager.
  function ensureDefaultAdapter() {
    if (activeAdapter) return;

    activeAdapter = createLegacyAdapter(TaskManager);
  }

  // What these next lines do:
  // Allow swapping in a different backend adapter later.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Allow swapping in a different backend adapter later.
  function setAdapter(adapter) {
    activeAdapter = validateAdapter(adapter);
  }

  // What these next lines do:
  // Always return a usable adapter or fail with clear error.
  // Why this matters in this project:
  // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Always return a usable adapter or fail with clear error.
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

export default TaskDataAdapter;
