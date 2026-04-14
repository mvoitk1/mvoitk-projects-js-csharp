## ADDED Requirements

### Requirement: ListItems rejects missing API key

#### Scenario: GET without apiKey query parameter
- **WHEN** GET `/api/v1.0/ListItems` without an `apiKey` query parameter (Guid defaults to `Guid.Empty`)
- **THEN** response status is 400
- **THEN** response body contains "Problem with ApiKey"

### Requirement: ListItems rejects invalid API key

#### Scenario: GET with random GUID as apiKey
- **WHEN** GET `/api/v1.0/ListItems?apiKey={randomGuid}` where `randomGuid` is not a registered API key
- **THEN** response status is 400
- **THEN** response body contains "Problem with ApiKey"

### Requirement: ListItems full CRUD lifecycle

Verify that a list item can be created, read, updated, listed, and deleted using a valid API key.

#### Scenario: Create, read, update, list, delete a list item
- **WHEN** POST `/api/v1.0/ListItems?apiKey={seededKey}` with `{"description": "Buy milk", "completed": false}`
- **THEN** response status is 201 and response body contains the created item with a generated `Id`, matching `Description` and `Completed`
- **WHEN** GET `/api/v1.0/ListItems/{id}?apiKey={seededKey}`
- **THEN** response status is 200 and fields match the created values
- **WHEN** PUT `/api/v1.0/ListItems/{id}?apiKey={seededKey}` with `Completed` toggled to `true`
- **THEN** response status is 204
- **WHEN** GET `/api/v1.0/ListItems?apiKey={seededKey}`
- **THEN** response contains the updated item with `Completed = true`
- **WHEN** DELETE `/api/v1.0/ListItems/{id}?apiKey={seededKey}`
- **THEN** response status is 200 and response body contains the deleted item
- **WHEN** GET `/api/v1.0/ListItems/{id}?apiKey={seededKey}`
- **THEN** response status is 404

### Requirement: ListItems supports completed filter

#### Scenario: Filter by completed status
- **WHEN** two items are created: one with `completed=false`, one with `completed=true`
- **WHEN** GET `/api/v1.0/ListItems?apiKey={seededKey}&completed=true`
- **THEN** response contains only the completed item
- **WHEN** GET `/api/v1.0/ListItems?apiKey={seededKey}&completed=false`
- **THEN** response contains only the non-completed item
- **WHEN** GET `/api/v1.0/ListItems?apiKey={seededKey}` (no completed filter)
- **THEN** response contains both items

### Requirement: ListItems cross-key data isolation

Items created under one API key must not be visible to another API key.

#### Scenario: Two API keys see only their own items
- **WHEN** a second API key is seeded (or created)
- **WHEN** an item is created under API key A
- **WHEN** GET `/api/v1.0/ListItems?apiKey={keyB}`
- **THEN** response is an empty list (Key B has no items)
- **WHEN** GET `/api/v1.0/ListItems/{itemId}?apiKey={keyB}` using the item ID created under Key A
- **THEN** response status is 404

### Requirement: ListItems PUT rejects mismatched ID

#### Scenario: URL ID differs from body ID
- **WHEN** PUT `/api/v1.0/ListItems/{idA}?apiKey={seededKey}` with a body containing `Id = {idB}` where `idA != idB`
- **THEN** response status is 400
