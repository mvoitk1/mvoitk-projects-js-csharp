https://mvoitk-js1.proxy.itcollege.ee/

# Task Manager

A modern, feature-rich task management application built with vanilla JavaScript. Manage your tasks efficiently with a beautiful dark-themed interface, multiple views, and a powerful command palette.

## Features

### Task Management
- **Create Tasks** - Add tasks with title, description, priority, due dates, and tags
- **Edit Tasks** - Modify any task property inline or via modal
- **Delete Tasks** - Remove tasks with confirmation
- **Status Tracking** - Track task progress: pending, in-progress, completed, cancelled
- **Priority Levels** - Set priority: low, medium, high, urgent
- **Tags System** - Organize tasks with customizable tags

### Views
- **Dashboard** - Overview with statistics and quick actions
- **Task List** - Full list view with filtering, sorting, and search
- **Calendar** - Visual calendar view of tasks by due date
- **Command Palette** - Quick command input for fast task management

### Additional Features
- **Search** - Full-text search across all tasks
- **Filtering** - Filter by status, priority, and tags
- **Sorting** - Sort by date, priority, or title
- **Data Persistence** - All data stored locally in browser localStorage
- **Data Validation** - Input validation for all task fields

## Getting Started

### Prerequisites
- Modern web browser (Chrome, Firefox, Safari, Edge)
- No server required - runs entirely in the browser

### Installation

1. Clone the repository:
```bash
git clone https://gitlab.proxy.itcollege.ee/2025-2026-spring/mvoitk-projects_js_csharp.git
```

2. Navigate to the project directory:
```bash
cd JS/assignment1
```

3. Open `index.html` in your browser:
```bash
# Option 1: Open directly
open index.html

# Option 2: Use a local server
npx serve .
```

## Usage

### Creating a Task
1. Click the "Add Task" button in the sidebar
2. Fill in the task details (title is required)
3. Optionally set description, priority, due date, and tags
4. Click "Create" to save

### Using the Command Palette
Press `Ctrl+K` (or `Cmd+K` on Mac) to open the command palette. Available commands:
- `add <title>` - Create a new task
- `list` - Show all tasks
- `search <query>` - Search tasks
- `filter <status>` - Filter by status
- `clear` - Clear filters

### Managing Tasks
- Click on a task to view details
- Use the quick actions (buttons) to change status
- Use the edit icon to modify task properties
- Use the delete icon to remove tasks

## Project Structure

```
assignment1/
├── index.html          # Main HTML file
├── README.md           # This file
├── js/
│   ├── app.js          # Main application logic
│   ├── commands.js    # Command palette functionality
│   ├── formatters.js  # Data formatting utilities
│   ├── storage.js     # localStorage persistence
│   ├── taskManager.js # Core task management
│   ├── utils.js       # Utility functions
│   └── validator.js   # Input validation
└── context/
    ├── specs.md       # Technical specifications
    ├── implementation_plan.md
    └── memory_bank.md
```

## Technology Stack

- **HTML5** - Semantic HTML structure
- **CSS3** - Modern CSS with variables and flexbox
- **JavaScript (ES6+)** - Vanilla JavaScript, no frameworks
- **localStorage** - Browser-based data persistence

## Browser Support

- Chrome 80+
- Firefox 75+
- Safari 13+
- Edge 80+

## License

This project is for educational purposes.
