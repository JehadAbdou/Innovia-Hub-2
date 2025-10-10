# Testning och kvalitet

1. Varför dessa tester är viktiga

- Registrering/inloggningstesterna skyddar grundläggande säkerhet (lösenordshashning, validering av fält) och förhindrar att ogiltiga användare skapas.

- Bokningstesterna säkerställer kärnflödet: att förslag sparas som "pending" och att systemet ger ett tydligt felmeddelande när en tid redan är upptagen.



2. Hur projektet är gjort för vidareutveckling

- Separation i lager (Controllers → Services → Repositories) gör koden enkel att förstå och ändra.

- Dependency Injection + interfaces (t.ex. `IBookingRepository`, `IAiService`) gör det lätt att mocka beroenden i tester eller byta implementation.

- Små, ansvarsfokuserade tjänster och DTOs minskar kopplingar och gör det säkrare att lägga till nya funktioner.



3. Säkerhet — hantering av hemliga nycklar i produktion

- Jag sparar API-nycklar, host, och host-lösenord i en lokal `.env`-fil för utveckling. Se till att denna fil finns i `.gitignore` och att riktiga produktionshemligheter förvaras i en säker secret store.
