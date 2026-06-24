# SmartEdu RAG System

## Overview

SmartEdu RAG System is an AI-powered educational platform developed using ASP.NET Core Razor Pages. The system integrates Retrieval-Augmented Generation (RAG) techniques to provide intelligent question answering and educational support by combining Large Language Models (LLMs) with a knowledge base.

## Features

* User authentication and authorization.
* Course and learning material management.
* AI-powered question answering using RAG architecture.
* Retrieval of relevant educational documents before generating responses.
* Semantic search for learning resources.
* Responsive user interface built with Razor Pages.
* Layered architecture for maintainability and scalability.

---

## Project Architecture

The solution is organized into multiple layers following Clean Architecture principles:

```text
SmartEdu_RAG_System_RazorPage_Group06
│
├── SmartEdu.RazorWeb       # Presentation Layer (ASP.NET Core Razor Pages)
├── SmartEdu.Business       # Business Logic Layer
├── SmartEdu.DataAccess     # Data Access Layer
├── SmartEdu.Shared         # Shared Models, DTOs, Utilities
└── SmartEdu.RazorWeb.slnx  # Solution File
```

### Layer Responsibilities

| Project                 | Responsibility                                                  |
| ----------------------- | --------------------------------------------------------------- |
| **SmartEdu.RazorWeb**   | Handles UI, Razor Pages, authentication, and user interactions. |
| **SmartEdu.Business**   | Contains business rules and application services.               |
| **SmartEdu.DataAccess** | Manages database access and repositories.                       |
| **SmartEdu.Shared**     | Stores shared models, DTOs, constants, and helper classes.      |

---

## Technologies Used

* ASP.NET Core Razor Pages
* Entity Framework Core
* SQL Server
* C#
* Dependency Injection
* Repository Pattern
* Retrieval-Augmented Generation (RAG)
* Large Language Models (LLMs)

---

## Prerequisites

Before running the project, ensure that you have installed:

* Visual Studio 2022 or later
* .NET SDK 8.0 (or compatible version)
* SQL Server / SQL Server Express
* SQL Server Management Studio (Optional)

---

## Getting Started

### 1. Clone Repository

```bash
git clone <repository-url>
cd SmartEdu_RAG_System_RazorPage_Group06
```

### 2. Open the Solution

Open the solution file in Visual Studio:

```text
SmartEdu.RazorWeb.slnx
```

---

## Database Configuration

Open:

```text
SmartEdu.RazorWeb/appsettings.json
```

Update the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=SmartEduDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

---

## Restore Dependencies

Using Visual Studio:

* Right-click the solution.
* Select **Restore NuGet Packages**.

Or use CLI:

```bash
dotnet restore
```

---

## Apply Database Migration

Open Package Manager Console and execute:

```powershell
Update-Database
```

Or using .NET CLI:

```bash
dotnet ef database update
```

---

## Running the Application

### Using Visual Studio

1. Set **SmartEdu.RazorWeb** as the Startup Project.
2. Press **F5** or click **Start**.

### Using Command Line

```bash
cd SmartEdu.RazorWeb
dotnet run
```

The application will be available at:

```text
https://localhost:xxxx
```

---

## AI RAG Workflow

The system follows the Retrieval-Augmented Generation pipeline:

1. User submits a question.
2. The system searches relevant documents from the knowledge base.
3. Retrieved documents are passed as context to the LLM.
4. The AI generates an accurate and context-aware response.
5. The response is returned to the user.

---

## Future Improvements

* Integrate vector databases (Pinecone, ChromaDB, FAISS).
* Support multiple LLM providers.
* Implement chat history and conversation memory.
* Add role-based access control.
* Enhance document ingestion and indexing.

---

## Contributors

* Group 06

---

## License

This project is developed for educational purposes.
