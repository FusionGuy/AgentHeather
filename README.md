# Agent Heather — HR Policy Chat Assistant

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Azure App Service](https://img.shields.io/badge/Azure-App%20Service-blue)](https://azure.microsoft.com/services/app-service/)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-green)](https://azure.microsoft.com/products/ai-services/openai-service)

**Agent Heather** is an ASP.NET Core Razor Pages web application that serves as an AI-powered HR policy assistant. It ingests HR policy documents and uses Azure OpenAI (GPT) with a TF-IDF retrieval-augmented generation (RAG) pipeline to answer employee questions about HR policies and procedures.

🌐 **Live Demo**: [https://heather-demo-chat.azurewebsites.net](https://heather-demo-chat.azurewebsites.net)

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [How It Works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Building & Running Locally](#building--running-locally)
- [Deployment](#deployment)
- [Suggested Improvements](#suggested-improvements)

---

## Features

- **Document Library** — Browse pre-loaded HR policy documents with metadata (title, filename, page count, ingestion date) and view full content in modals
- **Single-Question Q&A** — Ask a one-off question on the home page and get a cited answer from Agent Heather
- **Multi-Turn Chat** — Full conversational chat interface with session-based message history
- **TF-IDF Retrieval** — Lightweight in-memory retrieval engine that chunks documents and ranks them by cosine similarity to find the most relevant context
- **Azure OpenAI Integration** — Sends retrieved context + user question to Azure OpenAI's Responses API for grounded, cited answers
- **Responsive UI** — Bootstrap 5.3 layout with a custom Heather avatar (SVG)

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Browser (User)                    │
│                                                     │
│  ┌──────────┐  ┌──────────┐  ┌───────────────────┐  │
│  │ Index    │  │ Chat     │  │ Privacy / Error   │  │
│  │ (Q&A)   │  │ (Multi-  │  │                   │  │
│  │          │  │  turn)   │  │                   │  │
│  └────┬─────┘  └────┬─────┘  └───────────────────┘  │
└───────┼──────────────┼───────────────────────────────┘
        │              │
        ▼              ▼
┌─────────────────────────────────────────────────────┐
│              ASP.NET Core Razor Pages                │
│                                                     │
│  ┌──────────────┐  ┌────────────────────────────┐   │
│  │ AgentService │◄─┤ TfIdfRetrievalService      │   │
│  │              │  │  • Chunk documents (700ch)  │   │
│  │  • Build     │  │  • TF-IDF vectorization     │   │
│  │    prompt    │  │  • Cosine similarity ranking │   │
│  │  • Call API  │  └────────────┬───────────────┘   │
│  │  • Parse     │               │                   │
│  │    response  │  ┌────────────┴───────────────┐   │
│  └──────┬───────┘  │ PdfService                 │   │
│         │          │  • In-memory document store │   │
│         │          │  • 4 pre-loaded HR manuals  │   │
│         │          └────────────────────────────┘   │
└─────────┼───────────────────────────────────────────┘
          │
          ▼
┌─────────────────────┐
│   Azure OpenAI      │
│   Responses API     │
│   (GPT-5.2-chat)    │
└─────────────────────┘
```

---

## Project Structure

```
HeatherDemoApp/
├── Models/
│   ├── ChatMessage.cs          # Chat message model (role + content)
│   └── PdfDocument.cs          # PDF document model (id, title, content, metadata)
├── Pages/
│   ├── Shared/
│   │   ├── _Layout.cshtml      # Main layout (Bootstrap CDN, inline SVG avatar, nav)
│   │   ├── _Layout.cshtml.css  # Scoped layout styles
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── _ViewImports.cshtml     # Tag helper imports
│   ├── _ViewStart.cshtml       # Default layout assignment
│   ├── Index.cshtml / .cs      # Home page — document library + single Q&A
│   ├── Chat.cshtml / .cs       # Multi-turn chat interface with session history
│   ├── Privacy.cshtml / .cs    # Privacy policy page
│   └── Error.cshtml / .cs      # Error page
├── Services/
│   ├── AgentService.cs         # Azure OpenAI integration, prompt building, response parsing
│   ├── PdfService.cs           # In-memory document store with 4 pre-loaded HR manuals
│   └── RetrievalService.cs     # TF-IDF retrieval engine (chunking, vectorization, ranking)
├── wwwroot/
│   ├── css/site.css            # Site-wide styles
│   ├── images/heather-avatar.svg  # Heather avatar SVG
│   ├── js/site.js              # Client-side JavaScript
│   └── lib/                    # Bootstrap 5.3, jQuery 3.x, jQuery Validation
├── Program.cs                  # App startup, DI registration, middleware pipeline
├── appsettings.json            # Azure AI config, target site config
├── appsettings.Development.json # Development overrides
├── deploy.ps1                  # PowerShell deployment script (publish → zip → Azure)
├── verify.ps1                  # Post-deployment verification script
├── HeatherDemoApp.csproj       # Project file (.NET 10)
└── HeatherDemoApp.sln          # Solution file
```

---

## How It Works

### 1. Document Ingestion

At startup (`Program.cs`), the app initializes services in sequence:

1. **`PdfService.InitializeAsync()`** — Loads 4 pre-hardcoded HR policy documents into memory (ENSO Group HR Manual, Community Foundations of Canada HR Guide, GESCI HR Policies, RHA Health Services HR Policy Manual)
2. **`TfIdfRetrievalService.InitializeAsync(docs)`** — Chunks each document into ~700-character segments with 150-character overlap, then builds a document-frequency index across all chunks for TF-IDF scoring

### 2. Question Answering (RAG Pipeline)

When a user asks a question:

1. **Retrieval** — The `TfIdfRetrievalService` tokenizes the query, computes TF-IDF vectors, and ranks all chunks by cosine similarity to find the top 5 most relevant passages
2. **Prompt Construction** — The `AgentService` builds a system prompt instructing the model to answer only from provided context, then assembles the conversation history + retrieved context + user question
3. **Azure OpenAI Call** — Sends the assembled messages to Azure OpenAI's Responses API (`gpt-5.2-chat`)
4. **Response Parsing** — Parses the response (handling multiple API response shapes) and returns the answer with document citations

### 3. Chat Session Management

The Chat page (`Chat.cshtml.cs`) uses ASP.NET Core's session middleware (`DistributedMemoryCache` + `Session`) to persist conversation history as serialized JSON, enabling multi-turn conversations within a browser session.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An [Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service) resource with a deployed model
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (for deployment only)

---

## Configuration

### `appsettings.json`

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/openai/responses?api-version=2025-04-01-preview",
    "Model": "gpt-5.2-chat",
    "ApiKey": "YOUR_API_KEY_HERE"
  },
  "TargetSite": {
    "Url": "https://your-site.com",
    "Name": "Your Organization",
    "ShortName": "Your Org",
    "Description": "Description of your organization"
  }
}
```

> **⚠️ Security Note**: Never commit API keys to source control. The `AgentService` also checks the `AZURE_OPENAI_API_KEY` environment variable, which takes precedence over the config file. Use environment variables or Azure App Service application settings in production.

---

## Building & Running Locally

```bash
# Clone the repository
git clone https://github.com/FusionGuy/AgentHeather.git
cd AgentHeather

# Set your API key (option 1: environment variable — recommended)
export AZURE_OPENAI_API_KEY="your-key-here"

# Or edit appsettings.json with your key (option 2 — local dev only)

# Build and run
dotnet run
```

The app starts on **http://localhost:80** by default. To use a custom port:

```bash
# Set PORT environment variable
set PORT=5046    # Windows cmd
$env:PORT=5046   # PowerShell
export PORT=5046 # Linux/macOS

dotnet run
```

Or use the `--urls` flag:

```bash
dotnet run --urls http://localhost:5046
```

---

## Deployment

The app is deployed to **Azure App Service** (Linux) via zip deploy. A PowerShell script automates the process:

```powershell
# Ensure you're logged in to Azure CLI
az login

# Run the deployment script
.\deploy.ps1
```

### What `deploy.ps1` Does

1. **Publish** — `dotnet publish` with `Release` config, targeting `linux-x64` (framework-dependent)
2. **Zip** — Creates a zip archive of the publish output
3. **Deploy** — Uploads the zip to Azure App Service via the Kudu ZipDeploy API using an Azure management token
4. **Restart** — Restarts the app service to pick up the new deployment

### Post-Deployment Verification

```powershell
.\verify.ps1
```

Waits 30 seconds for the app to start, then checks if the homepage returns HTTP 200 and contains the expected content.

---

## Suggested Improvements

### High Priority

1. **Move secrets to Azure Key Vault or App Settings** — The API key should be stored in Azure Key Vault or as an App Service environment variable, never in `appsettings.json`

2. **Replace hardcoded documents with real PDF ingestion** — Currently, HR documents are hardcoded strings in `PdfService.cs`. Integrate with Azure Blob Storage + Azure Document Intelligence to ingest actual PDF files dynamically

3. **Use vector embeddings instead of TF-IDF** — Replace the TF-IDF retrieval with Azure OpenAI Embeddings (e.g., `text-embedding-3-large`) for more accurate semantic search. Store embeddings in Azure AI Search or a vector database

4. **Fix static file serving** — The .NET 10 `MapStaticAssets()` system doesn't serve `wwwroot` files correctly on Azure App Service Linux. Currently worked around by using CDN for Bootstrap/jQuery and inline SVG. Investigate the root cause (likely a content root path mismatch)

### Medium Priority

5. **Add persistent chat storage** — Replace in-memory session storage with a database (e.g., Azure Cosmos DB or SQL) to persist conversations across sessions and restarts

6. **Streaming responses** — Implement Server-Sent Events (SSE) or SignalR to stream Azure OpenAI responses token-by-token for a more responsive chat experience

7. **Add authentication** — Integrate Azure AD / Entra ID for user authentication and role-based access

8. **Implement proper logging and monitoring** — Add Application Insights for telemetry, request tracing, and error monitoring

9. **Add error handling UI** — Show user-friendly error messages when the Azure OpenAI call fails or times out

### Low Priority

10. **Add document management UI** — Allow admins to upload, edit, and delete HR documents through the web interface

11. **Multi-language support** — Add localization for non-English users

12. **Dark mode** — Add a theme toggle for dark/light mode

13. **Export chat history** — Allow users to download their chat conversations as PDF or text

14. **Rate limiting** — Implement request rate limiting to prevent abuse of the AI endpoint

15. **Unit and integration tests** — Add test coverage for services (AgentService, RetrievalService, PdfService) and page models

---

## License

This project is for demonstration purposes.
