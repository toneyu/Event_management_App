# Origin Technical Exercise — Event Management API & SPA

A full-stack Event Management & Attendee Registration application built for the Origin Technical Exercise.

- **Backend (`EventBE`)**: C# / ASP.NET Core (.NET 10) Web API with thread-safe in-memory storage, OpenAPI/Swagger documentation, and xUnit unit tests.
- **Frontend (`EventUI`)**: React 18 + TypeScript + Vite Single Page Application (SPA).

---

## Features & Business Rules Implemented

### 1. Event Management (CRUD)
- **Create Event**: Specify `title`, `description`, `date`, and `maxCapacity`.
- **Read Events**: List all events ordered by date (including calculated `registeredCount`, `availableSpots`, `isPast`, and `isFull` flags) or fetch a single event with its full attendee list.
- **Update Event**: Modify event details. Prevents reducing `maxCapacity` below the current number of registered attendees.
- **Delete Event**: Remove an event and its registrations.

### 2. Attendee Registrations & Business Rules
- **Register Attendee**: Register a user (`userName` + `userEmail`) for an event.
- **Unregister Attendee**: Remove a user's registration by email, freeing up event capacity.
- **Enforced Business Rules**:
  1. **No Past Event Registrations**: Rejects registrations when the event's `Date` is in the past (`400 Bad Request`).
  2. **Capacity Enforcement**: Rejects registrations when `registeredCount >= maxCapacity` (`409 Conflict`).
  3. **No Duplicate Registrations**: Rejects duplicate registrations for the same user email (case-insensitive) on the same event (`409 Conflict`).

---

## Project Structure

```text
Origin_assessment/
├── EventBE/
│   ├── EventBE.sln
│   ├── EventBE.Api/                 # ASP.NET Core 8 Web API
│   │   ├── Common/ServiceResult.cs  # Result pattern for clean business logic
│   │   ├── Controllers/             # REST API endpoints (/api/events)
│   │   ├── DTOs/                    # Request & response contracts
│   │   ├── Models/                  # In-memory domain entities
│   │   ├── Services/                # Thread-safe EventService & business rules
│   │   └── Program.cs               # DI, CORS, Swagger, & seed data setup
│   └── EventBE.Tests/               # xUnit unit test project
│       └── EventServiceTests.cs     # Unit tests for CRUD & all 3 business rules
├── EventUI/                         # React (Vite + TypeScript) SPA
│   ├── src/
│   │   ├── api/eventsApi.ts         # Typed REST client
│   │   ├── components/              # EventDetailPanel & EventFormModal
│   │   ├── types/event.ts           # TypeScript interfaces
│   │   └── App.tsx                  # Dashboard, KPI metrics, filtering, & state
│   └── vite.config.ts
└── README.md
```

---

## Prerequisites

- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Node.js 18+** & **npm** ([Download](https://nodejs.org/))

---

## Getting Started (Running Locally)

### 1. Run the Backend API (`EventBE`)

Open a terminal in the repository root and run:

```bash
cd EventBE
dotnet run --project EventBE.Api
```

- The API will start at **`http://localhost:5150`**.
- Interactive **Swagger UI** is available at **`http://localhost:5150/swagger`**.
- On startup, 4 sample events are seeded in memory (upcoming open events, an event at max capacity, and a past event) so you can immediately test all business rules.

### 2. Run Backend Unit Tests

```bash
cd EventBE
dotnet test
```

### 3. Run the Frontend Application (`EventUI`)

Open a second terminal in the repository root and run:

```bash
cd EventUI
npm install
npm run dev
```

- Open **`http://localhost:5173`** in your browser.

---

## REST API Endpoints Summary

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/events` | List all events ordered by date |
| `GET` | `/api/events/{id}` | Get a single event and its attendee registrations |
| `POST` | `/api/events` | Create a new event (`{ title, description, date, maxCapacity }`) |
| `PUT` | `/api/events/{id}` | Update an existing event |
| `DELETE` | `/api/events/{id}` | Delete an event |
| `POST` | `/api/events/{id}/registrations` | Register an attendee (`{ userName, userEmail }`) |
| `DELETE` | `/api/events/{id}/registrations/{userEmail}` | Unregister an attendee by email |

