# Frontend build stage
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY Frontend/package*.json ./
RUN npm ci
COPY Frontend/ .
ENV VITE_API_URL=http://localhost:8080
RUN npm run build

# Backend build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src
COPY Backend/*.csproj ./
RUN dotnet restore
COPY Backend/ ./
RUN dotnet publish -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=backend-build /app ./
RUN mkdir -p wwwroot
COPY --from=frontend-build /frontend/dist/. ./wwwroot/

EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]

# Expose port 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend.dll"]