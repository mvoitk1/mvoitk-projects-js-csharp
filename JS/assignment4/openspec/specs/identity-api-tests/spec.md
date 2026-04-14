## ADDED Requirements

### Requirement: Login happy path test

Verify that `POST /api/v1.0/Account/Login` with valid credentials returns a complete `JwtResponse`.

#### Scenario: Successful login with seeded admin user
- **WHEN** POST to `/api/v1.0/Account/Login` with `{"email": "akaver@akaver.com", "password": "Foo.bar1"}`
- **THEN** response status is 200
- **THEN** response body deserializes to `JwtResponse` with non-empty `Token`, `RefreshToken` (36-char GUID string), `FirstName` = "Andres", `LastName` = "Käver"

### Requirement: Login failure returns generic error for wrong password

Verify that incorrect password returns 404 with a generic message that does not reveal whether the email exists.

#### Scenario: Login with valid email but wrong password
- **WHEN** POST to `/api/v1.0/Account/Login` with `{"email": "akaver@akaver.com", "password": "WrongPass1"}`
- **THEN** response status is 404
- **THEN** response body deserializes to `Message` with `Messages` containing "User/Password problem!"

### Requirement: Login failure returns generic error for non-existent user

Verify that a non-existent email returns the same 404 error as a wrong password, preventing user enumeration.

#### Scenario: Login with non-existent email
- **WHEN** POST to `/api/v1.0/Account/Login` with `{"email": "nonexistent@test.com", "password": "Foo.bar1"}`
- **THEN** response status is 404
- **THEN** response body deserializes to `Message` with `Messages` containing "User/Password problem!"

### Requirement: JWT token content validation after login

Verify the JWT issued on login contains correct claims, issuer, audience, and no unexpected claims.

#### Scenario: Decode and verify JWT claims for seeded user
- **WHEN** POST to `/api/v1.0/Account/Login` with valid credentials and decode the returned JWT via `JwtSecurityTokenHandler.ReadJwtToken()`
- **THEN** JWT contains a `ClaimTypes.Email` claim matching `akaver@akaver.com`
- **THEN** JWT `iss` = `taltech.akaver.com`
- **THEN** JWT `aud` = `taltech.akaver.com`
- **THEN** JWT does NOT contain `ClaimTypes.GivenName` (seeded user has no explicit claims added via `AddClaimAsync`)

### Requirement: Login respects custom expiresInSeconds parameter

Verify that the optional `expiresInSeconds` query parameter controls JWT expiration.

#### Scenario: Login with expiresInSeconds=3
- **WHEN** POST to `/api/v1.0/Account/Login?expiresInSeconds=3` with valid credentials
- **THEN** the decoded JWT `exp` claim is approximately 3 seconds from the current UTC time (within 2-second tolerance)

### Requirement: Login cleans up expired refresh tokens

Verify that logging in removes previously expired refresh tokens from the database.

#### Scenario: Expired tokens cleaned on subsequent login
- **WHEN** user logs in twice (creating 2 refresh tokens), then the first token's expiration passes, then the user logs in a third time
- **THEN** a refresh attempt using the first session's expired refresh token fails
- **THEN** the most recent login's refresh token remains valid

---

### Requirement: Register happy path test

Verify that `POST /api/v1.0/Account/Register` with valid data creates a user and returns a `JwtResponse` with name claims in the JWT.

#### Scenario: Successful registration of a new user
- **WHEN** POST to `/api/v1.0/Account/Register` with a unique email, valid password, firstName, lastName
- **THEN** response status is 200
- **THEN** response body deserializes to `JwtResponse` with non-empty `Token`, `RefreshToken`, correct `FirstName`, `LastName`
- **THEN** the decoded JWT contains `ClaimTypes.GivenName` matching the provided firstName
- **THEN** the decoded JWT contains `ClaimTypes.Surname` matching the provided lastName

### Requirement: Register rejects duplicate email

#### Scenario: Register with an already-registered email
- **WHEN** a user is registered, then a second POST to `/api/v1.0/Account/Register` uses the same email
- **THEN** response status is 400
- **THEN** response body contains message "User already registered"

### Requirement: Register rejects weak password

#### Scenario: Register with password that fails Identity validation
- **WHEN** POST to `/api/v1.0/Account/Register` with password "123"
- **THEN** response status is 400
- **THEN** response body `Messages` collection contains Identity password validation error strings

### Requirement: Registered user can subsequently login

#### Scenario: Register then login round-trip
- **WHEN** a new user registers successfully, then POST to `/api/v1.0/Account/Login` with the same email and password
- **THEN** login response status is 200
- **THEN** login response returns a valid `JwtResponse`

### Requirement: Registration JWT contains no duplicate claims

Verify that the JWT issued on registration does not contain duplicate claim types, which could occur if `AddClaimAsync` and `CreateUserPrincipalAsync` both inject the same claims.

#### Scenario: No duplicate claim types in registration JWT
- **WHEN** a new user registers and the returned JWT is decoded
- **THEN** grouping all claims by `Type`, every group has exactly `Count() == 1`
- **THEN** specifically `ClaimTypes.Email`, `ClaimTypes.NameIdentifier`, `ClaimTypes.GivenName`, `ClaimTypes.Surname` each appear at most once

---

### Requirement: Single refresh token cycle

Verify the basic token rotation flow: expired JWT + valid refresh token yields new JWT and new refresh token.

#### Scenario: Refresh after JWT expiration
- **WHEN** user logs in with `expiresInSeconds=1`, waits 1.5 seconds, then POST to `/api/v1.0/Account/RefreshToken` with the expired JWT and original refresh token
- **THEN** response status is 200
- **THEN** new `JwtResponse` contains a different `Token` and a different `RefreshToken` than the originals

### Requirement: Multi-cycle refresh token rotation (3+ consecutive cycles)

Verify that refresh tokens can be rotated multiple times in sequence, each cycle producing unique tokens, with no token reuse.

#### Scenario: Three consecutive refresh cycles
- **WHEN** user logs in with `expiresInSeconds=1` → JWT₁, RT₁
- **WHEN** wait 1.5s → RefreshToken(JWT₁, RT₁) with `expiresInSeconds=1` → JWT₂, RT₂
- **WHEN** wait 1.5s → RefreshToken(JWT₂, RT₂) with `expiresInSeconds=1` → JWT₃, RT₃
- **WHEN** wait 1.5s → RefreshToken(JWT₃, RT₃) → JWT₄, RT₄
- **THEN** all four JWTs are distinct strings
- **THEN** all four refresh tokens (RT₁, RT₂, RT₃, RT₄) are distinct strings
- **THEN** each refresh response has status 200

### Requirement: PreviousToken grace period allows old token reuse briefly

Verify that immediately after a refresh rotation, the OLD refresh token still works within the 1-minute grace window.

#### Scenario: Refresh using previous token within grace period
- **WHEN** user logs in → JWT₁, RT₁, then refreshes → JWT₂, RT₂, then immediately refreshes again using JWT₂ and the OLD RT₁
- **THEN** response status is 200 (previous token accepted within 1-minute grace period)

### Requirement: Invalid refresh token is rejected

#### Scenario: Refresh with random GUID as refresh token
- **WHEN** user logs in, then POST to `/api/v1.0/Account/RefreshToken` with the valid JWT but a random GUID as `RefreshToken`
- **THEN** response status is 500 (ProblemDetails)
- **THEN** response contains "no valid refresh tokens found"

### Requirement: Malformed JWT is rejected on refresh

#### Scenario: Refresh with unparseable JWT string
- **WHEN** POST to `/api/v1.0/Account/RefreshToken` with `Jwt = "not-a-jwt"` and a random `RefreshToken`
- **THEN** response status is 400
- **THEN** response message contains "Cant parse the token"

### Requirement: JWT missing email claim is rejected on refresh

#### Scenario: Refresh with JWT that has no email claim
- **WHEN** a JWT is crafted using `IdentityExtensions.GenerateJwt()` with an empty claims list, then POST to `/api/v1.0/Account/RefreshToken` with this JWT
- **THEN** response status is 400
- **THEN** response message contains "No email in jwt"

### Requirement: JWT content consistency across refresh cycles

Verify that refreshing a token preserves all identity claims and only changes temporal claims.

#### Scenario: Compare claims before and after refresh
- **WHEN** user logs in → JWT₁, refreshes → JWT₂, both JWTs are decoded
- **THEN** `ClaimTypes.NameIdentifier` (sub) is identical in both
- **THEN** `ClaimTypes.Email` is identical in both
- **THEN** `iss` and `aud` are identical in both
- **THEN** only `exp`, `nbf`, `iat` differ between them
- **THEN** no duplicate claim types exist in JWT₂

### Requirement: Refresh token DB records do not accumulate (no double records)

Verify that after multiple refresh cycles for a single session, only one valid refresh token exists per session. The controller rejects `Count > 1` as an error.

#### Scenario: Five consecutive refresh cycles with no accumulation
- **WHEN** a new user registers, then performs 5 sequential refresh cycles (each with `expiresInSeconds=1` and 1.5s wait)
- **THEN** every refresh succeeds with status 200 (the controller's `Count != 1` assertion is never triggered)
- **THEN** all 6 refresh tokens (initial + 5 rotations) are distinct

### Requirement: Concurrent sessions for the same user work independently

Verify that two separate login sessions (each with their own refresh token) do not interfere with each other.

#### Scenario: Two independent sessions refresh successfully
- **WHEN** user logs in twice → (JWT_A, RT_A) and (JWT_B, RT_B)
- **WHEN** RefreshToken(JWT_A, RT_A) → success
- **WHEN** RefreshToken(JWT_B, RT_B) → success
- **THEN** both refresh operations return status 200 with valid new tokens
