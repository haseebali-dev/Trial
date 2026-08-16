# Crypto Trading Intelligence Platform

A personal-use crypto trading intelligence platform for monitoring, analyzing, and planning trades. Built with Angular, ASP.NET Core, SQLite, and local AI (Ollama).

## Architecture

```
┌─────────────────────────────────────────────┐
│              ANGULAR FRONTEND               │
│                                             │
│ Dashboard  │  Market  │  Scanner            │
│ AI Analysis│ Watchlist │ Journal            │
│ Backtesting│ Settings  │                    │
└─────────────────────┬───────────────────────┘
                      │ HTTP / WebSocket
                      ▼
┌─────────────────────────────────────────────┐
│          ASP.NET CORE WEB API               │
│                                             │
│ Market Data Service                         │
│ Technical Analysis Service                  │
│ AI Service                                   │
│ Signal/Confluence Engine                    │
│ Watchlist Service                           │
│ Journal Service                             │
│ Backtesting Service                         │
└─────────────────────┬───────────────────────┘
                      │
          ┌───────────┼────────────┐
          ▼           ▼            ▼
      SQLite       Ollama       Market APIs
```

## Tech Stack

- **Frontend**: Angular 18+, TypeScript, RxJS, TradingView Lightweight Charts
- **Backend**: ASP.NET Core 8, C#, Entity Framework Core
- **Database**: SQLite (local file)
- **AI**: Ollama (local LLM inference)
- **Market Data**: Binance API, CoinGecko API (free tiers)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)
- [Ollama](https://ollama.ai/) (for local AI)
- Git

## Getting Started

### 1. Clone and Setup

```bash
git clone <repository-url>
cd crypto-trading-platform
```

### 2. Backend Setup

```bash
cd backend/CryptoTrading.API
dotnet restore
dotnet build
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger`

### 3. Frontend Setup

```bash
cd frontend/crypto-dashboard
npm install
ng serve
```

The frontend will be available at `http://localhost:4200`

### 4. Ollama Setup (for AI features)

```bash
# Install Ollama from https://ollama.ai/
# Then pull a model:
ollama pull llama3.1:8b

# Verify it's running:
curl http://localhost:11434/api/tags
```

## Project Structure

```
crypto-trading-platform/
│
├── frontend/
│   └── crypto-dashboard/
│       ├── src/
│       │   ├── app/
│       │   │   ├── core/
│       │   │   │   ├── services/
│       │   │   │   ├── models/
│       │   │   │   ├── guards/
│       │   │   │   └── interceptors/
│       │   │   │
│       │   │   ├── shared/
│       │   │   │   ├── components/
│       │   │   │   ├── pipes/
│       │   │   │   └── directives/
│       │   │   │
│       │   │   ├── features/
│       │   │   │   ├── dashboard/
│       │   │   │   ├── market/
│       │   │   │   ├── scanner/
│       │   │   │   ├── ai-analysis/
│       │   │   │   ├── watchlist/
│       │   │   │   ├── journal/
│       │   │   │   ├── backtesting/
│       │   │   │   └── settings/
│       │   │   │
│       │   │   ├── app.routes.ts
│       │   │   └── app.config.ts
│       │   │
│       │   └── assets/
│       └── package.json
│
├── backend/
│   └── CryptoTrading.API/
│       ├── Controllers/
│       ├── Services/
│       ├── Interfaces/
│       ├── Models/
│       ├── DTOs/
│       ├── Data/
│       ├── Repositories/
│       ├── Indicators/
│       ├── AI/
│       ├── MarketData/
│       ├── Backtesting/
│       ├── Configuration/
│       ├── Middleware/
│       ├── Program.cs
│       └── appsettings.json
│
├── docs/
├── scripts/
├── docker/
├── README.md
└── .gitignore
```

## Features (Phase 1 - Foundation Complete)

✅ Project structure created
✅ ASP.NET Core Web API with SQLite + EF Core
✅ Angular 18 standalone components with routing
✅ Health check endpoint (`GET /api/health`)
✅ CORS configured for Angular dev server
✅ Global error handling middleware
✅ Database models for all entities
✅ Configuration classes for settings

## Development Phases

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Project Foundation | ✅ Complete |
| 2 | Market Data | 🔄 Next |
| 3 | Chart | ⏳ |
| 4 | Technical Indicators | ⏳ |
| 5 | Market Bias | ⏳ |
| 6 | Confluence Engine | ⏳ |
| 7 | AI System (Ollama) | ⏳ |
| 8 | Main Dashboard | ⏳ |
| 9 | Watchlist | ⏳ |
| 10 | Market Scanner | ⏳ |
| ... | ... | ⏳ |

## Configuration

### Backend (appsettings.json)

Key settings:
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `MarketData` - API endpoints, refresh rates
- `AI` - Ollama URL, default model, temperature
- `Scanner` - Default symbols, scan intervals

### Frontend (environment.ts)

- `apiUrl` - Backend API base URL

## License

Personal use only.