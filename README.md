# Robot Control API & Remote Monitoring and Control API Tests

This project consists of two interconnected parts: a Robot Control API built using .NET 9 Minimal APIs secured with JWT authentication, and a comprehensive test suite designed to ensure its reliability and correctness through both unit and integration tests.

---

## Project Overview

### Robot Control API
The Robot Control API is a lightweight yet powerful service that enables users to interact with a simulated robot. It demonstrates the use of .NET Minimal APIs, enabling fast and secure operations. Users can:

- **Log In**: Authenticate via a POST `/login` endpoint to obtain a JWT token.
- **Issue Commands**: Submit commands (e.g., "Move Forward") using the POST `/command` endpoint.
- **Update & Retrieve Commands**: Update existing commands with PUT `/command` and retrieve a command by its ID via GET `/command`.
- **Monitor Robot Status**: Check the current status of the robot via GET `/status`.
- **Review Command History**: Fetch an audit log of all issued commands via GET `/history`.

JWT-based authentication ensures that only authorized requests can modify settings or access sensitive data.

### Remote Monitoring and Control API Tests
In parallel, the **Remote Monitoring and Control API Tests** project provides a rigorous set of unit and integration tests. These tests simulate the API’s runtime behavior using the [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) and an in-memory database. This testing strategy verifies both individual endpoints and the system’s overall behavior in isolation.

---

## 🛠️ Technologies Used

- **Backend Framework:** .NET Minimal APIs (targeting .NET 8/9)
- **Authentication:** JWT Authentication
- **API Documentation:** Swagger UI
- **Testing Framework:** xUnit
- **Test Utilities:**
  - Microsoft.AspNetCore.Mvc.Testing (for in-memory API testing)
  - Microsoft.EntityFrameworkCore.InMemory (for a lightweight test database)
  - Moq (for dependency mocking)
  - Newtonsoft.Json (for advanced JSON operations when needed)

---

## API Features

### Authentication Flow

- **Login Endpoint (POST /login):**  
  Users authenticate by sending a POST with valid credentials (e.g., `username: "user", password: "password"`). A valid request returns a JWT token. Include the token in the `Authorization` header using the format `Bearer YOUR_JWT_TOKEN` for all protected endpoints.

### Robot Commands

- **Submit Command (POST /command):**  
  Users send commands (e.g., "Move Forward") to the robot. Each command is associated with a user and a robot.
  
- **Update Command (PUT /command):**  
  Modify an existing command.
  
- **Retrieve Command (GET /command):**  
  Fetch command details by providing its unique ID.

### Robot Status & Command History

- **Robot Status (GET /status):**  
  Returns the current state of the robot.
- **Command History (GET /history):**  
  Provides an audit log of all commands issued to the robot.

### Error Handling & Security

- **Error Handling:**  
  The API returns appropriate HTTP status codes (e.g., `401` for unauthorized access, and `404` for non-existent resources).  
- **Security:**  
  JWT tokens must be kept secure. Tokens expire after 1 hour, requiring re-authentication.

---

## 🚀 Getting Started

### For the API

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/yourusername/robot-control-api.git
   cd robot-control-api
