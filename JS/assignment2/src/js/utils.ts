// @ts-nocheck
/**
 * Utility functions for the Task Management Application
 * Contains UUID generation, date handling, and helper functions
 */

const Utils = (function() {
  'use strict';

  /**
   * Generate a UUID v4 compliant string
   * @returns {string} UUID v4 format
   */
  function generateUUID() {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    
    // What these next lines do:
    // Fallback for older browsers without crypto.randomUUID.
    // Why this matters in this project:
    // Returning this value here defines the output contract of the helper and keeps callers predictable.
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  /**
   * Get current ISO 8601 timestamp
   * @returns {string} ISO 8601 timestamp
   */
  function getCurrentTimestamp() {
    return new Date().toISOString();
  }

  /**
   * Parse a date string in YYYY-MM-DD format
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {Date|null} Date object or null if invalid
   */
  function parseDate(dateString) {
    if (!dateString || typeof dateString !== 'string') {
      return null;
    }
    
    const dateRegex = /^(\d{4})-(\d{2})-(\d{2})$/;
    const match = dateString.match(dateRegex);
    
    if (!match) {
      return null;
    }
    
    const year = parseInt(match[1], 10);
    const month = parseInt(match[2], 10) - 1; // Months are 0-indexed
    const day = parseInt(match[3], 10);
    
    const date = new Date(year, month, day);
    
    // What these next lines do:
    // Reject impossible dates (for example 2026-02-31).
    // Why this matters in this project:
    // Standardizing date handling avoids subtle bugs when comparing or displaying time values.
    if (date.getFullYear() !== year || 
        date.getMonth() !== month || 
        date.getDate() !== day) {
      return null;
    }
    
    return date;
  }

  /**
   * Format a Date object to YYYY-MM-DD string
   * @param {Date} date - Date object
   * @returns {string} Formatted date string
   */
  function formatDate(date) {
    if (!(date instanceof Date) || isNaN(date.getTime())) {
      return '';
    }
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    
    return `${year}-${month}-${day}`;
  }

  /**
   * Format a timestamp to human-readable string
   * @param {string} isoTimestamp - ISO 8601 timestamp
   * @returns {string} Formatted timestamp
   */
  function formatTimestamp(isoTimestamp) {
    if (!isoTimestamp) return '';
    
    const date = new Date(isoTimestamp);
    if (isNaN(date.getTime())) return '';
    
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Check if a date is in the past
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {boolean} True if date is in the past
   */
  function isDateInPast(dateString) {
    const date = parseDate(dateString);
    if (!date) return false;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    date.setHours(0, 0, 0, 0);
    
    return date < today;
  }

  /**
   * Check if a date is today
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {boolean} True if date is today
   */
  function isDateToday(dateString) {
    const date = parseDate(dateString);
    if (!date) return false;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    date.setHours(0, 0, 0, 0);
    
    return date.getTime() === today.getTime();
  }

  /**
   * Check if a date is in the future
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {boolean} True if date is in the future
   */
  function isDateInFuture(dateString) {
    const date = parseDate(dateString);
    if (!date) return false;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    date.setHours(0, 0, 0, 0);
    
    return date > today;
  }

  /**
   * Get days until a due date
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {number|null} Number of days (negative if overdue) or null if invalid
   */
  function getDaysUntil(dateString) {
    const date = parseDate(dateString);
    if (!date) return null;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    date.setHours(0, 0, 0, 0);
    
    // What these next lines do:
    // Positive = future, zero = today, negative = overdue.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const diffTime = date.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    return diffDays;
  }

  /**
   * Escape HTML special characters
   * @param {string} text - Text to escape
   * @returns {string} Escaped text
   */
  function escapeHtml(text) {
    if (typeof text !== 'string') return '';
    
    const htmlEscapes = {
      '&': '&',
      '<': '<',
      '>': '>',
      '"': '"',
      "'": '&#x27;',
      '/': '&#x2F;'
    };
    
    return text.replace(/[&<>"'\/]/g, char => htmlEscapes[char]);
  }

  /**
   * Strip HTML tags from text
   * @param {string} text - Text with potential HTML tags
   * @returns {string} Text without HTML tags
   */
  function stripHtmlTags(text) {
    if (typeof text !== 'string') return '';
    return text.replace(/<[^>]*>/g, '');
  }

  /**
   * Trim whitespace from string and handle null/undefined
   * @param {string|null|undefined} text - Text to trim
   * @returns {string} Trimmed text or empty string
   */
  function trimText(text) {
    if (text === null || text === undefined) return '';
    return String(text).trim();
  }

  /**
   * Parse comma or space-separated tags
   * @param {string} tagsString - Comma or space-separated tags
   * @returns {string[]} Array of trimmed tags
   */
  function parseTags(tagsString) {
    if (!tagsString) return [];
    
    // What these next lines do:
    // Accept comma- or space-separated tag input.
    // Why this matters in this project:
    // Keeping this value/function in a `const` prevents accidental reassignment and makes behavior more predictable.
    const tags = tagsString.split(/[,\s]+/);
    
    // What these next lines do:
    // Filter empty and trim
    // Why this matters in this project:
    // Returning this value here defines the output contract of the helper and keeps callers predictable.
    return tags
      .map(tag => trimText(tag))
      .filter(tag => tag.length > 0);
  }

  /**
   * Deep clone an object
   * @param {any} obj - Object to clone
   * @returns {any} Cloned object
   */
  function deepClone(obj) {
    // What these next lines do:
    // Simple deep clone for plain JSON-compatible objects.
    // Why this matters in this project:
    // Returning this value here defines the output contract of the helper and keeps callers predictable.
    return JSON.parse(JSON.stringify(obj));
  }

  /**
   * Get priority weight for sorting
   * @param {string} priority - Priority level
   * @returns {number} Weight value
   */
  function getPriorityWeight(priority) {
    const weights = {
      'urgent': 4,
      'high': 3,
      'medium': 2,
      'low': 1
    };
    return weights[priority] || 0;
  }

  /**
   * Get status weight for sorting
   * @param {string} status - Status value
   * @returns {number} Weight value
   */
  function getStatusWeight(status) {
    const weights = {
      'in-progress': 3,
      'pending': 2,
      'cancelled': 1,
      'completed': 0
    };
    return weights[status] || 0;
  }

  // What these next lines do:
  // Public API
  // Why this matters in this project:
  // Returning this value here defines the output contract of the helper and keeps callers predictable.
  return {
    generateUUID,
    getCurrentTimestamp,
    parseDate,
    formatDate,
    formatTimestamp,
    isDateInPast,
    isDateToday,
    isDateInFuture,
    getDaysUntil,
    escapeHtml,
    stripHtmlTags,
    trimText,
    parseTags,
    deepClone,
    getPriorityWeight,
    getStatusWeight
  };
})();

export default Utils;
