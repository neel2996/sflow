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
    
    # Copy published files from build stage
    COPY --from=build /app/publish .
    
    # Render requires apps to listen on port 10000
    ENV ASPNETCORE_URLS=http://+:10000
    EXPOSE 10000
    
    # Start the API
    ENTRYPOINT ["dotnet", "SourceFlow.Api.dll"]