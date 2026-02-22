# Technical Specifications for Task Management Utility

## Task Object Schema

### Complete Schema Definition
```javascript
interface Task {
  id: string;              // Unique identifier (UUID v4 format)
  title: string;           // Task title (1-100 characters)
  description?: string;    // Optional description (0-1000 characters)
  status: TaskStatus;      // Current task status
  priority: TaskPriority;  // Task priority level
  dueDate?: string;        // Due date in ISO format (YYYY-MM-DD)
  tags: string[];          // Array of tag strings
  createdAt: string;       // Creation timestamp (ISO 8601)
  updatedAt: string;       // Last update timestamp (ISO 8601)
}

type TaskStatus = "pending" | "in-progress" | "completed" | "cancelled";
type TaskPriority = "low" | "medium" | "high" | "urgent";
```

### Field Specifications

#### ID Field
- **Type**: string
- **Format**: UUID v4 (e.g., "550e8400-e29b-41d4-a716-446655440000")
- **Generation**: Auto-generated on task creation
- **Uniqueness**: Guaranteed across all tasks

#### Title Field
- **Type**: string
- **Required**: Yes
- **Length**: 1-100 characters
- **Validation**: Non-empty after trimming whitespace
- **Allowed Characters**: Alphanumeric, spaces, common punctuation

#### Description Field
- **Type**: string
- **Required**: No
- **Length**: 0-1000 characters
- **Validation**: Optional, but if provided, length check
- **Special Handling**: Multi-line support, HTML tag stripping

#### Status Field
- **Type**: enum
- **Values**: "pending", "in-progress", "completed", "cancelled"
- **Default**: "pending"
- **Case Sensitivity**: Input normalized to lowercase

#### Priority Field
- **Type**: enum
- **Values**: "low", "medium", "high", "urgent"
- **Default**: "medium"
- **Case Sensitivity**: Input normalized to lowercase

#### Due Date Field
- **Type**: string (ISO date format)
- **Required**: No
- **Format**: YYYY-MM-DD
- **Validation**: Must be valid date, cannot be in past (with override option)
- **Special Values**: null for no due date

#### Tags Field
- **Type**: string[]
- **Required**: No (empty array allowed)
- **Element Constraints**: 1-20 characters, alphanumeric + hyphens
- **Array Limits**: Maximum 10 tags per task
- **Uniqueness**: Duplicate tags automatically removed

#### Timestamp Fields
- **Type**: string (ISO 8601 timestamp)
- **Format**: "2024-01-15T10:30:00.000Z"
- **Auto-managed**: Set on creation/update, not user-editable

## API Design for CRUD Operations

### Core API Interface
```javascript
class TaskManager {
  // Create
  async createTask(taskData: Partial<Task>): Promise<Task>

  // Read
  async getTask(id: string): Promise<Task | null>
  async getAllTasks(): Promise<Task[]>
  async findTasks(query: TaskQuery): Promise<Task[]>

  // Update
  async updateTask(id: string, updates: Partial<Task>): Promise<Task>

  // Delete
  async deleteTask(id: string): Promise<boolean>
}
```

### Operation Details

#### Create Task
- **Input**: Partial task object (id auto-generated)
- **Validation**: All required fields present and valid
- **Return**: Complete task object with generated fields
- **Error Handling**: ValidationError if invalid data

#### Get Task
- **Input**: Task ID string
- **Return**: Task object or null if not found
- **Error Handling**: StorageError if storage access fails

#### Get All Tasks
- **Input**: None
- **Return**: Array of all tasks
- **Sorting**: By createdAt descending (newest first)
- **Error Handling**: StorageError if storage access fails

#### Find Tasks
- **Input**: Query object with filters
- **Return**: Filtered array of tasks
- **Supported Filters**: status, priority, tags, dueDate range
- **Error Handling**: ValidationError for invalid query

#### Update Task
- **Input**: Task ID and partial updates
- **Validation**: Updates don't violate constraints
- **Return**: Updated complete task object
- **Error Handling**: NotFoundError if task doesn't exist

#### Delete Task
- **Input**: Task ID
- **Return**: Boolean indicating success
- **Error Handling**: NotFoundError if task doesn't exist

## Command Interfaces

### Command Parser Design
- **Input Format**: `command [arguments]`
- **Argument Parsing**: Space-separated, quoted strings for multi-word values
- **Case Sensitivity**: Commands case-insensitive
- **Help System**: `help [command]` for usage information

### Supported Commands

#### Add Command
```
add "Task Title" [options]
```
**Options:**
- `-d, --description "Description text"`
- `-s, --status pending|in-progress|completed|cancelled`
- `-p, --priority low|medium|high|urgent`
- `--due-date YYYY-MM-DD`
- `-t, --tag tag1 tag2 ...`

**Examples:**
```
add "Fix login bug" -p high --due-date 2024-02-01 -t bug frontend
add "Review code" -d "Review pull request #123"
```

#### List Command
```
list [filters]
```
**Filters:**
- `--status pending|in-progress|completed|cancelled`
- `--priority low|medium|high|urgent`
- `--tag tag1 tag2 ...`
- `--due-before YYYY-MM-DD`
- `--due-after YYYY-MM-DD`

**Examples:**
```
list --status pending --priority high
list --tag bug --due-before 2024-02-01
list  # List all tasks
```

#### Update Command
```
update <id> [field value] [options]
```
**Fields:**
- `title "New Title"`
- `description "New Description"`
- `status pending|in-progress|completed|cancelled`
- `priority low|medium|high|urgent`
- `due-date YYYY-MM-DD`
- `tags tag1 tag2 ...` (replaces all tags)

**Examples:**
```
update abc-123 status completed
update def-456 title "Updated task title" priority high
```

#### Delete Command
```
delete <id> [--force]
```
**Options:**
- `--force`: Skip confirmation prompt

**Examples:**
```
delete abc-123
delete def-456 --force
```

#### Filter Command
```
filter [filters]  # Set persistent filter for list command
```
**Same filters as list command**

#### Search Command
```
search <query> [options]
```
**Options:**
- `--fields title|description|tags` (default: all)
- `--case-sensitive`

**Examples:**
```
search "login bug"
search "review" --fields title
```

## Input Validation Rules

### Title Validation
- **Required**: Yes
- **Min Length**: 1 character
- **Max Length**: 100 characters
- **Pattern**: `/^[\w\s\.,!?\-()]+$/` (alphanumeric, spaces, common punctuation)
- **Trimming**: Leading/trailing whitespace removed
- **Error Code**: `VALIDATION_TITLE_REQUIRED`, `VALIDATION_TITLE_LENGTH`, `VALIDATION_TITLE_INVALID`

### Description Validation
- **Required**: No
- **Max Length**: 1000 characters
- **Sanitization**: HTML tags stripped
- **Error Code**: `VALIDATION_DESCRIPTION_LENGTH`

### Status Validation
- **Allowed Values**: ["pending", "in-progress", "completed", "cancelled"]
- **Case Insensitive**: Input normalized to lowercase
- **Error Code**: `VALIDATION_STATUS_INVALID`

### Priority Validation
- **Allowed Values**: ["low", "medium", "high", "urgent"]
- **Case Insensitive**: Input normalized to lowercase
- **Error Code**: `VALIDATION_PRIORITY_INVALID`

### Due Date Validation
- **Format**: YYYY-MM-DD
- **Date Validity**: Must be valid calendar date
- **Future Check**: Warning if date is in past (user can override)
- **Error Code**: `VALIDATION_DUE_DATE_FORMAT`, `VALIDATION_DUE_DATE_INVALID`, `VALIDATION_DUE_DATE_PAST`

### Tags Validation
- **Element Pattern**: `/^[\w\-]+$/` (alphanumeric + hyphens)
- **Element Length**: 1-20 characters
- **Array Size**: 0-10 tags
- **Duplicates**: Automatically removed
- **Error Code**: `VALIDATION_TAG_INVALID`, `VALIDATION_TAG_LENGTH`, `VALIDATION_TAG_COUNT`

## Error Codes and Messages

### Validation Errors
- `VALIDATION_TITLE_REQUIRED`: "Task title is required"
- `VALIDATION_TITLE_LENGTH`: "Task title must be 1-100 characters"
- `VALIDATION_TITLE_INVALID`: "Task title contains invalid characters"
- `VALIDATION_DESCRIPTION_LENGTH`: "Task description must be 0-1000 characters"
- `VALIDATION_STATUS_INVALID`: "Status must be: pending, in-progress, completed, or cancelled"
- `VALIDATION_PRIORITY_INVALID`: "Priority must be: low, medium, high, or urgent"
- `VALIDATION_DUE_DATE_FORMAT`: "Due date must be in YYYY-MM-DD format"
- `VALIDATION_DUE_DATE_INVALID`: "Due date is not a valid date"
- `VALIDATION_DUE_DATE_PAST`: "Due date is in the past. Use --force to override"
- `VALIDATION_TAG_INVALID`: "Tags must contain only alphanumeric characters and hyphens"
- `VALIDATION_TAG_LENGTH`: "Each tag must be 1-20 characters"
- `VALIDATION_TAG_COUNT`: "Maximum 10 tags allowed per task"

### Operation Errors
- `TASK_NOT_FOUND`: "Task with ID '{id}' not found"
- `STORAGE_ERROR`: "Failed to access local storage: {details}"
- `COMMAND_INVALID`: "Invalid command: {command}"
- `COMMAND_ARGS_INVALID`: "Invalid arguments for command: {command}"

### System Errors
- `STORAGE_QUOTA_EXCEEDED`: "Storage quota exceeded. Please delete some tasks"
- `DATA_CORRUPTION`: "Stored data is corrupted. Data has been reset"

## Storage Schema

### localStorage Structure
```javascript
// Key: "tasks"
// Value: JSON string of Task[]
localStorage.setItem("tasks", JSON.stringify(taskArray));

// Key: "app_metadata"
// Value: JSON string with app state
localStorage.setItem("app_metadata", JSON.stringify({
  version: "1.0.0",
  lastBackup: "2024-01-15T10:30:00.000Z",
  taskCount: 42
}));
```

### Data Migration Strategy
- Version field in metadata for future schema changes
- Backward compatibility maintained
- Migration functions for version upgrades
- Data export/import for major changes

### Backup and Recovery
- Automatic JSON export on data changes
- Manual export via "export" command
- Import validation before data replacement
- Recovery from corrupted state to empty state