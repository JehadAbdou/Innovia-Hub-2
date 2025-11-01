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

### 1. Backend

Öppna en terminal i `Backend/` och kör:
```powershell
cd Backend
dotnet restore
dotnet build
dotnet run
```

Backend startar på `http://localhost:5022` (API-bas: `http://localhost:5022/api`).

Notera:
- Projektet seedar data och en admin-användare vid första körningen (se `Services/DbSeeder.cs`).
- Standard-admin skapas med: användarnamn `admin`, lösenord `Admin@123`, roll `admin`.
- Du kan inte bli admin när du registrerar dig. För att logga in som admin, använd e-postadressen `admin@example.com` och lösenordordet `Admin@123`
- SignalR hub körs på `/bookingHub`.
- Databasanslutning styrs av `ConnectionStrings:DefaultConnection` i `Backend/appsettings.json`.
  - Du kan byta port/användare/lösen här eller via user secrets/ miljövariabler.

### 2. Starta Frontend

Frontend använder Vite och läser API-bas via `VITE_API_URL`.

1. Skapa en .env i `Frontend` med:
```env
VITE_API_URL=http://localhost:5022/api
```

Öppna en ny terminal i `Frontend/` och kör:
```powershell
cd Frontend
npm install
npm run dev
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
