/**
 * GUI Application - Task Management Application
 * Handles navigation, views, and user interactions
 */

const App = (function() {
  'use strict';

  // State
  let currentView = 'dashboard';
  let currentFilter = null;
  let currentStatusFilter = 'all';
  let currentSearchQuery = '';
  let currentCalendarDate = new Date();
  let taskTags = [];

  // DOM Elements
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
    
    // Initialize TaskManager
    await TaskManager.init();
    
    // Load initial data
    await refreshAll();
  }

  /**
   * Cache DOM elements
   */
  function cacheElements() {
    elements.headerTitle = document.getElementById('header-title');
    elements.searchInput = document.getElementById('search-input');
    elements.totalBadge = document.getElementById('total-badge');
    
    // Stats
    elements.statTotal = document.getElementById('stat-total');
    elements.statPending = document.getElementById('stat-pending');
    elements.statProgress = document.getElementById('stat-progress');
    elements.statCompleted = document.getElementById('stat-completed');
    
    // Views
    elements.dashboardView = document.getElementById('dashboard-view');
    elements.tasksView = document.getElementById('tasks-view');
    elements.calendarView = document.getElementById('calendar-view');
    
    // Task containers
    elements.recentTasks = document.getElementById('recent-tasks');
    elements.upcomingTasks = document.getElementById('upcoming-tasks');
    elements.allTasks = document.getElementById('all-tasks');
    
    // Modal
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
    elements.saveTaskBtn = document.getElementById('save-task-btn');
    elements.deleteTaskBtn = document.getElementById('delete-task-btn');
    elements.cancelBtn = document.getElementById('cancel-btn');
    elements.modalClose = document.getElementById('modal-close');
    elements.addTaskBtn = document.getElementById('add-task-btn');
    
    // Calendar
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
    // Navigation
    document.querySelectorAll('.nav-item[data-view]').forEach(item => {
      item.addEventListener('click', () => switchView(item.dataset.view));
    });

    // View all link
    document.querySelectorAll('.section-link[data-view]').forEach(item => {
      item.addEventListener('click', () => switchView(item.dataset.view));
    });

    // Task filters
    document.querySelectorAll('.filter-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        currentStatusFilter = btn.dataset.status;
        updateFilterButtons();
        loadTasks();
      });
    });

    // Search
    elements.searchInput.addEventListener('input', debounce((e) => {
      currentSearchQuery = e.target.value;
      loadTasks();
    }, 300));

    // Add task button
    elements.addTaskBtn.addEventListener('click', () => openTaskModal());

    // Modal events
    elements.modalClose.addEventListener('click', closeTaskModal);
    elements.cancelBtn.addEventListener('click', closeTaskModal);
    elements.saveTaskBtn.addEventListener('click', saveTask);
    elements.deleteTaskBtn.addEventListener('click', deleteTask);
    
    elements.modal.addEventListener('click', (e) => {
      if (e.target === elements.modal) {
        closeTaskModal();
      }
    });

    // Tags input
    elements.tagsInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        addTag(elements.tagsInput.value);
      } else if (e.key === 'Backspace' && elements.tagsInput.value === '' && taskTags.length > 0) {
        taskTags.pop();
        renderTags();
      }
    });

    // Calendar navigation
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

    // Keyboard shortcuts
    document.addEventListener('keydown', handleKeyboard);
  }

  /**
   * Handle keyboard shortcuts
   */
  function handleKeyboard(e) {
    // Escape to close modal
    if (e.key === 'Escape' && elements.modal.classList.contains('active')) {
      closeTaskModal();
    }
    
    // Ctrl/Cmd + N for new task
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
      e.preventDefault();
      openTaskModal();
    }
    
    // Ctrl/Cmd + F for search
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
    
    // Update nav items
    document.querySelectorAll('.nav-item').forEach(item => {
      item.classList.remove('active');
      if (item.dataset.view === viewName) {
        item.classList.add('active');
      }
    });

    // Update views
    document.querySelectorAll('.view').forEach(view => {
      view.classList.remove('active');
    });

    const viewElement = document.getElementById(`${viewName}-view`);
    if (viewElement) {
      viewElement.classList.add('active');
    }

    // Update header title
    const titles = {
      dashboard: 'Dashboard',
      tasks: 'All Tasks',
      calendar: 'Calendar'
    };
    elements.headerTitle.textContent = titles[viewName] || 'Dashboard';

    // Load view data
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
    const tasks = await TaskManager.getAllTasks();
    const byStatus = {
      pending: tasks.filter(t => t.status === 'pending').length,
      'in-progress': tasks.filter(t => t.status === 'in-progress').length,
      completed: tasks.filter(t => t.status === 'completed').length
    };

    if (elements.totalBadge) elements.totalBadge.textContent = tasks.length;

    elements.statTotal.textContent = tasks.length;
    elements.statPending.textContent = byStatus.pending;
    elements.statProgress.textContent = byStatus['in-progress'];
    elements.statCompleted.textContent = byStatus.completed;
  }

  /**
   * Load dashboard data
   */
  async function loadDashboard() {
    const tasks = await TaskManager.getAllTasks();
    
    // Recent tasks (last 5)
    const recent = tasks.slice(0, 5);
    elements.recentTasks.innerHTML = recent.length > 0 
      ? recent.map(task => renderTaskCard(task)).join('')
      : renderEmptyState('No tasks yet', 'Create your first task to get started');

    // Upcoming deadlines (next 7 days, not completed)
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

    // Attach click handlers
    attachTaskCardHandlers(elements.recentTasks);
    attachTaskCardHandlers(elements.upcomingTasks);
  }

  /**
   * Load tasks with filters
   */
  async function loadTasks() {
    let tasks = await TaskManager.getAllTasks();
    
    // Apply status filter
    if (currentStatusFilter !== 'all') {
      tasks = tasks.filter(t => t.status === currentStatusFilter);
    }
    
    // Apply search filter
    if (currentSearchQuery) {
      const query = currentSearchQuery.toLowerCase();
      tasks = tasks.filter(t => 
        t.title.toLowerCase().includes(query) ||
        (t.description && t.description.toLowerCase().includes(query)) ||
        (t.tags && t.tags.some(tag => tag.toLowerCase().includes(query)))
      );
    }

    elements.allTasks.innerHTML = tasks.length > 0
      ? tasks.map(task => renderTaskCard(task)).join('')
      : renderEmptyState('No tasks found', currentSearchQuery
          ? 'Try adjusting your search query'
          : 'Create your first task to get started');

    attachTaskCardHandlers(elements.allTasks);
  }

  /**
   * Render task card HTML
   */
  function renderTaskCard(task) {
    const dueDateClass = getDueDateClass(task.dueDate, task.status);
    const daysUntil = task.dueDate ? Utils.getDaysUntil(task.dueDate) : null;
    
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
        <div class="task-content" data-action="edit" data-id="${task.id}">
          <div class="task-title">${Utils.escapeHtml(task.title)}</div>
          ${task.description ? `<div class="task-description">${Utils.escapeHtml(task.description)}</div>` : ''}
          <div class="task-meta">
            <span class="status-badge ${task.status}">${formatStatus(task.status)}</span>
            <span class="priority-badge ${task.priority}">${task.priority}</span>
            ${task.dueDate ? `<span class="task-due-date ${dueDateClass}">📅 ${dueDateText}</span>` : ''}
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
    container.querySelectorAll('.task-card').forEach(card => {
      const id = card.dataset.id;
      
      card.querySelector('[data-action="toggle"]').addEventListener('click', (e) => {
        e.stopPropagation();
        toggleTaskStatus(id);
      });
      
      card.querySelector('[data-action="edit"]').addEventListener('click', (e) => {
        e.stopPropagation();
        editTask(id);
      });
      
      card.querySelector('[data-action="delete"]').addEventListener('click', (e) => {
        e.stopPropagation();
        confirmDeleteTask(id);
      });
    });
  }

  /**
   * Toggle task status
   */
  async function toggleTaskStatus(id) {
    const task = await TaskManager.getTask(id);
    if (!task) return;

    const newStatus = task.status === 'completed' ? 'pending' : 'completed';
    
    try {
      await TaskManager.updateTask(id, { status: newStatus });
      await refreshAll();
    } catch (error) {
      alert('Failed to update task: ' + error.message);
    }
  }

  /**
   * Edit task
   */
  async function editTask(id) {
    const task = await TaskManager.getTask(id);
    if (!task) return;

    openTaskModal(task);
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
      await TaskManager.deleteTask(id);
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
  function openTaskModal(task = null) {
    taskTags = [];
    
    if (task) {
      // Edit mode
      elements.modalTitle.textContent = 'Edit Task';
      elements.taskId.value = task.id;
      elements.taskTitle.value = task.title;
      elements.taskDescription.value = task.description || '';
      elements.taskStatus.value = task.status;
      elements.taskPriority.value = task.priority;
      elements.taskDueDate.value = task.dueDate || '';
      elements.deleteTaskBtn.style.display = 'block';
      taskTags = [...(task.tags || [])];
    } else {
      // Create mode
      elements.modalTitle.textContent = 'New Task';
      elements.taskForm.reset();
      elements.taskId.value = '';
      elements.deleteTaskBtn.style.display = 'none';
    }

    renderTags();
    elements.modal.classList.add('active');
    elements.taskTitle.focus();
  }

  /**
   * Close task modal
   */
  function closeTaskModal() {
    elements.modal.classList.remove('active');
    taskTags = [];
  }

  /**
   * Add tag
   */
  function addTag(tag) {
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
    taskTags.splice(index, 1);
    renderTags();
  }

  /**
   * Render tags
   */
  function renderTags() {
    const tagElements = taskTags.map((tag, index) => `
      <span class="tag-item">
        ${Utils.escapeHtml(tag)}
        <span class="tag-remove" data-index="${index}">×</span>
      </span>
    `).join('');

    elements.tagsContainer.innerHTML = tagElements + `
      <input type="text" class="tags-input" id="tags-input" placeholder="${taskTags.length === 0 ? 'Add tags (press Enter)' : ''}">
    `;

    // Re-attach events
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

    // Tag remove buttons
    elements.tagsContainer.querySelectorAll('.tag-remove').forEach(btn => {
      btn.addEventListener('click', () => removeTag(parseInt(btn.dataset.index)));
    });
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
      tags: taskTags
    };

    try {
      if (id) {
        // Update existing
        await TaskManager.updateTask(id, taskData);
      } else {
        // Create new
        await TaskManager.createTask(taskData);
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
        await TaskManager.deleteTask(id);
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
    
    // Update month display
    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'];
    elements.calendarMonth.textContent = `${monthNames[month]} ${year}`;

    // Get tasks
    const tasks = await TaskManager.getAllTasks();
    const tasksByDate = {};
    tasks.forEach(task => {
      if (task.dueDate) {
        if (!tasksByDate[task.dueDate]) {
          tasksByDate[task.dueDate] = [];
        }
        tasksByDate[task.dueDate].push(task);
      }
    });

    // Generate calendar grid
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

    // Attach click handlers for calendar tasks
    elements.calendarGrid.querySelectorAll('.calendar-task-dot[data-id]').forEach(dot => {
      dot.addEventListener('click', (e) => {
        e.stopPropagation();
        editTask(dot.dataset.id);
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

  // Public API
  return {
    init,
    openTaskModal,
    switchView
  };
})();

// Auto-initialize
App.init();
