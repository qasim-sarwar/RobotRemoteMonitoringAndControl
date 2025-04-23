## Robot Control API & Remote Monitoring and Control API Tests
This project consists of two interconnected parts: a Robot Control API built using .NET 9 Minimal APIs secured with JWT authentication, and a comprehensive test suite designed to ensure its reliability and correctness through both unit and integration tests.

Project Overview The Robot Control API is a lightweight yet powerful service that enables users to interact with a simulated robot. It demonstrates the use of .NET 9 Minimal APIs, enabling fast and secure operations. Users can log in, issue commands (such as moving the robot), monitor the robot's status, and review the command history. JWT-based authentication ensures that only authorized requests can affect the robot's settings or view sensitive data.

In parallel, the Remote Monitoring and Control API Tests project provides a rigorous set of unit and integration tests. These tests simulate the API’s runtime behavior using the WebApplicationFactory and an in-memory database, ensuring that both individual endpoints and overall system behaviors are validated efficiently and in isolation.

## 🛠️ Technologies Used
Backend Framework: .NET 9 Minimal APIs with c#13

Authentication: JWT Authentication

API Documentation: Swagger UI

Testing Framework: xUnit

Test Utilities:

Microsoft.AspNetCore.Mvc.Testing (for in-memory API testing)

Microsoft.EntityFrameworkCore.InMemory (for a lightweight test database)

Moq (for dependency mocking)

Newtonsoft.Json (for JSON operations)

## API Features
Authentication Flow Login Endpoint (POST /login): Users first authenticate by sending a POST request with valid credentials (for example, username: user and password: password). Successful authentication returns a JWT token, which must be included in the Authorization header (using the format Bearer YOUR_JWT_TOKEN) for all protected endpoints.

Robot Commands Submit Command (POST /command): Users send commands (e.g., "Move Forward") to the robot. Each command is linked with a specific user and robot, along with details about the task.

Update Command (PUT /command): Modify an existing command.

Retrieve Command (GET /command): Retrieve details of a command by providing its unique ID.

Robot Status & Command History Robot Status (GET /status): Returns the current state of the robot.
Command History (GET /history): Lists all commands issued, serving as an audit log for all operations performed on the robot.

Error Handling & Security Error Handling: The API returns appropriate HTTP status codes (such as 401 for unauthorized access and 404 for non-existent resources). Double-check the JWT token validity if you encounter unauthorized errors.
Security: Keep your JWT tokens secure and do not expose them publicly. The token expiration is set to 1 hour, after which you must log in again to obtain a new token.

## 🚀 Getting Started
For the API Clone the Repository:

bash git clone https://github.com/yourusername/robot-control-api.git cd robot-control-api Run the Application Locally:

Ensure you have the .NET 9 SDK installed, then run:
bash dotnet run The API will start on https://localhost:7211.

## Open Swagger UI:
Open your browser and navigate to https://localhost:7211/swagger/index.html to test the API endpoints.

## JWT Authentication:
First, authorize by calling the /login endpoint.

Then, click the Authorize button in Swagger UI and enter your token using the format Bearer YOUR_JWT_TOKEN.

## Testing Endpoints:

POST /command: Submit a new command by entering a JSON payload like:

json { "commandText": "Move Forward", "robot": "Robot1", "user": "user" }

GET /status: Retrieve the robot’s current status.

Use similar approaches for updating and retrieving commands as needed.

For the Test Suite The Remote Monitoring and Control API Tests project validates all the API endpoints and behaviors using both unit and integration tests.

Clone the Repository Containing the Tests:
bash git clone https://github.com/your-repository-name.git cd RemoteMonitoringAndControlAPI.Tests Install Dependencies:

## Restore dependencies using:

bash dotnet restore Run the Tests:

Execute the test suite using:

bash dotnet test View Results:

Check the console output or use your IDE’s test explorer to see detailed results.

## API Endpoints Summary
Endpoint Method Description /login POST Authenticate and retrieve a JWT token. /command POST Submit a new command to the robot. /command PUT Update an existing command. /command GET Retrieve a command's details by ID. /status GET Retrieve the current status of the robot. /history GET Retrieve the complete history of commands issued.

## API Calls
1. Login (POST /login)
Request (JSON):

json { "username": "user", "password": "password" } Response (JSON):

json { "token": "YOUR_JWT_TOKEN" }

2. Submit a Command (POST /command)
Request (JSON):

json { "commandText": "Move Forward", "robot": "Robot1", "user": "user" } Authorization Header:

http Bearer YOUR_JWT_TOKEN

3. Retrieve Robot Status (GET /status)
Request URL:

https://localhost:7211/status Authorization Header: http Bearer YOUR_JWT_TOKEN

## 🚧 Testing Details & Best Practices
Test Coverage Summary Test Case Covered? Health Check API ✅ Yes Login - Valid Credentials ✅ Yes Login - Invalid Credentials ✅ Yes Unauthorized Access ✅ Yes Command - Add New ✅ Yes Command - Retrieve Existing ✅ Yes Command - Update Existing ✅ Yes Command - Get Non-Existing ✅ Yes Robot Status API ✅ Yes Command History API ✅ Yes Testing Framework & Setup Integration Testing: The tests leverage the WebApplicationFactory to simulate the API in an isolated in-memory environment with Microsoft.EntityFrameworkCore.InMemory. This ensures a lightweight yet realistic testing experience.

Unit Testing: Unit tests validate individual endpoints and key functionalities. Tools like Moq are used to simulate dependencies, and Newtonsoft.Json helps with advanced JSON handling when needed.

## Mock Database
In-Memory Database: Every test run uses an independent dataset, ensuring isolated and repeatable test outcomes without the need for a physical database.

## Extending Tests
Adding New Tests: New test methods can easily be added to the project (for instance, in a file like ProgramTests.cs) to cover additional endpoints or new features as they’re implemented. Mocking Additional Services: If your API evolves to include new services, consider using Moq to create mocked implementations, ensuring that the tests remain both isolated and robust. Updating Test Configuration: The WebApplicationFactory setup can be adjusted in the test project constructor to match various environments or configuration needs.

## Conclusion
This merged documentation outlines both the operational aspects of the Robot Control API and its accompanying tests. The API provides a simple yet effective interface for interacting with a simulated robot, while the test suite ensures that the application’s integrity is maintained through rigorous unit and integration tests. Whether you’re interested in expanding API functionality or extending test coverage, this guide is designed to help you quickly understand, deploy, and validate the system.
