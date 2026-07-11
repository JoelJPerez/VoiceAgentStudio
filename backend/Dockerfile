# ── Build stage ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/VoiceAgentStudio.API/VoiceAgentStudio.API.csproj", "src/VoiceAgentStudio.API/"]
COPY ["src/VoiceAgentStudio.Application/VoiceAgentStudio.Application.csproj", "src/VoiceAgentStudio.Application/"]
COPY ["src/VoiceAgentStudio.Domain/VoiceAgentStudio.Domain.csproj", "src/VoiceAgentStudio.Domain/"]
COPY ["src/VoiceAgentStudio.Infrastructure/VoiceAgentStudio.Infrastructure.csproj", "src/VoiceAgentStudio.Infrastructure/"]

RUN dotnet restore "src/VoiceAgentStudio.API/VoiceAgentStudio.API.csproj"

COPY . .
RUN dotnet publish "src/VoiceAgentStudio.API/VoiceAgentStudio.API.csproj" \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VoiceAgentStudio.API.dll"]
