# VoiceAgent Studio

> Plataforma de agentes de IA conversacionales para campañas masivas.
> Proyecto de portafolio — .NET 8 + Angular 17 + OpenAI GPT-4o.

---

## Stack técnico

| Capa | Tecnología |
|---|---|
| Frontend | Angular 17 (standalone), Angular Material, RxJS, Signals |
| Backend | .NET 8, ASP.NET Core, Clean Architecture |
| Base de datos | SQL Server 2022, Entity Framework Core 8 |
| Auth | JWT Bearer Tokens |
| IA (Sprint 2) | OpenAI GPT-4o / Claude — streaming via SignalR |
| Infra | Docker, Docker Compose, nginx |

---

## Inicio rápido (con Docker)

```bash
# 1. Clonar el repositorio
git clone https://github.com/tu-usuario/voiceagent-studio.git
cd voiceagent-studio

# 2. Configurar API key de OpenAI
cp .env.example .env
# Edita .env y agrega tu OPENAI_API_KEY

# 3. Levantar todo
docker-compose up --build

# La app estará en:
# Frontend → http://localhost:4200
# API + Swagger → http://localhost:5000
```

**Credenciales demo:** `admin@voiceagent.dev` / `Admin1234!`

---

## Desarrollo local (sin Docker)

### Backend

```bash
cd backend

# Restaurar paquetes
dotnet restore

# Aplicar migraciones (requiere SQL Server corriendo)
cd src/VoiceAgentStudio.API
dotnet ef database update --project ../VoiceAgentStudio.Infrastructure

# Correr la API
dotnet run
# Swagger → http://localhost:5000
```

### Crear primera migración

```bash
cd backend
dotnet ef migrations add InitialCreate \
  --project src/VoiceAgentStudio.Infrastructure \
  --startup-project src/VoiceAgentStudio.API \
  --output-dir Persistence/Migrations
```

### Frontend

```bash
cd frontend

# Instalar dependencias
npm install

# Correr en modo desarrollo
ng serve
# App → http://localhost:4200
```

---

## Estructura del proyecto

```
VoiceAgentStudio/
├── backend/
│   ├── src/
│   │   ├── VoiceAgentStudio.Domain/       # Entities, Enums (sin dependencias)
│   │   ├── VoiceAgentStudio.Application/  # Use cases, DTOs, Interfaces
│   │   ├── VoiceAgentStudio.Infrastructure/ # EF Core, JWT, BCrypt
│   │   └── VoiceAgentStudio.API/          # Controllers, Middleware, Program.cs
│   └── Dockerfile
├── frontend/
│   ├── src/app/
│   │   ├── core/          # Models, Services, Guards, Interceptors
│   │   ├── features/      # Auth, Agents, Campaigns, Chat (por sprint)
│   │   └── shared/        # Componentes reutilizables
│   ├── Dockerfile
│   └── nginx.conf
└── docker-compose.yml
```

---

## API Endpoints

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login → JWT token |
| POST | `/api/auth/register` | Registro de nuevo usuario |

### Agents
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/agents` | Listar mis agentes |
| GET | `/api/agents/{id}` | Detalle de un agente |
| POST | `/api/agents` | Crear agente |
| PUT | `/api/agents/{id}` | Actualizar agente |
| DELETE | `/api/agents/{id}` | Eliminar agente (soft delete) |
| PATCH | `/api/agents/{id}/toggle-status` | Activar/Desactivar |

---

## Sprints

- [x] **Sprint 1** — Fundamentos: Auth JWT, CRUD Agentes, Angular Material UI
- [ ] **Sprint 2** — Chat Simulator con IA real (OpenAI streaming + SignalR)
- [ ] **Sprint 3** — Campañas masivas, importar contactos CSV, monitor en vivo
- [ ] **Sprint 4** — Analytics dashboard, reportes, Docker deploy

---

## Variables de entorno

```env
OPENAI_API_KEY=sk-...
```

> ⚠️ Nunca subas tu API key al repositorio. El archivo `.env` está en `.gitignore`.
