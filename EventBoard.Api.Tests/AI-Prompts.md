# AI Prompts Used During Testing

## Prompt 1 – Unit Tests for EventService

Generate xUnit unit tests for EventService using Moq. Cover all public methods, including successful execution, invalid input, and exception scenarios. Ensure the tests follow the Arrange-Act-Assert pattern.

---

## Prompt 2 – Unit Tests for AuthService

Generate xUnit unit tests for AuthService using Moq. Cover:
- Successful user registration
- Duplicate email registration
- Successful login
- Invalid password
- User not found

Use Arrange-Act-Assert and verify repository and JWT service interactions.

---

## Prompt 3 – Integration Testing

Generate a CustomWebApplicationFactory using WebApplicationFactory and an EF Core InMemory database. Write integration tests for the EventsController CRUD endpoints and include authentication where required.

---

## Prompt 4 – Coverage Improvement

Review the Coverlet coverage report and identify uncovered branches in the EventsController. Generate at least three additional integration tests that improve line and branch coverage.

---

## Prompt 5 – UAT Test Plan

Generate a User Acceptance Testing (UAT) plan in Markdown for the EventBoard application. Include:
- Eight UAT scripts
- Preconditions
- Test steps
- Expected results
- Risk ranking
- Test execution summary