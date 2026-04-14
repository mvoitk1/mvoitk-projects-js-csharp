// @ts-nocheck
import TaskDataAdapter from './dataAdapter';
import Utils from './utils';

/**
 * GUI Application - Task Management Application
 * Handles navigation, views, and user interactions
 */

const App = (function() {
  'use strict';

  // What these next lines do:
  // UI state shared across views.
  // Why this matters in this project:
  // This state coordinates filters/search/calendar across screens so user actions stay consistent.
  let currentView = 'dashboard';
  let currentFilter = null;
  let currentStatusFilter = 'all';
  let currentSearchQuery = '';
  let currentCalendarDate = new Date();
  let taskTags = [];
  let taskChecklist = [];
  let modalMode = 'create';
  let dataAdapter = null;

  // What these next lines do:
  // Cached DOM nodes so we do not query repeatedly.
  // Why this matters in this project:
  // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
  const elements = {};

  /**
   * Initialize the application
   */
  async function init() {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', onDOMReady);
    } else {
      await onDOMReady();
    }
  }

  /**
   * Handle DOM ready event
   */
  async function onDOMReady() {
    cacheElements();
    setupEventListeners();
    
    // What these next lines do:
    // Data adapter is the bridge between UI and storage/business logic.
    // Why this matters in this project:
    // Being explicit about storage behavior prevents corruption and makes recovery paths clearer.
    dataAdapter = TaskDataAdapter.getAdapter();
    await dataAdapter.init();
    
    // What these next lines do:
    // Load initial data
    // Why this matters in this project:
    // Running this at startup ensures the UI shows real saved tasks instead of an empty placeholder screen.
    await refreshAll();
  }

  /**
   * Cache DOM elements
   */
  function cacheElements() {
    elements.headerTitle = document.getElementById('header-title');
    elements.searchInput = document.getElementById('search-input');
    elements.totalBadge = document.getElementById('total-badge');
    elements.pendingBadge = document.getElementById('pending-badge');
    elements.progressBadge = document.getElementById('progress-badge');
    elements.completedBadge = document.getElementById('completed-badge');
    
    // What these next lines do:
    // Stats
    // Why this matters in this project:
    // These element references are used to update count badges quickly whenever task data changes.
    elements.statTotal = document.getElementById('stat-total');
    elements.statPending = document.getElementById('stat-pending');
    elements.statProgress = document.getElementById('stat-progress');
    elements.statCompleted = document.getElementById('stat-completed');
    
    // What these next lines do:
    // Views
    // Why this matters in this project:
    // Keeping view references cached makes section switching fast and avoids repeated DOM lookups.
    elements.dashboardView = document.getElementById('dashboard-view');
    elements.tasksView = document.getElementById('tasks-view');
    elements.calendarView = document.getElementById('calendar-view');
    
    // What these next lines do:
    // Task containers
    // Why this matters in this project:
    // These containers are where rendered task cards are injected, so list updates appear immediately.
    elements.recentTasks = document.getElementById('recent-tasks');
    elements.upcomingTasks = document.getElementById('upcoming-tasks');
    elements.allTasks = document.getElementById('all-tasks');
    
    // What these next lines do:
    // Modal
    // Why this matters in this project:
    // These references control create/edit/view dialogs, which are core to task CRUD actions.
    elements.modal = document.getElementById('task-modal');
    elements.modalTitle = document.getElementById('modal-title');
    elements.taskForm = document.getElementById('task-form');
    elements.taskId = document.getElementById('task-id');
    elements.taskTitle = document.getElementById('task-title');
    elements.taskDescription = document.getElementById('task-description');
    elements.taskStatus = document.getElementById('task-status');
    elements.taskPriority = document.getElementById('task-priority');
    elements.taskDueDate = document.getElementById('task-due-date');
    elements.tagsContainer = document.getElementById('tags-container');
    elements.tagsInput = document.getElementById('tags-input');
    elements.checklistControls = document.querySelector('.checklist-controls');
    elements.checklistItems = document.getElementById('checklist-items');
    elements.checklistInput = document.getElementById('checklist-input');
    elements.addChecklistBtn = document.getElementById('add-checklist-btn');
    elements.saveTaskBtn = document.getElementById('save-task-btn');
    elements.deleteTaskBtn = document.getElementById('delete-task-btn');
    elements.cancelBtn = document.getElementById('cancel-btn');
    elements.modalClose = document.getElementById('modal-close');
    elements.addTaskBtn = document.getElementById('add-task-btn');
    
    // What these next lines do:
    // Calendar
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Calendar.
    elements.calendarMonth = document.getElementById('calendar-month');
    elements.calendarGrid = document.getElementById('calendar-grid');
    elements.prevMonth = document.getElementById('prev-month');
    elements.nextMonth = document.getElementById('next-month');
    elements.todayBtn = document.getElementById('today-btn');
  }

  /**
   * Set up event listeners
   */
  function setupEventListeners() {
    // What these next lines do:
    // Navigation
    // Why this matters in this project:
    // Navigation wiring determines which view is visible and keeps sidebar/header state in sync.
    document.querySelectorAll('.nav-item[data-view]').forEach(item => {
      item.addEventListener('click', () => switchView(item.dataset.view));
    });

    // What these next lines do:
    // Quick filters in sidebar
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    document.querySelectorAll('.filter-nav').forEach(item => {
      item.addEventListener('click', () => {
        currentFilter = item.dataset.filter;
        currentStatusFilter = item.dataset.filter;
        switchView('tasks');
        updateFilterButtons();
      });
    });

    // What these next lines do:
    // View all link
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: View all link.
    document.querySelectorAll('.section-link[data-view]').forEach(item => {
      item.addEventListener('click', () => switchView(item.dataset.view));
    });

    // What these next lines do:
    // Task filters
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    document.querySelectorAll('.filter-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        currentStatusFilter = btn.dataset.status;
        updateFilterButtons();
        loadTasks();
      });
    });

    // What these next lines do:
    // Search
    // Why this matters in this project:
    // Search behavior directly affects discoverability, so this logic must be predictable.
    elements.searchInput.addEventListener('input', debounce((e) => {
      currentSearchQuery = e.target.value;
      loadTasks();
    }, 300));

    // What these next lines do:
    // Add task button
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Add task button.
    elements.addTaskBtn.addEventListener('click', () => openTaskModal());

    // What these next lines do:
    // Modal events
    // Why this matters in this project:
    // Clear event handling keeps user interactions predictable and avoids duplicate side effects.
    elements.modalClose.addEventListener('click', closeTaskModal);
    elements.cancelBtn.addEventListener('click', closeTaskModal);
    elements.saveTaskBtn.addEventListener('click', saveTask);
    elements.deleteTaskBtn.addEventListener('click', deleteTask);
    
    elements.modal.addEventListener('click', (e) => {
      if (e.target === elements.modal) {
        closeTaskModal();
      }
    });

    // What these next lines do:
    // Tags input
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Tags input.
    elements.tagsInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        addTag(elements.tagsInput.value);
      } else if (e.key === 'Backspace' && elements.tagsInput.value === '' && taskTags.length > 0) {
        taskTags.pop();
        renderTags();
      }
    });

    // What these next lines do:
    // Checklist input
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Checklist input.
    elements.checklistInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        addChecklistItem(elements.checklistInput.value);
      }
    });

    elements.addChecklistBtn.addEventListener('click', () => {
      addChecklistItem(elements.checklistInput.value);
    });

    // What these next lines do:
    // Calendar navigation
    // Why this matters in this project:
    // Navigation wiring determines which view is visible and keeps sidebar/header state in sync.
    elements.prevMonth.addEventListener('click', () => {
      currentCalendarDate.setMonth(currentCalendarDate.getMonth() - 1);
      renderCalendar();
    });

    elements.nextMonth.addEventListener('click', () => {
      currentCalendarDate.setMonth(currentCalendarDate.getMonth() + 1);
      renderCalendar();
    });

    elements.todayBtn.addEventListener('click', () => {
      currentCalendarDate = new Date();
      renderCalendar();
    });

    // What these next lines do:
    // Keyboard shortcuts
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Keyboard shortcuts.
    document.addEventListener('keydown', handleKeyboard);
  }

  /**
   * Handle keyboard shortcuts
   */
  function handleKeyboard(e) {
    // What these next lines do:
    // Escape to close modal
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Escape to close modal.
    if (e.key === 'Escape' && elements.modal.classList.contains('active')) {
      closeTaskModal();
    }
    
    // What these next lines do:
    // Ctrl/Cmd + N for new task
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Ctrl/Cmd + N for new task.
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
      e.preventDefault();
      openTaskModal();
    }
    
    // What these next lines do:
    // Ctrl/Cmd + F for search
    // Why this matters in this project:
    // Search behavior directly affects discoverability, so this logic must be predictable.
    if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
      e.preventDefault();
      elements.searchInput.focus();
    }
  }

  /**
   * Switch between views
   */
  function switchView(viewName) {
    currentView = viewName;
    
    // What these next lines do:
    // Update nav items
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    document.querySelectorAll('.nav-item').forEach(item => {
      item.classList.remove('active');
      if (item.dataset.view === viewName || 
          (item.dataset.filter === currentFilter && viewName === 'tasks')) {
        item.classList.add('active');
      }
    });

    // What these next lines do:
    // Update views
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    document.querySelectorAll('.view').forEach(view => {
      view.classList.remove('active');
    });

    const viewElement = document.getElementById(`${viewName}-view`);
    if (viewElement) {
      viewElement.classList.add('active');
    }

    // What these next lines do:
    // Update header title
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const titles = {
      dashboard: 'Dashboard',
      tasks: 'All Tasks',
      calendar: 'Calendar'
    };
    elements.headerTitle.textContent = titles[viewName] || 'Dashboard';

    // What these next lines do:
    // Load view data
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Load view data.
    loadViewData(viewName);
  }

  /**
   * Load data for the current view
   */
  async function loadViewData(viewName) {
    switch (viewName) {
      case 'dashboard':
        await loadDashboard();
        break;
      case 'tasks':
        await loadTasks();
        break;
      case 'calendar':
        await renderCalendar();
        break;
    }
  }

  /**
   * Refresh all data
   */
  async function refreshAll() {
    await loadDashboard();
    await loadTasks();
    updateBadges();
  }

  /**
   * Update badges
   */
  async function updateBadges() {
    const tasks = await dataAdapter.getAllTasks();
    const byStatus = {
      pending: tasks.filter(t => t.status === 'pending').length,
      'in-progress': tasks.filter(t => t.status === 'in-progress').length,
      completed: tasks.filter(t => t.status === 'completed').length
    };

    if (elements.totalBadge) elements.totalBadge.textContent = tasks.length;
    if (elements.pendingBadge) elements.pendingBadge.textContent = byStatus.pending;
    if (elements.progressBadge) elements.progressBadge.textContent = byStatus['in-progress'];
    if (elements.completedBadge) elements.completedBadge.textContent = byStatus.completed;

    elements.statTotal.textContent = tasks.length;
    elements.statPending.textContent = byStatus.pending;
    elements.statProgress.textContent = byStatus['in-progress'];
    elements.statCompleted.textContent = byStatus.completed;
  }

  /**
   * Load dashboard data
   */
  async function loadDashboard() {
    const tasks = await dataAdapter.getAllTasks();
    
    // What these next lines do:
    // Recent tasks (last 5)
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const recent = tasks.slice(0, 5);
    elements.recentTasks.innerHTML = recent.length > 0 
      ? recent.map(task => renderTaskCard(task)).join('')
      : renderEmptyState('No tasks yet', 'Create your first task to get started');

    // What these next lines do:
    // Upcoming deadlines (next 7 days, not completed)
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const today = Utils.formatDate(new Date());
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    const nextWeekStr = Utils.formatDate(nextWeek);
    
    const upcoming = tasks
      .filter(t => t.dueDate && t.dueDate >= today && t.dueDate <= nextWeekStr && t.status !== 'completed' && t.status !== 'cancelled')
      .sort((a, b) => a.dueDate.localeCompare(b.dueDate))
      .slice(0, 5);

    elements.upcomingTasks.innerHTML = upcoming.length > 0
      ? upcoming.map(task => renderTaskCard(task)).join('')
      : renderEmptyState('No upcoming deadlines', 'Tasks due in the next 7 days will appear here');

    // What these next lines do:
    // Attach click handlers
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Attach click handlers.
    attachTaskCardHandlers(elements.recentTasks);
    attachTaskCardHandlers(elements.upcomingTasks);
  }

  /**
   * Load tasks with filters
   */
  async function loadTasks() {
    let tasks = await dataAdapter.getAllTasks();
    
    // What these next lines do:
    // Apply status filter
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    if (currentStatusFilter !== 'all') {
      tasks = tasks.filter(t => t.status === currentStatusFilter);
    }
    
    // What these next lines do:
    // Apply search filter
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    if (currentSearchQuery) {
      const query = currentSearchQuery.toLowerCase();
      tasks = tasks.filter(t => 
        t.title.toLowerCase().includes(query) ||
        (t.description && t.description.toLowerCase().includes(query)) ||
        (t.tags && t.tags.some(tag => tag.toLowerCase().includes(query)))
      );
    }

    // What these next lines do:
    // Apply persistent filter (from sidebar quick filters).
    // Why this matters in this project:
    // Applying this filter here ensures users only see tasks matching the chosen criteria.
    if (currentFilter) {
      tasks = tasks.filter(t => t.status === currentFilter);
    }

    elements.allTasks.innerHTML = tasks.length > 0
      ? tasks.map(task => renderTaskCard(task)).join('')
      : renderEmptyState('No tasks found', currentFilter || currentSearchQuery 
          ? 'Try adjusting your filters or search query' 
          : 'Create your first task to get started');

    attachTaskCardHandlers(elements.allTasks);
  }

  /**
   * Render task card HTML
   */
  function renderTaskCard(task) {
    // What these next lines do:
    // Helper values drive "overdue/today" text and checklist badge.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const dueDateClass = getDueDateClass(task.dueDate, task.status);
    const daysUntil = task.dueDate ? Utils.getDaysUntil(task.dueDate) : null;
    const checklistProgress = getChecklistProgress(task);
    
    let dueDateText = '';
    if (task.dueDate) {
      if (daysUntil < 0) dueDateText = `${Math.abs(daysUntil)} days overdue`;
      else if (daysUntil === 0) dueDateText = 'Due today';
      else if (daysUntil === 1) dueDateText = 'Due tomorrow';
      else dueDateText = task.dueDate;
    }

    return `
      <div class="task-card priority-${task.priority} ${task.status === 'completed' ? 'completed' : ''}" data-id="${task.id}">
        <div class="task-checkbox ${task.status === 'completed' ? 'checked' : ''}" data-action="toggle" data-id="${task.id}"></div>
        <div class="task-content" data-action="view" data-id="${task.id}">
          <div class="task-title">${Utils.escapeHtml(task.title)}</div>
          ${task.description ? `<div class="task-description">${Utils.escapeHtml(task.description)}</div>` : ''}
          <div class="task-meta">
            <span class="status-badge ${task.status}">${formatStatus(task.status)}</span>
            <span class="priority-badge ${task.priority}">${task.priority}</span>
            ${task.dueDate ? `<span class="task-due-date ${dueDateClass}">📅 ${dueDateText}</span>` : ''}
            ${checklistProgress ? `<span class="checklist-badge">☑ ${checklistProgress}</span>` : ''}
            ${task.tags && task.tags.length > 0 ? task.tags.slice(0, 2).map(tag => `<span class="task-tag">${Utils.escapeHtml(tag)}</span>`).join('') : ''}
          </div>
        </div>
        <div class="task-actions">
          <button class="task-action-btn" data-action="edit" data-id="${task.id}" title="Edit">✏️</button>
          <button class="task-action-btn delete" data-action="delete" data-id="${task.id}" title="Delete">🗑️</button>
        </div>
      </div>
    `;
  }

  /**
   * Get due date CSS class
   */
  function getDueDateClass(dueDate, status) {
    if (!dueDate || status === 'completed' || status === 'cancelled') return '';
    
    const daysUntil = Utils.getDaysUntil(dueDate);
    if (daysUntil < 0) return 'overdue';
    if (daysUntil === 0) return 'today';
    return '';
  }

  /**
   * Format status for display
   */
  function formatStatus(status) {
    const labels = {
      'pending': 'Pending',
      'in-progress': 'In Progress',
      'completed': 'Completed',
      'cancelled': 'Cancelled'
    };
    return labels[status] || status;
  }

  /**
   * Render empty state
   */
  function renderEmptyState(title, text) {
    return `
      <div class="empty-state">
        <div class="empty-state-icon">📋</div>
        <div class="empty-state-title">${title}</div>
        <div class="empty-state-text">${text}</div>
        <button class="btn btn-primary" onclick="App.openTaskModal()">Create Task</button>
      </div>
    `;
  }

  /**
   * Attach event handlers to task cards
   */
  function attachTaskCardHandlers(container) {
    // What these next lines do:
    // Bind handlers for each action icon on each visible card.
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Bind handlers for each action icon on each visible card.
    container.querySelectorAll('.task-card').forEach(card => {
      const id = card.dataset.id;

      card.querySelectorAll('[data-action="toggle"]').forEach(el => {
        el.addEventListener('click', (e) => {
          e.stopPropagation();
          toggleTaskStatus(id);
        });
      });

      card.querySelectorAll('[data-action="view"]').forEach(el => {
        el.addEventListener('click', (e) => {
          e.stopPropagation();
          viewTask(id);
        });
      });

      card.querySelectorAll('[data-action="edit"]').forEach(el => {
        el.addEventListener('click', (e) => {
          e.stopPropagation();
          editTask(id);
        });
      });

      card.querySelectorAll('[data-action="delete"]').forEach(el => {
        el.addEventListener('click', (e) => {
          e.stopPropagation();
          confirmDeleteTask(id);
        });
      });
    });
  }

  /**
   * Toggle task status
   */
  async function toggleTaskStatus(id) {
    const task = await dataAdapter.getTask(id);
    if (!task) return;

    const newStatus = task.status === 'completed' ? 'pending' : 'completed';
    
    try {
      await dataAdapter.updateTask(id, { status: newStatus });
      await refreshAll();
    } catch (error) {
      alert('Failed to update task: ' + error.message);
    }
  }

  /**
   * Edit task
   */
  async function viewTask(id) {
    const task = await dataAdapter.getTask(id);
    if (!task) return;
    openTaskModal(task, 'view');
  }

  /**
   * Edit task
   */
  async function editTask(id) {
    const task = await dataAdapter.getTask(id);
    if (!task) return;

    openTaskModal(task, 'edit');
  }

  /**
   * Confirm delete task
   */
  function confirmDeleteTask(id) {
    if (confirm('Are you sure you want to delete this task?')) {
      deleteTaskById(id);
    }
  }

  /**
   * Delete task by ID
   */
  async function deleteTaskById(id) {
    try {
      await dataAdapter.deleteTask(id);
      await refreshAll();
    } catch (error) {
      alert('Failed to delete task: ' + error.message);
    }
  }

  /**
   * Update filter buttons
   */
  function updateFilterButtons() {
    document.querySelectorAll('.filter-btn').forEach(btn => {
      btn.classList.remove('active');
      if (btn.dataset.status === currentStatusFilter) {
        btn.classList.add('active');
      }
    });
  }

  /**
   * Open task modal
   */
  function openTaskModal(task = null, mode = 'create') {
    // What these next lines do:
    // Reset temporary modal data each time modal opens.
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Reset temporary modal data each time modal opens.
    taskTags = [];
    taskChecklist = [];
    modalMode = mode;

    if (task && mode === 'view') {
      // What these next lines do:
      // View mode
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: View mode.
      elements.modalTitle.textContent = 'Task Details';
      elements.taskId.value = task.id;
      elements.taskTitle.value = task.title;
      elements.taskDescription.value = task.description || '';
      elements.taskStatus.value = task.status;
      elements.taskPriority.value = task.priority;
      elements.taskDueDate.value = task.dueDate || '';
      taskTags = [...(task.tags || [])];
      taskChecklist = normalizeChecklist(task.checklist);
    } else if (task) {
      // What these next lines do:
      // Edit mode
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Edit mode.
      elements.modalTitle.textContent = 'Edit Task';
      elements.taskId.value = task.id;
      elements.taskTitle.value = task.title;
      elements.taskDescription.value = task.description || '';
      elements.taskStatus.value = task.status;
      elements.taskPriority.value = task.priority;
      elements.taskDueDate.value = task.dueDate || '';
      elements.deleteTaskBtn.style.display = 'block';
      taskTags = [...(task.tags || [])];
      taskChecklist = normalizeChecklist(task.checklist);
    } else {
      // What these next lines do:
      // Create mode
      // Why this matters in this project:
      // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Create mode.
      elements.modalTitle.textContent = 'New Task';
      elements.taskForm.reset();
      elements.taskId.value = '';
      elements.deleteTaskBtn.style.display = 'none';
    }

    applyModalMode();
    renderTags();
    renderChecklist();
    elements.modal.classList.add('active');
    elements.taskTitle.focus();
  }

  function applyModalMode() {
    // What these next lines do:
    // View mode = read-only; edit/create mode = interactive.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const isView = modalMode === 'view';
    const hasTask = !!elements.taskId.value;

    elements.taskTitle.readOnly = isView;
    elements.taskDescription.readOnly = isView;
    elements.taskStatus.disabled = isView;
    elements.taskPriority.disabled = isView;
    elements.taskDueDate.disabled = isView;

    if (elements.checklistControls) {
      elements.checklistControls.style.display = isView ? 'none' : 'grid';
    }

    elements.saveTaskBtn.style.display = isView ? 'none' : 'inline-block';
    elements.deleteTaskBtn.style.display = !isView && hasTask ? 'block' : 'none';
  }

  /**
   * Close task modal
   */
  function closeTaskModal() {
    elements.modal.classList.remove('active');
    taskTags = [];
    taskChecklist = [];
    modalMode = 'create';
  }

  /**
   * Add tag
   */
  function addTag(tag) {
    if (modalMode === 'view') return;

    const trimmed = tag.trim().toLowerCase();
    if (trimmed && !taskTags.includes(trimmed) && taskTags.length < 10) {
      taskTags.push(trimmed);
      elements.tagsInput.value = '';
      renderTags();
    }
  }

  /**
   * Remove tag
   */
  function removeTag(index) {
    if (modalMode === 'view') return;
    taskTags.splice(index, 1);
    renderTags();
  }

  /**
   * Render tags
   */
  function renderTags() {
    // What these next lines do:
    // View mode shows static tags; edit mode shows removable chips + input.
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: View mode shows static tags; edit mode shows removable chips + input.
    if (modalMode === 'view') {
      elements.tagsContainer.innerHTML = taskTags.length > 0
        ? taskTags.map(tag => `<span class="tag-item">${Utils.escapeHtml(tag)}</span>`).join('')
        : `<div class="checklist-empty">No tags.</div>`;
      return;
    }

    const tagElements = taskTags.map((tag, index) => `
      <span class="tag-item">
        ${Utils.escapeHtml(tag)}
        <span class="tag-remove" data-index="${index}">×</span>
      </span>
    `).join('');

    elements.tagsContainer.innerHTML = tagElements + `
      <input type="text" class="tags-input" id="tags-input" placeholder="${taskTags.length === 0 ? 'Add tags (press Enter)' : ''}">
    `;

    // What these next lines do:
    // Re-attach events
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const newTagsInput = document.getElementById('tags-input');
    newTagsInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        addTag(newTagsInput.value);
      } else if (e.key === 'Backspace' && newTagsInput.value === '' && taskTags.length > 0) {
        taskTags.pop();
        renderTags();
      }
    });

    // What these next lines do:
    // Tag remove buttons
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Tag remove buttons.
    elements.tagsContainer.querySelectorAll('.tag-remove').forEach(btn => {
      btn.addEventListener('click', () => removeTag(parseInt(btn.dataset.index, 10)));
    });
  }

  /**
   * Normalize checklist into stable UI shape.
   */
  function normalizeChecklist(checklist) {
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
   * Add checklist item.
   */
  function addChecklistItem(text) {
    if (modalMode === 'view') return;

    const trimmed = text.trim();
    if (!trimmed) return;

    taskChecklist.push({
      id: Utils.generateUUID(),
      text: trimmed,
      completed: false
    });

    elements.checklistInput.value = '';
    renderChecklist();
  }

  /**
   * Toggle checklist item completion.
   */
  async function toggleChecklistItem(index) {
    const item = taskChecklist[index];
    if (!item) return;
    item.completed = !item.completed;

    if (modalMode === 'view' && elements.taskId.value) {
      try {
        await dataAdapter.updateTask(elements.taskId.value, { checklist: taskChecklist });
        await refreshAll();
      } catch (error) {
        alert('Failed to update checklist: ' + error.message);
      }
    }

    renderChecklist();
  }

  /**
   * Remove checklist item.
   */
  function removeChecklistItem(index) {
    if (modalMode === 'view') return;
    taskChecklist.splice(index, 1);
    renderChecklist();
  }

  /**
   * Render checklist section in modal.
   */
  function renderChecklist() {
    if (!elements.checklistItems) return;

    if (taskChecklist.length === 0) {
      elements.checklistItems.innerHTML = `<div class="checklist-empty">No checklist items yet.</div>`;
      return;
    }

    elements.checklistItems.innerHTML = taskChecklist.map((item, index) => `
      <div class="checklist-item ${item.completed ? 'completed' : ''}">
        <label>
          <input type="checkbox" data-check-action="toggle" data-index="${index}" ${item.completed ? 'checked' : ''} />
          <span>${Utils.escapeHtml(item.text)}</span>
        </label>
        ${modalMode === 'view' ? '' : `<button type="button" class="checklist-remove" data-check-action="remove" data-index="${index}" aria-label="Remove checklist item">×</button>`}
      </div>
    `).join('');

    elements.checklistItems.querySelectorAll('[data-check-action="toggle"]').forEach(el => {
      el.addEventListener('change', async () => {
        await toggleChecklistItem(parseInt(el.dataset.index, 10));
      });
    });

    elements.checklistItems.querySelectorAll('[data-check-action="remove"]').forEach(el => {
      el.addEventListener('click', () => {
        removeChecklistItem(parseInt(el.dataset.index, 10));
      });
    });
  }

  /**
   * Get checklist completion summary for task card.
   */
  function getChecklistProgress(task) {
    if (!Array.isArray(task.checklist) || task.checklist.length === 0) {
      return '';
    }

    const total = task.checklist.length;
    const done = task.checklist.filter(item => item.completed).length;
    return `${done}/${total}`;
  }

  /**
   * Save task
   */
  async function saveTask() {
    const title = elements.taskTitle.value.trim();
    const description = elements.taskDescription.value.trim();
    const status = elements.taskStatus.value;
    const priority = elements.taskPriority.value;
    const dueDate = elements.taskDueDate.value || null;
    const id = elements.taskId.value;

    if (!title) {
      alert('Please enter a task title');
      elements.taskTitle.focus();
      return;
    }

    const taskData = {
      title,
      description,
      status,
      priority,
      dueDate,
      tags: taskTags,
      checklist: taskChecklist
    };

    try {
      if (id) {
        // What these next lines do:
        // Existing ID means edit flow.
        // Why this matters in this project:
        // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Existing ID means edit flow.
        await dataAdapter.updateTask(id, taskData);
      } else {
        // What these next lines do:
        // No ID means create flow.
        // Why this matters in this project:
        // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: No ID means create flow.
        await dataAdapter.createTask(taskData);
      }

      closeTaskModal();
      await refreshAll();
    } catch (error) {
      alert('Failed to save task: ' + error.message);
    }
  }

  /**
   * Delete task from modal
   */
  async function deleteTask() {
    const id = elements.taskId.value;
    if (id && confirm('Are you sure you want to delete this task?')) {
      try {
        await dataAdapter.deleteTask(id);
        closeTaskModal();
        await refreshAll();
      } catch (error) {
        alert('Failed to delete task: ' + error.message);
      }
    }
  }

  /**
   * Render calendar
   */
  async function renderCalendar() {
    const year = currentCalendarDate.getFullYear();
    const month = currentCalendarDate.getMonth();
    
    // What these next lines do:
    // Update month display
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'];
    elements.calendarMonth.textContent = `${monthNames[month]} ${year}`;

    // What these next lines do:
    // Get tasks
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const tasks = await dataAdapter.getAllTasks();
    const tasksByDate = {};
    tasks.forEach(task => {
      if (task.dueDate) {
        if (!tasksByDate[task.dueDate]) {
          tasksByDate[task.dueDate] = [];
        }
        tasksByDate[task.dueDate].push(task);
      }
    });

    // What these next lines do:
    // Build a fixed 6-week (42-cell) grid so month layout stays stable.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());
    
    const today = Utils.formatDate(new Date());
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    
    let html = days.map(d => `<div class="calendar-day-header">${d}</div>`).join('');
    
    const current = new Date(startDate);
    for (let i = 0; i < 42; i++) {
      const dateStr = Utils.formatDate(current);
      const isCurrentMonth = current.getMonth() === month;
      const isToday = dateStr === today;
      const dayTasks = tasksByDate[dateStr] || [];
      
      html += `
        <div class="calendar-day ${!isCurrentMonth ? 'other-month' : ''} ${isToday ? 'today' : ''}" data-date="${dateStr}">
          <div class="calendar-day-number">${current.getDate()}</div>
          <div class="calendar-tasks">
            ${dayTasks.slice(0, 3).map(t => `
              <div class="calendar-task-dot priority-${t.priority}" data-id="${t.id}" title="${Utils.escapeHtml(t.title)}">
                ${Utils.escapeHtml(t.title.substring(0, 15))}${t.title.length > 15 ? '...' : ''}
              </div>
            `).join('')}
            ${dayTasks.length > 3 ? `<div class="calendar-task-dot" style="background: var(--text-muted)">+${dayTasks.length - 3} more</div>` : ''}
          </div>
        </div>
      `;
      
      current.setDate(current.getDate() + 1);
    }

    elements.calendarGrid.innerHTML = html;

    // What these next lines do:
    // Attach click handlers for calendar tasks
    // Why this matters in this project:
    // This step is part of the main data/UI flow, so mistakes here would directly affect user-visible behavior: Attach click handlers for calendar tasks.
    elements.calendarGrid.querySelectorAll('.calendar-task-dot[data-id]').forEach(dot => {
      dot.addEventListener('click', (e) => {
        e.stopPropagation();
        viewTask(dot.dataset.id);
      });
    });
  }

  /**
   * Debounce helper
   */
  function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }

  // What these next lines do:
  // Public API
  // Why this matters in this project:
  // Returning this value here defines the output contract of the helper and keeps callers predictable.
  return {
    init,
    openTaskModal,
    switchView
  };
})();

export default App;
