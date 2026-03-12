## ADDED Requirements

### Requirement: Prospective venue operators can submit a venue access request
The system SHALL allow a prospective venue operator to submit a structured venue request that captures company, venue, contact, and operational details needed for admin review.

#### Scenario: User submits a venue request
- **WHEN** a user completes the venue request form with all required fields
- **THEN** the system stores the request with a pending review status and confirms successful submission to the requester

#### Scenario: User submits incomplete request data
- **WHEN** a user submits the venue request form without required details
- **THEN** the system rejects the submission and displays validation errors on the form

### Requirement: Platform admins can review and triage venue access requests
The system SHALL provide a platform-admin review screen that lists submitted venue requests, exposes request details, and supports assigning a review outcome and venue rights where approval permits it.

#### Scenario: Admin opens venue request review workspace
- **WHEN** an authenticated platform admin opens the venue request review route
- **THEN** the system displays pending and processed requests with key metadata needed for triage

#### Scenario: Admin updates request status
- **WHEN** an authenticated platform admin approves, rejects, or defers a venue request
- **THEN** the system persists the selected decision, records the updated review status, and reflects the change in the review list

#### Scenario: Admin assigns venue rights after approval
- **WHEN** an authenticated platform admin grants venue access to a user for an approved venue request
- **THEN** the system persists the venue membership and selected access level for that venue
