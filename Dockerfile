# -------------------------------
# Stage 1: Build React frontend
# -------------------------------
FROM node:22 AS frontend
WORKDIR /app
COPY Frontend/package*.json ./
RUN npm install
COPY Frontend/ ./
RUN npm run build

# -------------------------------
# Stage 2: Build .NET backend
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend
WORKDIR /app
COPY Backend/*.csproj ./
RUN dotnet restore
COPY Backend/. ./
# Copy frontend build into backend build stage
COPY --from=frontend /app/dist ./dist
RUN dotnet publish -c Release -o out

# -------------------------------
# Stage 3: Runtime
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# Copy the published backend
COPY --from=backend /app/out ./
# ✅ ADD THIS LINE - Copy the dist folder to the final image
COPY --from=backend /app/dist ./dist
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Backend.dll"]