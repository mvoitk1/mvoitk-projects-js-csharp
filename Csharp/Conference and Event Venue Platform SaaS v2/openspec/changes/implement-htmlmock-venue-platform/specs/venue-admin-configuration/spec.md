## ADDED Requirements

### Requirement: Venue managers can view an administrative venue dashboard
The system SHALL provide a company-manager dashboard that summarizes venue performance, configuration status, and actionable operational signals for the selected venue.

#### Scenario: Manager opens admin dashboard
- **WHEN** an authenticated company manager opens the venue admin dashboard route
- **THEN** the system displays venue-level metrics, status summaries, and actionable operational items

### Requirement: Venue managers can configure spaces and layouts
The system SHALL provide a space and layout management workflow for maintaining venue spaces, capacities, layout options, and configuration notes.

#### Scenario: Manager views spaces and layouts
- **WHEN** an authenticated company manager opens the spaces and layouts route
- **THEN** the system displays configured spaces, supported layouts, and capacity-oriented metadata for the selected venue

#### Scenario: Manager updates a space configuration
- **WHEN** an authenticated company manager saves valid changes to a space or layout configuration
- **THEN** the system persists the changes and reflects the updated configuration in the management workspace

### Requirement: Platform admins can review venue onboarding and access control
The system SHALL provide a platform-admin workspace for reviewing venue onboarding requests and granting or withholding venue rights based on approval state.

#### Scenario: Platform admin opens onboarding review workspace
- **WHEN** an authenticated platform admin opens the onboarding review route
- **THEN** the system displays venue requests, review state, and rights-assignment actions with blocked states where approval is incomplete
