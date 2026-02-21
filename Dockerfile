# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/SourceFlow.Api/SourceFlow.Api.csproj backend/SourceFlow.Api/
RUN dotnet restore backend/SourceFlow.Api/SourceFlow.Api.csproj

COPY backend/SourceFlow.Api/ backend/SourceFlow.Api/
RUN dotnet publish backend/SourceFlow.Api/SourceFlow.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SourceFlow.Api.dll"]
