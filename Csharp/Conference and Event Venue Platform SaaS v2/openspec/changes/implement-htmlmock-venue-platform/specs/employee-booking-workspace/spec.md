## ADDED Requirements

### Requirement: Employees can see an operational booking dashboard
The system SHALL provide an employee dashboard that summarizes upcoming bookings, service readiness, and operational metrics relevant to day-of coordination.

#### Scenario: Employee opens dashboard
- **WHEN** an authenticated employee opens the booking dashboard route
- **THEN** the system displays booking metrics, upcoming events, and operational highlights scoped to the employee's venue context

### Requirement: Employees can manage their booking list and booking details
The system SHALL provide an employee workspace for viewing booking cards, statuses, schedules, and service details for assigned or venue-relevant bookings.

#### Scenario: Employee views bookings list
- **WHEN** an authenticated employee opens the bookings workspace
- **THEN** the system displays bookable events with status, timing, venue space, attendee, and service summary information

#### Scenario: Employee inspects coordination context
- **WHEN** an authenticated employee opens an event coordination view
- **THEN** the system displays staffing, setup, service, and readiness information for that booking

### Requirement: Employees can view and manage catering orders tied to bookings
The system SHALL provide a catering workspace that shows booking-linked catering orders, order state, lock timing, and editable order selections within allowed constraints.

#### Scenario: Employee opens catering orders workspace
- **WHEN** an authenticated employee opens the catering orders route
- **THEN** the system displays catering orders and summaries associated with relevant bookings

#### Scenario: Employee edits catering before lock deadline
- **WHEN** an authenticated employee updates a catering order before its lock deadline
- **THEN** the system persists the updated catering selections and recalculates the displayed summary
