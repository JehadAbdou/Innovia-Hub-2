# Innovia Hub – Intranät och bokningssystem

Detta repo innehåller projektarbetet för kursuppgiften **Innovia Hub**.

## Om uppgiften

Innovia Hub är ett intranät och bokningssystem för coworkingcentret Innovia Hub. Systemet är byggt för att underlätta vardagen för både medlemmar och administratörer.

För användaren
- Medlemmar kan logga in och boka resurser i realtid, som skrivbord, mötesrum, VR-headsets och AI-servrar.
- Systemet visar aktuellt tillgängliga tider och uppdateras automatiskt via SignalR när någon annan gör en bokning – användaren ser direkt om en tid blir upptagen.
- En responsiv och enkel frontend gör att systemet kan användas på dator, surfplatta och mobil.

För administratören
- Administratörer har en egen panel där de kan hantera användare, resurser och bokningar.
- De kan aktivera/inaktivera resurser, ta bort bokningar eller uppdatera information om medlemmar.
- admin kan också se sensorer och ändra konfiguration av sensorana.
- All data hanteras via ett API som backend tillhandahåller.

Tekniska funktioner
- Backend är byggt i ASP.NET Core med Entity Framework Core och Identity för autentisering och behörigheter.
- Bokningar och användare lagras i en SQL-databas (MySQL).
- Realtidskommunikation sker med SignalR, vilket gör att alla användare får live-uppdateringar utan att behöva ladda om sidan.
- Frontend är byggd i React (Vite) och kommunicerar med backend via ett REST API och en SignalR-klient.


## Vår Stack

- **Backend:** ASP.NET Core (C#)
- **Frontend:** React (Vite)
- **Databas:** SQL (MySQL)
- **Realtidskommunikation:** SignalR
- **API (framtid):** Mockat sensor-API

---

## Kom igång – Installation

### Alternativ 1: Med Docker (rekommenderas)

Krav:
- Docker Desktop
- Docker Compose

```bash
# Starta alla tjänster (backend, frontend, databas)
docker-compose up -d

# Se loggar
docker-compose logs -f
```

Tjänsterna kommer vara tillgängliga på:
- Frontend: http://localhost:5173
- Backend API: http://localhost:8080
- MySQL: localhost:3306

### Alternativ 2: Lokal installation

Krav på verktyg/versioner
- **.NET SDK:** 9.0
- **Node.js:** 18 eller 20 rekommenderas
- **MySQL:** igång lokalt på port 3306

Nedan följer en steg-för-steg guide för att köra projektet lokalt.

### 1. Databas Setup

1. Installera MySQL Server och MySQL Workbench från [MySQL Downloads](https://dev.mysql.com/downloads/)

2. När MySQL är installerat, öppna MySQL Command Line Client eller MySQL Workbench och skapa databasen:
```sql
CREATE DATABASE innoviaIOT;
```

3. Kontrollera att MySQL körs på port 3306 och ställ in följande standardinställningar:
   - Username: `root`
   - Password: `your_mysql_password` (använd ditt lokala MySQL-lösenord)

### 2. Backend Setup

1. Kopiera `.env.example` till `.env` i projektets rot:
```bash
cp .env.example .env
```

2. Uppdatera `.env` med dina MySQL-inställningar:
```env
DB_HOST=localhost
DB_PORT=3306
DB_NAME=innoviaIOT
DB_USER=root
DB_PASSWORD=your_mysql_password  # Ersätt med ditt lokala MySQL-lösenord
```

3. Installera .NET SDK 9.0 från [.NET Downloads](https://dotnet.microsoft.com/download)

4. Öppna en terminal i `Backend/` och kör:
```powershell
cd Backend
dotnet tool install --global dotnet-ef    # Installera Entity Framework Tools
dotnet restore                            # Återställ paket
dotnet ef database update                 # Skapa/uppdatera databasen
dotnet run                                # Starta backend
```

Backend startar på `http://localhost:8080`

Notera:
- Ett admin-konto skapas automatiskt:
  - Email: `admin@example.com`
  - Lösenord: `Admin@123`
- Vanliga användare kan registrera sig via gränssnittet
- SignalR hub körs på `/bookingHub`

### 3. Frontend Setup

1. Installera Node.js (version 18 eller 20) från [Node.js Downloads](https://nodejs.org/)

2. Skapa `.env` fil i `Frontend/` mappen:
```bash
cd Frontend
echo "VITE_API_URL=http://localhost:8080/api" > .env
```

3. Installera beroenden och starta utvecklingsservern:
```powershell
npm install
npm run dev
```

Frontend startar på `http://localhost:5173`

### 4. Testa Applikationen

1. Öppna http://localhost:5173 i webbläsaren

2. Logga in som admin:
   - Email: `admin@example.com`
   - Lösenord: `Admin@123`

3. Testa funktioner:
   - Skapa bokningar
   - Administrera resurser
   - Se realtidsuppdateringar
   - Testa sensorkopplingar
   - Prova AI-funktioner

### 5. Felsökning

Om du stöter på problem:

1. Databasanslutning:
   - Kontrollera att MySQL körs: `mysql -u root -p`
   - Verifiera anslutningssträngen i `.env`
   - Se till att databasen existerar: `SHOW DATABASES;`

2. Backend-problem:
   - Kontrollera loggarna för felmeddelanden
   - Verifiera att alla migrationer är körda
   - Testa API:et direkt via Swagger: http://localhost:8080/swagger

3. Frontend-problem:
   - Kontrollera webbläsarens konsol för fel
   - Verifiera att `VITE_API_URL` är korrekt
   - Rensa npm cache vid behov: `npm cache clean --force`
```

Frontend startar på `http://localhost:5173` 

---

## Strukturen
- `Backend/` – ASP.NET Core API, EF Core, Identity, SignalR
- `Frontend/` – React + Vite, React Router, SignalR-klient

## Databasen
- Starta MySQL lokalt och säkerställ att konfigurationen matchar `appsettings.json` (se ovan).
- Databasen och seed-data skapas automatiskt första gången du kör backend.

### Lokala DB-uppgifter (ditt system)

- Enligt dina uppgifter använder du en lokal MySQL-instans med följande utvecklingsinställningar:
  - Host: `localhost` (127.0.0.1)
  - Port: `3306`
  - Database: `innoviaIOT`
  - User: `root`
  - Password: `271444j`

- För att köra backend lokalt (utan Docker) är `Backend/appsettings.json` nu inställd med ovanstående standardanslutning så `dotnet run` försöker koppla mot din lokala DB.

### Skillnad mellan lokal MySQL och Docker-compose MySQL

- Docker Compose skapar en separat MySQL-container som i denna repo är mappad till värdens port `3307` för att undvika konflikt med en redan körande lokal MySQL på `3306`.
- Compose-DB (exempelinställningar):
  - Host (container): `db` (nätverksnamn i compose)
  - Port (container): `3306`
  - Mappad till host: `127.0.0.1:3307` (hostport)
  - App user: `innovia` / `innovia_pass`
  - Database: `innoviahub`

- Om du vill att container-backend ska använda din lokala DB istället för compose-DB, överstyr backendens env-variabler i `docker-compose.yml` eller använd `host.docker.internal` som `DB_HOST`:

```yaml
  backend:
    environment:
      - DB_HOST=host.docker.internal
      - DB_PORT=3306
      - DB_USER=root
      - DB_PASSWORD=271444j
      - DB_NAME=innoviaIOT
```

Notera: `host.docker.internal` fungerar i Docker Desktop på Windows/Mac. På Linux behöver du ange värdens IP eller konfigurera nätverket annorlunda.

---

## Felsökning

- CORS-fel mellan frontend och backend:
  - Kontrollera att backend tillåter anrop från `http://localhost:5173`.
  - Säkerställ att `VITE_API_URL` pekar på rätt adress (`http://localhost:5022/api`).

- Databasanslutning misslyckas:
  - Verifiera att MySQL kör på port `3306` eller uppdatera `appsettings.json` till din port.
  - Kontrollera användare/lösenord och att databasen finns/kan skapas.
