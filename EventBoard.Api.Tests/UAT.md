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
| 2 | Docker one-command startup | High |
| 3 | Create Event (any authenticated user) | High |
| 4 | Admin: User Management | High |
| 5 | Update / Delete Event (Admin) | High |
| 6 | RSVP (yes/maybe/no) | Medium |
| 7 | Browse Events (public) | Medium |
| 8 | Favorites | Medium |
| 9 | Admin: Statistics | Low |
| 10 | Weather Forecast | Low |

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

# UAT-07: RSVP to an Event

**Priority:** Medium

### Objective
Verify users can RSVP yes / maybe / no and that the count updates.

### Preconditions
- User is logged in.

### Test Steps
1. Open an event detail page.
2. Note the current RSVP tallies.
3. Click **Yes**.
4. Click **Maybe** to change the response.

### Expected Result
- The selected option is highlighted as the user's response.
- The RSVP tallies (Yes/Maybe/No and total) update accordingly.
- The RSVP appears under "My RSVPs" on the dashboard.

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

# UAT-09: Browse Events as a Visitor (Public)

**Priority:** Medium

### Objective
Verify that an unauthenticated visitor can browse events.

### Test Steps
1. Open the application without logging in.
2. View the event list on the home page.
3. Open an event's detail page.

### Expected Result
- The event list and detail load without requiring login.
- RSVP controls prompt the visitor to sign in.

### Status
Pass / Fail

---

# UAT-10: Admin — User Management

**Priority:** High

### Objective
Verify an admin can manage user accounts and roles.

### Preconditions
- Logged in as `admin@eventboard.com`.

### Test Steps
1. Open Admin Panel → Manage Users.
2. Promote a `User` to `Admin`, then demote back.
3. Disable another user's account.
4. Log out and try to log in as the disabled user.

### Expected Result
- Role changes are reflected in the list.
- The disabled account cannot log in (clear error message).
- The admin cannot demote or disable their own account.

### Status
Pass / Fail

---

# UAT-11: Admin — Statistics

**Priority:** Low

### Objective
Verify the admin dashboard shows summary statistics.

### Test Steps
1. Open Admin Panel.
2. Review the statistics cards.

### Expected Result
- Totals for users, events, categories, RSVPs and favorites are shown.
- The RSVP Yes/Maybe/No breakdown is displayed.

### Status
Pass / Fail

---

# UAT-12: Weather Forecast on Event Detail

**Priority:** Low

### Objective
Verify current weather and a short forecast appear for an event's city.

### Preconditions
- A valid `OPENWEATHER_API_KEY` is configured.

### Test Steps
1. Open an event whose location includes a real city.
2. View the weather panel.

### Expected Result
- Current conditions (temperature, description, icon) are shown.
- A short multi-day forecast is displayed.
- If no API key is configured, a graceful "unavailable" message is shown instead.

### Status
Pass / Fail

---

# UAT-13: One-Command Docker Startup

**Priority:** High

### Objective
Verify the entire stack starts with a single command.

### Test Steps
1. From the repo root run `docker-compose down -v`.
2. Run `docker-compose up --build`.
3. Wait for the API to report the database migrated and seeded.
4. Open `http://localhost`.

### Expected Result
- SQL Server, API and frontend all start.
- The app is reachable at `http://localhost` and seeded data is visible.
- Uploaded images persist across `docker-compose restart`.

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
| UAT-07 | RSVP (yes/maybe/no) | Medium | Pass |
| UAT-08 | Favorites | Medium | Pass |
| UAT-09 | Public Browsing | Medium | Pass |
| UAT-10 | Admin User Management | High | Pass |
| UAT-11 | Admin Statistics | Low | Pass |
| UAT-12 | Weather Forecast | Low | Pass |
| UAT-13 | Docker Startup | High | Pass |

---

## Prepared By

**Name:** Abdul Ahad

**Project:** EventBoard API

**Testing Type:** User Acceptance Testing (UAT)