## ADDED Requirements

### Requirement: Expired JWT is rejected by authenticated endpoints

Verify that the `ClockSkew = TimeSpan.Zero` JWT configuration enforces strict token expiration.

#### Scenario: Access protected endpoint with expired token
- **WHEN** user logs in with `expiresInSeconds=1`
- **WHEN** wait 2 seconds for the token to expire
- **WHEN** GET `/api/v1.0/TodoCategories` with the expired JWT in the Authorization header
- **THEN** response status is 401 Unauthorized

### Requirement: JWT from one user cannot access another user's data

End-to-end authorization test spanning the JWT middleware and controller ownership filters.

#### Scenario: Cross-user data access via GET by ID
- **WHEN** User A registers and creates a TodoCategory, obtaining its `Id`
- **WHEN** User B registers and attempts GET `/api/v1.0/TodoCategories/{id}` using User B's JWT and User A's category `Id`
- **THEN** response status is 404 (not 200 or 403)

### Requirement: API versioning enforced in URL

#### Scenario: Valid API version returns success
- **WHEN** GET `/api/v1.0/TodoCategories` with a valid JWT
- **THEN** response status is 200

#### Scenario: Unsupported API version returns error
- **WHEN** GET `/api/v2.0/TodoCategories` with a valid JWT
- **THEN** response status is 400 or 404 (no v2.0 controllers are registered)
