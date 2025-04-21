# Robot Control API

This project is a simple Robot Control API built using .NET 8 Minimal APIs with JWT-based authentication. It provides endpoints for logging in, controlling a robot, viewing its status, and managing commands.

## 🛠️ Technologies Used
- .NET 9
- JWT Authentication
- Swagger UI for API documentation

## 🔑 Key Sections
Project Overview
The Robot Control API is a simple and flexible API that allows users to interact with a simulated robot. It demonstrates the power of .NET 9 Minimal APIs, providing a fast, lightweight framework for building secure, production-ready applications. This project focuses on providing users with a straightforward way to send commands, retrieve the robot's status, and manage command history.

Authentication Flow
The API uses JWT Authentication to secure access to certain endpoints. To interact with protected resources (like sending commands or viewing the robot's status), a user must first authenticate by providing valid credentials (username and password). The /login endpoint issues a JWT token, which can then be used for authorized requests to other endpoints.

Robot Commands
The core feature of this API is the ability to send and manage robot commands. These commands dictate the robot’s tasks, such as moving or performing specific operations. Each command is associated with a user, a robot, and a task description. The API allows users to submit new commands, update existing commands, and view the status of the robot as it performs these tasks.

Command History
The /history endpoint tracks all issued commands, allowing users to retrieve the entire command history for review. This helps to audit what actions have been taken and when.

## 🚀 Getting Started
Follow these steps to test the API locally.

### 1. Clone the Repository

First, clone this repository to your local machine.

bash
git clone https://github.com/yourusername/robot-control-api.git
cd robot-control-api

2. Run the Application Locally
Make sure you have the .NET 9 SDK installed. Then, run the application locally:

bash
dotnet run

The API will start running on:
https://localhost:7211

3. Open Swagger UI
Once the application is running, you can access the Swagger UI for testing the API. Open your browser and go to the following URL:

bash
https://localhost:7211/swagger/index.html

4. Authorize with JWT Token
Before interacting with the API endpoints, you need to first obtain a JWT token by calling the /login API.

-Call the /login endpoint:
-Open Swagger UI.
-Navigate to the POST /login endpoint.
-Click Try it out.
-Enter username: user and password: password.
-Click Execute.

You will receive a JWT token in the response.

Authorize with the Token:

-After obtaining the JWT token, click the Authorize button in the top right corner of the Swagger UI.
-In the "Enter 'Bearer {token}' in the field below" input, paste the JWT token you received.
-Click Authorize and then Close.

5. Test Endpoints
Once authorized, you can test the API endpoints directly in Swagger UI.

- POST /command
Submit a command to the robot:

-In the "Request body", enter a JSON payload:

json
{
    "commandText": "Move Forward",
    "robot": "Robot1",
    "user": "user"
}

-Click Execute to send the command to the API.

- GET /status
-Retrieve the current status of the robot:
-Click Execute to see the status.

### ✅ Test Coverage Summary
Test Case	Covered?
✅ Health Check API	✅ Yes
✅ Login - Valid Credentials	✅ Yes
✅ Login - Invalid Credentials	✅ Yes
✅ Unauthorized Access	✅ Yes
✅ Command - Add New	✅ Yes
✅ Command - Retrieve Existing	✅ Yes
✅ Command - Update Existing	✅ Yes
✅ Command - Get Non-Existing	✅ Yes
✅ Robot Status API	✅ Yes
✅ Command History API	✅ Yes

6. API Endpoints
POST /login: Authenticates a user and returns a JWT token. Call this first to get the JWT token.

1. POST /command: Submit a new command to the robot.
2. PUT /command: Update an existing command.
3. GET /command: Get the details of a command by ID.
4. GET /status: Get the current status of the robot.
5. GET /history: Get the command history.

7. Example API Calls
Here are examples of how to call the API using Swagger:

1. POST /login:
-Request body (JSON):
json
{
  "username": "user",
  "password": "password"
}
-Response (JWT token):
json
{
  "token": "YOUR_JWT_TOKEN"
}

2. POST /command:
-Request body (JSON):
json
{
  "commandText": "Move Forward",
  "robot": "Robot1",
  "user": "user"
}
Example Authorization header:

nginx
Bearer YOUR_JWT_TOKEN

3. GET /status:

Request URL: https://localhost:7211/status

Authorization header:

nginx
Bearer YOUR_JWT_TOKEN
8. Error Handling
If you receive a 401 Unauthorized response, make sure to check if your JWT token is valid.

If you receive a 404 Not Found response, verify the resource exists (e.g., a command by ID).

🔒 Security
This API uses JWT (JSON Web Tokens) for authentication. Make sure to keep your JWT tokens secure and never expose them publicly.

🚧 Notes
This API uses a simple in-memory store for commands and robot status. In a production environment, consider using a database for persistent storage.
The JWT token expiration is set to 1 hour. After it expires, you will need to log in again to obtain a new token.