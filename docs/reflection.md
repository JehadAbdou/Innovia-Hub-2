# Reflektion — Innovia Hub (Krav 3)

Denna fil är en mall för din reflektion (sänt som `docs/reflection.md`). Fyll i varje avsnitt och exportera som PDF/DOC inför inlämning.

## 1. Sammanfattning (kort)
- Projekt: Innovia Hub — intranät och bokningssystem
- Ditt/er roll: (ex. frontend, backend, arkitektur, AI)
- Kort om vad ni levererar i detta projekt (2–4 meningar)

## 2. Vad jag har lärt mig
Beskriv huvudlärdomar: teknik, samarbete, verktyg, arkitektur, CI/CD, Docker, SignalR, EF Core m.m.
- Punktlista + korta förklaringar (3–6 punkter)

## 3. Tekniska val och motiveringar
För varje större val, kort förklaring varför:
- Backend: ASP.NET Core 9 — varför? (prestanda, ekosystem, EF Core)
- Databas: MySQL — varför? (bekant, tillgänglighet, hantering av data)
- Frontend: React + Vite — varför? (snabb dev, SPA, ekosystem)
- Realtid: SignalR — varför? (integrerat i .NET, enkel klient)
- AI: vilken modell/tjänst och varför (om tillämpligt)

## 4. Beskrivning av implementation (teknisk)
Kort och konkret:
- Arkitekturöversikt (komponenter och hur de samverkar)
- Datamodell (huvudsakliga tabeller/entities: User, Booking, Resource, ResourceType)
- Realtime-flöde: hur SignalR används (hub, events, klient-subscriptions)
- AI-flöde: var anrop görs (frontend vs backend), env-variabler/nyckelhantering
- IoT/integration: hur appen hanterar när IoT-servern är offline (error boundaries, fallback)

## 5. Arkitekturskiss (förenklad)
- Inkludera en enkel textbaserad skiss eller lägg in en bild i `docs/` och referera till den.

Exempel (ASCII):

```
[Browser/React SPA] <--HTTP/SignalR--> [Backend ASP.NET Core] <---> [MySQL]
                                          |
                                          +--> [IoT Service (mock)]
                                          +--> [AI Service (external)]
```

## 6. Problem och hur de löstes
Lista 3–5 konkreta problem ni stötte på och hur ni löste dem. Exempel:
- Problem: CORS mellan Vite och backend. Lösning: konfigurerade CORS-policy i `Program.cs` och satte `VITE_API_URL`.
- Problem: Databas inte redo i Docker. Lösning: lade till retry-logic och healthcheck i `docker-compose.yml`.

## 7. Vad jag skulle förbättra med mer tid
Kort lista med prioriterade förbättringar (funktionella + tekniska):
- Bättre testcoverage (unit/integration)
- Robustare AI-fallbacks
- Monitoring/metrics, loggning i produktion
- Mer finmaskig behörighetskontroll

## 8. Hur realtidsfunktionerna implementerats
Beskriv:
- Hub: `BookingHub` (server-side) — vilka meddelanden/events finns
- Klient: SignalR client i frontend (connect, reconnect, sätta upp event handlers)
- Hur du hanterar race-conditions/kollisioner (server-side validering + klientfeedback)

## 9. Driftsättning och miljökonfiguration
Beskriv hur ni driftsatt och vilka miljövariabler som krävs.
- Rekommenderade env vars:
  - `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`
  - `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`
  - `ALLOWED_ORIGINS`
  - `IOT_SERVICE_URL`, `RULES_SERVICE_URL`
  - (AI) `BACKEND_AI_KEY` eller `VITE_AI_URL`/`VITE_AI_KEY`

Kort driftsättningssteg (exempel med Docker):
1. `docker build --build-arg VITE_API_URL=https://your-api.example.com/api -t innovia-hub .`
2. `docker compose up --build -d`

## 10. Kända problem och begränsningar
- Lista kända begränsningar (skalning, säkerhet, edge-cases)
- Notera eventuella funktioner som inte är implementerade i deployed version (t.ex. vissa AI-funktioner om nyckel saknas)

---

När du fyllt i denna fil: exportera som PDF (t.ex. "Skriv ut" → PDF) eller spara som DOCX och lägg filen i `docs/reflection.pdf` eller `docs/reflection.docx`.

Behöver du att jag fyller i ett första utkast av några av sektionerna (t.ex. Arkitekturskiss eller Teknikval) med text baserat på koden i repo? Säg vilka sektioner så gör jag ett första utkast.
