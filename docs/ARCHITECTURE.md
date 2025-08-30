# ChatApp Architecture and Developer Guide

This document provides a complete overview of the ChatApp solution architecture, the roles of each project, the main runtime flows, and guidance for development and operations. It also includes text-based diagrams that render well in GitHub and IDEs.

Date: 2025-08-30

- Solution: ChatApp.sln
- Target Framework: .NET 9
- Major Building Blocks:
  - ChatApp.Web (ASP.NET Core Web App + SignalR)
  - ChatApp.Infrastructure (EF Core, Identity, RabbitMQ, Integrations)
  - ChatApp.Core (Domain + Interfaces + DTOs + Validation)
  - ChatApp.Bot (Console background worker that processes stock commands)


## 1. High-Level Overview

```
+--------------------+        HTTP/SignalR        +---------------------+
|     Web Clients    | <------------------------> |     ChatApp.Web     |
|  (Browsers/SPA)    |                           |  (ASP.NET Core)     |
+--------------------+                           +----------+----------+
                                                            |
                                                            | DI + Interfaces
                                                            v
                                                +-----------+-----------+
                                                |    ChatApp.Core       |
                                                |  (Domain + Abstractions)
                                                +-----------+-----------+
                                                            |
                                                            | Implementations
                                                            v
                                                +-----------+-----------+
                                                | ChatApp.Infrastructure |
                                                |  EF Core, Identity,    |
                                                |  RabbitMQ, HTTP, etc.  |
                                                +-----------+-----------+
                                                            ^
                                                            |
                                           RabbitMQ Messages |
                                                            |
+--------------------+                           +----------+----------+
|   External API     | <---- HTTP (Stooq) -----> |   ChatApp.Bot       |
|  (Stooq Quotes)    |                           |  (Console Worker)   |
+--------------------+                           +---------------------+
```

Key ideas:
- ChatApp.Web provides the UI and SignalR hub for real-time chat and quote broadcasting.
- Users submit messages; stock commands are offloaded via IMessageBroker to RabbitMQ.
- ChatApp.Bot consumes commands, fetches quotes from Stooq (via Flurl.Http), and publishes results back.
- The web app receives stock quote events and broadcasts them through SignalR to all connected clients.
- Persistence: EF Core (SQLite) stores users and chat history. Identity handles authN/authZ.


## 2. Projects and Responsibilities

### ChatApp.Core
- Purpose: Domain model, abstractions, and validation.
- Typical contents (based on tests and references):
  - Entities: ApplicationUser, ChatMessage, etc.
  - Interfaces: IChatRepository, IMessageBroker, IStockService, IStockQuoteBroadcaster, IStockQuoteHandlerService…
  - DTOs + FluentValidation validators (e.g., StockQuoteDtoValidator).
- No infrastructure dependencies. Referenced by all other projects.

### ChatApp.Infrastructure
- Purpose: Implementations for persistence, integration, and messaging.
- Packages: EF Core (Sqlite), Identity EFCore, Flurl.Http, RabbitMQ.Client, Options, Hosting.
- Likely components (inferred from tests and DI setup):
  - Data: DbContext for chat messages + Identity schema (Sqlite).
  - Repositories: ChatRepository implementing IChatRepository.
  - Messaging: RabbitMq message broker implementing IMessageBroker.
  - Services: StockService (HTTP client to Stooq), StockQuoteHandlerService (subscribes/consumes messages and forwards via IStockQuoteBroadcaster).

### ChatApp.Web
- Purpose: Web front-end with MVC/Razor Pages + SignalR hub.
- Uses Identity UI for registration/login.
- Contains the SignalR ChatHub and a SignalRStockQuoteBroadcaster that adapts stock quote events to the hub.
- References Core + Infrastructure for DI registrations and service wiring.

### ChatApp.Bot
- Purpose: Console app hosting a background service to consume stock commands, call stock quote API, and publish results back to RabbitMQ.
- References Core + Infrastructure so it can reuse the same abstractions and implementations (message broker, stock service, DTO validation).


## 3. Runtime Flows

### 3.1 Real-Time Chat Message Flow (non-stock)

```
User -> SignalR -> ChatHub.SendMessage(message)
  - Validates identity
  - Persists ChatMessage via IChatRepository (scoped from DI)
  - Broadcasts via Clients.All.SendAsync("ReceiveMessage", username, message, timestamp)
```

Data Path:
- Web client sends message to SignalR hub.
- Hub saves message to database via repository (Infrastructure).
- Hub broadcasts message to all connected clients.

### 3.2 Stock Command Flow

```
User types: /stock=AAPL
    |
    v
ChatHub.SendMessage detects `/stock=`
    - Publishes stock command via IMessageBroker (RabbitMQ)
    - Acknowledges to caller: "Looking up stock quote…"

ChatApp.Bot subscribes to commands
    - Fetches quote from Stooq (StockService)
    - Publishes stock quote event via IMessageBroker

ChatApp.Web subscribes to stock quote events (StockQuoteHandlerService)
    - Validates and converts to message
    - Uses IStockQuoteBroadcaster to broadcast

SignalRStockQuoteBroadcaster -> Clients.All.SendAsync("ReceiveStockQuote", botUsername, quote, timestamp)
```

Notes:
- The web app remains responsive even if RabbitMQ is down (hub handles exceptions and informs the caller).
- When RabbitMQ is disabled/unavailable, chat still works; only stock features are degraded.

### 3.3 Online Users Tracking

- ChatHub keeps a static concurrent dictionary mapping connectionId -> username.
- On connection join/leave:
  - Adds/removes entries and updates ApplicationUser.IsOnline in database when appropriate.
  - Broadcasts UserJoined/UserLeft and UpdateOnlineUsers to the ChatRoom group.


## 4. Key Components (with code references)

### 4.1 ChatHub (ChatApp.Web/Hubs/ChatHub.cs)
- Authorize attribute gates access to authenticated users.
- SendMessage:
  - Detects `/stock=` commands and delegates to IMessageBroker.
  - Persists normal messages through IChatRepository.
  - Broadcasts ReceiveMessage to all clients.
- JoinChat / OnConnectedAsync / OnDisconnectedAsync:
  - Manages online users and emits notifications.

### 4.2 SignalRStockQuoteBroadcaster (ChatApp.Web/Hubs/SignalRStockQuoteBroadcaster.cs)
- Implements IStockQuoteBroadcaster.
- Broadcasts stock quotes to all connected clients using hub context.

### 4.3 Infrastructure Services (inferred by tests)
- ChatRepository: Saves/fetches ChatMessage.
- StockService: Calls Stooq API using Flurl.Http.
- RabbitMqMessageBroker: Publishes and consumes commands/events.
- StockQuoteHandlerService: Listens for quote events and pushes to IStockQuoteBroadcaster.


## 5. Data Model (Domain-level)

Core Entities (expected):
- ApplicationUser
  - IdentityUser fields + IsOnline (bool) used by hub to track status.
- ChatMessage
  - Id, UserId, Username, Content, Timestamp, IsStockQuote.

Storage: SQLite database via EF Core.


## 6. Dependency Injection and Hosting

- Web: Registers DbContext, Identity, repositories, message broker, hosted services (e.g., quote handler), SignalR.
- Bot: Registers message broker, stock service, hosted background worker to process commands.
- Abstractions in Core allow Web and Bot to swap implementations if needed (e.g., in-memory broker for tests).


## 7. Public Contracts and Events

Interfaces (Core):
- IChatRepository
  - AddMessageAsync, GetRecentAsync, etc.
- IMessageBroker
  - PublishStockCommandAsync(stockCode, requestedBy)
  - PublishStockQuoteAsync(quoteDto)
  - SubscribeStockCommands / SubscribeStockQuotes (implementation-specific in Infrastructure)
- IStockService
  - GetQuoteAsync(symbol)
- IStockQuoteBroadcaster
  - BroadcastStockQuoteAsync(username, quoteText, timestamp)

Event payloads (DTOs):
- StockQuoteDto: symbol, price, when, provider, errors.


## 8. Testing Strategy (from tests directory)

- Unit tests cover:
  - ChatHub behaviors (command handling, messaging, online users)
  - SignalRStockQuoteBroadcaster broadcasting contract
  - Validators in Core (FluentValidation)
  - Repository and broker lifecycle in Infrastructure
  - StockService integration (HTTP call parsing)
- Integration tests validate the full stock command flow through RabbitMQ boundaries.


## 9. Operations

### 9.1 Running Locally
- Web: `dotnet run --project src/ChatApp.Web`
- Bot: `dotnet run --project src/ChatApp.Bot`
- RabbitMQ: `docker-compose up -d` from solution root.
- Web UI default URL: see console output (e.g., http://localhost:5281).
- RabbitMQ Management: http://localhost:15672 (user: admin, password: password123).

### 9.2 Environment Variables / Configuration (typical)
- Connection strings for SQLite or other RDBMS.
- RabbitMQ host, port, username, password.
- External API base URLs.
- See appsettings.json in each project and override with environment variables if needed.


## 10. Security Considerations
- ASP.NET Core Identity for authentication and authorization.
- Only authenticated users can connect to ChatHub.
- Server validates and sanitizes messages; consider applying further content validation for production (max length, HTML encoding on UI, etc.).
- Secrets should not be committed; use user-secrets or environment variables.


## 11. Extensibility
- Swap RabbitMQ with another broker by implementing IMessageBroker.
- Replace Stooq with another data provider by implementing IStockService.
- Add new message types via Core DTOs and handlers.
- Front-end can be upgraded to React/Vue using the same SignalR endpoints.


## 12. Troubleshooting
- If stock commands do not respond:
  - Ensure RabbitMQ container is running (management UI reachable).
  - Check bot console logs for errors reaching Stooq.
  - Verify the web app registered the StockQuoteHandler hosted service.
- If users do not appear online:
  - Confirm SignalR connection established (browser console).
  - Review database for ApplicationUser.IsOnline flag updates.


## 13. Diagrams

### 13.1 Component Diagram
```
+------------------+            +-------------------+
|   ChatApp.Web    | uses       | ChatApp.Core      |
| - SignalR Hub    +----------->| - Interfaces      |
| - Identity UI    |            | - Entities/DTOs   |
+--------+---------+            +---------+---------+
         |                                ^
         | implements                     |
         v                                |
+--------+---------+            +---------+---------+
| ChatApp.Infrastructure|<------+   ChatApp.Bot     |
| - EF Core/Identity    | uses  | - Background Svc  |
| - RabbitMQ Client     |       | - Uses Core       |
| - HTTP Integrations   |       +-------------------+
+-----------------------+
```

### 13.2 Sequence: Stock Command
```
User           ChatHub           IMessageBroker        Bot Worker       StockService      QuoteHandler     Broadcaster     Clients
 |   /stock=AAPL  |                      |                  |                |                 |               |              |
 |--------------->|                      |                  |                |                 |               |              |
 |                | Publish stock cmd    |                  |                |                 |               |              |
 |                |--------------------->|                  |                |                 |               |              |
 |  Ack caller    |                      |                  |                |                 |               |              |
 |<---------------|                      |                  |                |                 |               |              |
 |                |                      | Cmd received     |                |                 |               |              |
 |                |                      |----------------->|                |                 |               |              |
 |                |                      |                  | Get quote      |                 |               |              |
 |                |                      |                  |--------------->|                 |               |              |
 |                |                      |                  |  Quote DTO     |                 |               |              |
 |                |                      |                  |<---------------|                 |               |              |
 |                |                      | Publish event    |                |                 |               |              |
 |                |                      |<-----------------|                |                 |               |              |
 |                |                      |                  |                | Quote consumed  |               |              |
 |                |                      |                  |                |---------------->|               |              |
 |                |                      |                  |                |                 | Broadcast     |              |
 |                |                      |                  |                |                 |-------------> |-------------->
```


## 14. Glossary
- Hub: SignalR component that handles real-time messages.
- Broadcaster: Adapter that sends domain events to SignalR clients.
- Broker: Message queue abstraction (RabbitMQ implementation provided).
- Bot: Background application processing asynchronous tasks.


## 15. References
- ASP.NET Core SignalR
- FluentValidation
- Entity Framework Core with SQLite
- RabbitMQ .NET Client
- Flurl.Http

