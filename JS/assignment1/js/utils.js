/**
 * Utility functions for the Task Management Application
 * Contains UUID generation, date handling, and helper functions
 */

const Utils = (function() {
  'use strict';

  /**
   * Makes a unique ID string for tasks.
   * Uses browser crypto when possible, otherwise a fallback generator.
   * @returns {string} UUID v4 format
   */
  function generateUUID() {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    
    // Fallback implementation for browsers without crypto.randomUUID
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  /**
   * Returns the current date and time in standard text format.
   * @returns {string} ISO 8601 timestamp
   */
  function getCurrentTimestamp() {
    return new Date().toISOString();
  }

  /**
   * Converts a date string like 2026-03-05 into a Date object.
   * Returns null if the text is missing or invalid.
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
    
    // Check if date is valid
    if (date.getFullYear() !== year || 
        date.getMonth() !== month || 
        date.getDate() !== day) {
      return null;
    }
    
    return date;
  }

  /**
   * Converts a Date object back into YYYY-MM-DD text.
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
   * Turns a timestamp into a readable date/time string for users.
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
   * Checks if the given date happened before today.
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
   * Checks if the given date is today.
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
   * Checks if the given date is after today.
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
   * Calculates how many days are left until a date.
   * Negative number means it is overdue.
   * @param {string} dateString - Date string in YYYY-MM-DD format
   * @returns {number|null} Number of days (negative if overdue) or null if invalid
   */
  function getDaysUntil(dateString) {
    const date = parseDate(dateString);
    if (!date) return null;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    date.setHours(0, 0, 0, 0);
    
    const diffTime = date.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    return diffDays;
  }

  /**
   * Escapes HTML special characters so user text is safer to render.
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
   * Removes HTML tags from text and keeps only plain content.
   * @param {string} text - Text with potential HTML tags
   * @returns {string} Text without HTML tags
   */
  function stripHtmlTags(text) {
    if (typeof text !== 'string') return '';
    return text.replace(/<[^>]*>/g, '');
  }

  /**
   * Cleans text by trimming spaces and safely handling null/undefined.
   * @param {string|null|undefined} text - Text to trim
   * @returns {string} Trimmed text or empty string
   */
  function trimText(text) {
    if (text === null || text === undefined) return '';
    return String(text).trim();
  }

  /**
   * Splits a tag string into an array of tags.
   * Works with commas and spaces.
   * @param {string} tagsString - Comma or space-separated tags
   * @returns {string[]} Array of trimmed tags
   */
  function parseTags(tagsString) {
    if (!tagsString) return [];
    
    // Split by comma or whitespace
    const tags = tagsString.split(/[,\s]+/);
    
    // Filter empty and trim
    return tags
      .map(tag => trimText(tag))
      .filter(tag => tag.length > 0);
  }

  /**
   * Creates a deep copy of an object so edits do not affect the original.
   * @param {any} obj - Object to clone
   * @returns {any} Cloned object
   */
  function deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  /**
   * Converts priority text into a number for sorting.
   * Bigger number means higher priority.
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
   * Converts status text into a number for sorting.
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

  // Public API
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

// Export for Node.js/CommonJS environments
if (typeof module !== 'undefined' && module.exports) {
  module.exports = Utils;
}
