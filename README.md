# ChatApp - .NET Real-time Chat Application

# ⚠️📌 For a full architecture overview and developer guide, see docs/ARCHITECTURE.md. ⚠️📌

A modern, real-time chat application built with ASP.NET Core, SignalR, and RabbitMQ featuring live messaging, user authentication with Identity, and stock quote integration.

## 🚀 Features

### ✅ Core Chat Features (Always Available)
- **Real-time messaging** between multiple users via SignalR
- **User registration and authentication** with ASP.NET Core Identity
- **Online user tracking** with live status updates
- **Message history persistence** using Entity Framework Core
- **Join/leave notifications** for user activity
- **Responsive UI** with Bootstrap 5 and modern design

### ✅ Stock Quote Features (Requires RabbitMQ + Bot)
- **Stock command detection** (`/stock=SYMBOL` format)
- **Real-time stock quotes** via Stooq API integration
- **Bot service** that processes commands asynchronously
- **Broadcast responses** to all users in the chat
- **Graceful degradation** if external services unavailable

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- Internet connection (for CDN dependencies)
- Docker running on your machine (WSL on Windows)

### Start RabbitMQ in Docker (Use WSL on Windows)
```bash
# Navigate to the project directory (root folder) and in a new terminal run:
docker-compose up -d
```
- **RabbitMQ Management**: `http://localhost:15672` (admin/password123)

### Running the Web App
1. Navigate to the project directory
2. Run: `dotnet run --project src/ChatApp.Web`
3. Open: `http://localhost:5281`

#### Step 2: Start the Bot Service
1. Navigate to the project directory
2. Run: `dotnet run --project src/ChatApp.Bot`

### Testing Multiple Users
- Open different browsers (Chrome, Edge, Firefox)
- Register different user accounts in each browser
- Navigate to Chat page to test real-time messaging and stock quote features

### Dependencies
- Uses CDN for SignalR and Font Awesome (requires internet)
- SQLite database is created automatically
- RabbitMQ for stock quote features

### Features Working
✅ User registration and authentication
✅ Real-time messaging between multiple users  
✅ Online user tracking
✅ Message history
✅ Broadcast Stock Quote to all online users
