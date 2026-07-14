# EventBoard Application - User Acceptance Testing (UAT) Plan

## Project
EventBoard API

## Objective
The purpose of this User Acceptance Testing (UAT) plan is to verify that the EventBoard application meets the business requirements and provides the expected functionality for end users before deployment.

---

# Risk Ranking

| Priority | Feature | Risk |
|----------|---------|------|
| 1 | User Registration & Login | High |
| 2 | Create Event | High |
| 3 | Update Event | High |
| 4 | Delete Event | High |
| 5 | View Events | Medium |
| 6 | Event Booking | Medium |
| 7 | Favorites | Medium |
| 8 | Weather Information | Low |

---

# UAT-01: User Registration

**Priority:** High

### Objective
Verify that a new user can successfully register.

### Preconditions
- User is not already registered.

### Test Steps
1. Open the application.
2. Navigate to Register.
3. Enter a username.
4. Enter a unique email.
5. Enter a password.
6. Click **Register**.

### Expected Result
- Registration succeeds.
- User account is created.
- User can log in using the new credentials.

### Status
Pass / Fail

---

# UAT-02: User Login

**Priority:** High

### Objective
Verify that registered users can log in.

### Preconditions
- User account already exists.

### Test Steps
1. Open Login page.
2. Enter email.
3. Enter password.
4. Click **Login**.

### Expected Result
- Login succeeds.
- JWT token is generated.
- User is redirected to the dashboard.

### Status
Pass / Fail

---

# UAT-03: Create Event

**Priority:** High

### Objective
Verify that an authenticated organizer can create an event.

### Preconditions
- User is logged in.

### Test Steps
1. Navigate to Create Event.
2. Enter event title.
3. Select category.
4. Select event date.
5. Enter location.
6. Save the event.

### Expected Result
- Event is successfully created.
- Event appears in the event list.

### Status
Pass / Fail

---

# UAT-04: Update Event

**Priority:** High

### Objective
Verify that an organizer can update an existing event.

### Preconditions
- Event already exists.
- User is the organizer.

### Test Steps
1. Open an existing event.
2. Edit the title.
3. Update the date.
4. Save changes.

### Expected Result
- Event information is updated.
- Updated values are displayed.

### Status
Pass / Fail

---

# UAT-05: Delete Event

**Priority:** High

### Objective
Verify that an organizer can delete an event.

### Preconditions
- Event exists.

### Test Steps
1. Select an event.
2. Click Delete.
3. Confirm deletion.

### Expected Result
- Event is removed.
- Event no longer appears in the event list.

### Status
Pass / Fail

---

# UAT-06: View Events

**Priority:** Medium

### Objective
Verify users can browse available events.

### Preconditions
- Events exist in the system.

### Test Steps
1. Open Events page.
2. Scroll through available events.
3. Open an event.

### Expected Result
- Events load successfully.
- Event details are displayed correctly.

### Status
Pass / Fail

---

# UAT-07: Book an Event

**Priority:** Medium

### Objective
Verify users can book an available event.

### Preconditions
- User is logged in.
- Event has available seats.

### Test Steps
1. Open an event.
2. Click **Book Event**.
3. Confirm booking.

### Expected Result
- Booking is successful.
- Booking appears in the user's bookings.

### Status
Pass / Fail

---

# UAT-08: Add Event to Favorites

**Priority:** Medium

### Objective
Verify users can mark an event as a favorite.

### Preconditions
- User is logged in.

### Test Steps
1. Open an event.
2. Click the Favorite icon.
3. Open Favorites page.

### Expected Result
- Event is added to Favorites.
- Favorite event appears in the user's Favorites list.

### Status
Pass / Fail

---

# Acceptance Criteria

The application will be accepted if:

- All High-risk UAT scripts pass.
- No Critical defects remain open.
- Authentication works correctly.
- Event CRUD operations function correctly.
- Booking functionality works correctly.
- Favorites functionality works correctly.
- Users can successfully browse events.
- System behaves as expected under normal usage.

---

# Test Execution Summary

| UAT ID | Feature | Priority | Result |
|---------|----------|----------|--------|
| UAT-01 | Registration | High | Pass |
| UAT-02 | Login | High | Pass |
| UAT-03 | Create Event | High | Pass |
| UAT-04 | Update Event | High | Pass |
| UAT-05 | Delete Event | High | Pass |
| UAT-06 | View Events | Medium | Pass |
| UAT-07 | Book Event | Medium | Pass |
| UAT-08 | Favorites | Medium | Pass |

---

## Prepared By

**Name:** Abdul Ahad

**Project:** EventBoard API

**Testing Type:** User Acceptance Testing (UAT)