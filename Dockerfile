# -------------------------
# Stage 1 — Build
# -------------------------
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    
    # Copy csproj and restore dependencies
    COPY backend/SourceFlow.Api/SourceFlow.Api.csproj backend/SourceFlow.Api/
    RUN dotnet restore backend/SourceFlow.Api/SourceFlow.Api.csproj
    
    # Copy remaining source code
    COPY backend/SourceFlow.Api/ backend/SourceFlow.Api/
    
    # Publish the application
    RUN dotnet publish backend/SourceFlow.Api/SourceFlow.Api.csproj \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false
    
    # -------------------------
    # Stage 2 — Runtime
    # -------------------------
    FROM mcr.microsoft.com/dotnet/aspnet:8.0
    WORKDIR /app
    
    # Copy published files
    COPY --from=build /app/publish .
    
    # IMPORTANT: Render expects container to listen on 8080
    EXPOSE 8080
    
    ENTRYPOINT ["dotnet", "SourceFlow.Api.dll"]