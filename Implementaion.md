
##  Genomförda Ändringar

### 1. **Integration av IoT API**
IoT API:t har integrerats i lösningen efter vissa anpassningar för att möjliggöra kommunikation mellan sensorer och innoviaHub projekt.  
Syftet är att tillåta dynamisk hantering av sensordata och att automatiskt kunna trigga regler baserat på inkommande realtidsdata.

---

### 2. **Rules.Engine**
I `Rules.Engine`-modulen har två nya metoder implementerats för att förbättra hanteringen av regler:

#### 🔹 `DeleteRule()`
- Ansvarar för att ta bort en specifik regel baserat på regel-ID.  

#### 🔹 `UpdateRule()`
- Möjliggör uppdatering av befintliga regler.  

---

### 3. **Realtime.Hub**
I `Realtime.Hub` har en ny metod implementerats för att hantera **alerts i realtid**.

#### 🔹 `TriggerAlert()`
- Metoden körs när inkommande sensordata matchar ett definierat regelkriterium.  
- Om kriterierna uppfylls skickas en varning (alert) till relevanta klienter via SignalR.  
- Ger möjlighet till realtidsövervakning av sensordata och snabb respons på kritiska händelser.

---

### 4. **API (Projekt: API)**
I API-projektet har IoT API:t integrerats, och nödvändiga endpoints har implementerats i `SensorController` för att möjliggöra CRUD-operationer på regler och sensorer.

#### 🔹 `SensorController` innehåller följande endpoints:

| Endpoint | HTTP Method | Beskrivning |
|-----------|--------------|-------------|
| `/api/sensors/devices` | **GET** | Hämtar alla registrerade IoT-enheter (devices). |
| `/api/sensors/rules` | **GET** | Hämtar alla definierade regler i systemet. |
| `/api/sensors/rules` | **POST** | Skapar en ny regel baserat på inkommande data. |
| `/api/sensors/rules/{id}` | **DELETE** | Tar bort en befintlig regel via `DeleteRule()`. |
| `/api/sensors/rules/{id}` | **PUT** | Uppdaterar en befintlig regel via `UpdateRule()`. |

---
