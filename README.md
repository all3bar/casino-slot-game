# Casino Slot Game

Sistema profesional de slots con protocolo SAS 6.0 y comunicación ID-003/Pulse para hardware de casino.

## Descripción

Juego de slots profesional desarrollado en C# (.NET / Unity), con soporte completo para:

- **Motor de 5 rodillos × 3 filas** con 12 símbolos, 20 líneas de pago y RNG certificable
- **Protocolo SAS 6.0** completo con polling, comandos de contadores y excepciones en tiempo real
- **Comunicación ID-003** para validadores de billetes (RS-232)
- **Protocolo Pulse** para coin acceptors y hoppers
- **Sistema de accounting** con contadores persistentes y checksums SHA-256
- **Features avanzados**: Free Spins, Bonus Game (pick-em), Wild/Scatter, Jackpot Progresivo
- **Panel de operador** con autenticación PIN
- **63 unit tests** que validan la lógica del juego, protocolo SAS y hardware

---

## Estructura del Proyecto

```
casino-slot-game/
├── Assets/
│   ├── config.json                        # Configuración del juego
│   └── Scripts/
│       ├── Core/
│       │   ├── GameManager.cs             # Singleton central; State Machine
│       │   ├── SlotMachine.cs             # Motor del juego
│       │   ├── Reel.cs                    # Rodillo con tira de símbolos
│       │   ├── Symbol.cs                  # Tipos de símbolo + Factory
│       │   ├── PaylineManager.cs          # 20 líneas de pago + PayTable
│       │   └── RNG.cs                     # Generador aleatorio certificable
│       ├── SAS/
│       │   ├── SASProtocol.cs             # Mensajes, CRC-16, BCD encoding
│       │   ├── SASCommands.cs             # Códigos de comandos y excepciones
│       │   ├── SASExceptions.cs           # Cola de excepciones en tiempo real
│       │   ├── SASCommunicator.cs         # Procesador de comandos SAS
│       │   └── SASMeterManager.cs         # Respuesta de contadores 0x7B
│       ├── Hardware/
│       │   ├── SerialCommunication.cs     # Wrapper de System.IO.Ports
│       │   ├── BillAcceptor.cs            # Controlador de validador de billetes
│       │   ├── ID003Protocol.cs           # Protocolo ID-003 (RS-232)
│       │   └── PulseController.cs         # Coin-in/Hopper pulses + HopperController
│       ├── Accounting/
│       │   ├── MeterManager.cs            # Contadores persistentes (thread-safe)
│       │   ├── EventLogger.cs             # Log de auditoría con tipos de evento
│       │   ├── PersistenceManager.cs      # Guardado seguro con SHA-256 checksum
│       │   └── GameHistory.cs             # Historial de 500 jugadas (replay)
│       ├── UI/
│       │   ├── MainGameUI.cs              # UI principal del juego
│       │   ├── PaytableUI.cs              # Tabla de pagos
│       │   ├── OperatorUI.cs              # Panel de operador (PIN protegido)
│       │   └── AnimationController.cs     # Control de animaciones
│       └── Features/
│           ├── FreeSpinsManager.cs        # Giros gratis con multiplicador
│           ├── BonusGameManager.cs        # Bonus pick-em game
│           └── JackpotManager.cs          # Jackpot progresivo
└── Tests/
    ├── CasinoSlot.Tests.csproj
    ├── Core/
    │   ├── RNGTests.cs
    │   ├── PaylineManagerTests.cs
    │   └── SlotMachineTests.cs
    ├── SAS/
    │   └── SASProtocolTests.cs
    └── Hardware/
        └── HardwareTests.cs
```

---

## Requisitos

- **.NET 8.0 SDK** (para ejecutar tests y compilar fuera de Unity)
- **Unity 2021.3 LTS** o superior (para el juego completo)
- **Windows** como plataforma objetivo (para System.IO.Ports / SerialPort)

---

## Ejecutar Tests

```bash
cd Tests
dotnet test
```

Resultado esperado: **63 tests pasados, 0 fallidos**.

---

## Configuración (`Assets/config.json`)

| Clave | Descripción | Valor por defecto |
|-------|-------------|-------------------|
| `sas.address` | Dirección SAS de la máquina (1-127) | `1` |
| `sas.portName` | Puerto COM para SAS | `COM1` |
| `sas.baudRate` | Velocidad de comunicación SAS | `19200` |
| `billAcceptor.portName` | Puerto COM para validador de billetes | `COM2` |
| `billAcceptor.acceptedDenominations` | Billetes aceptados (en centavos) | `[100,500,1000,2000,5000,10000]` |
| `machine.denomination` | Denominación de crédito | `1` |
| `machine.maxBetPerLine` | Apuesta máxima por línea | `10` |
| `machine.numPaylines` | Número de líneas de pago | `20` |
| `jackpot.seedAmount` | Importe inicial del jackpot (créditos) | `1000` |
| `jackpot.contributionRate` | Porcentaje de cada apuesta al jackpot | `0.01` (1%) |
| `rtp.target` | RTP objetivo | `96.0%` |

---

## Tabla de Pagos (Multiplicadores × apuesta por línea)

| Símbolo | 2 iguales | 3 iguales | 4 iguales | 5 iguales |
|---------|-----------|-----------|-----------|-----------|
| WILD    | 10        | 50        | 200       | 2000      |
| 7       | 5         | 30        | 150       | 1000      |
| BELL    | 4         | 20        | 100       | 500       |
| BAR 3   | 3         | 15        | 75        | 300       |
| BAR 2   | 2         | 10        | 50        | 200       |
| BAR 1   | 1         | 8         | 40        | 150       |
| CHERRY  | 4         | 10        | 40        | 100       |
| LEMON   | 1         | 5         | 20        | 80        |
| ORANGE  | 1         | 5         | 20        | 80        |
| PLUM    | 1         | 5         | 20        | 80        |
| SCATTER | 2×total bet| 10×total bet | 50×total bet | (ver abajo) |

**SCATTER:** 3 = 10 Free Spins, 4 = 15 Free Spins, 5 = 25 Free Spins  
**BONUS:** 3+ en cualquier lugar = Bonus Pick-em Game  
**WILD:** Sustituye todos los símbolos excepto SCATTER y BONUS

---

## Protocolo SAS 6.0 — Comandos implementados

| Código | Descripción |
|--------|-------------|
| `0x01` | Shutdown |
| `0x11` | Startup |
| `0x12` | Sound Off |
| `0x13` | Sound On |
| `0x06` | Enable Bill Acceptor |
| `0x07` | Disable Bill Acceptor |
| `0x08` | Enable Game Play |
| `0x09` | Disable Game Play |
| `0x1F` | Send Enabled Game Numbers |
| `0x2E` | Send Current Credits |
| `0x52` | Send Selected Game Number |
| `0x53` | Send Gaming Machine ID |
| `0x4C` | Send Last Accepted Bill |
| `0x54` | Send Total Coin In |
| `0x55` | Send Total Coin Out |
| `0x56` | Send Total Drop |
| `0x57` | Send Total Jackpot |
| `0x58` | Send Games Played |
| `0x59` | Send Games Won |
| `0x6F` | Send Authentication Info |
| `0x7B` | Send Gaming Machine Meters |

### Excepciones SAS (eventos en tiempo real)

Gestionadas mediante `SASExceptionManager` con cola de 100 entradas:

- Game Started / Game Ended / Game Recalled
- Cashout Button Pressed
- Bill Accepted / Rejected / Jammed / Stacker Full
- Jackpot Handpay Pending / Canceled
- Slot Door Opened / Closed
- AC Power Loss / Restored
- AFT Transfer Complete / Partial / Failed

---

## Arquitectura y Patrones de Diseño

| Patrón | Clase(s) |
|--------|----------|
| **Singleton** | `GameManager` |
| **State Machine** | `GameState` enum + `GameManager.TransitionTo()` |
| **Observer** | Eventos `SpinCompleted`, `FreeSpinsStarted`, `BillAccepted`, etc. |
| **Command** | `SASCommandProcessor.ProcessMessage()` + `switch` expression |
| **Factory** | `SymbolFactory.Create()` |
| **Strategy** | `IPayoutStrategy` (extensible vía `PayTable`) |

---

## Seguridad

- **Checksum SHA-256** en archivos de contadores para detectar manipulación
- **Autenticación PIN** en panel de operador (mínimo 4 dígitos)
- **CRC-16 SAS** en todos los mensajes del protocolo
- **Validación XOR** en mensajes ID-003
- **Contadores protegidos por lock** para acceso concurrente seguro
- **RNG criptográfico** (`RandomNumberGenerator`) para semilla inicial

---

## Para Certificación GLI/BMM

1. **RNG certificable**: La clase `RNG` usa semilla criptográfica (`RandomNumberGenerator.Create()`) y expone `Seed` + `CallCount` para auditoría
2. **Tira de rodillos documentada**: Los 5 strips están definidos en `Reel.GetDefaultStrip()` con símbolos balanceados para ~96% RTP
3. **Contadores no resetables**: Los meters solo se incrementan; ningún método los decrementa externamente
4. **Log de eventos completo**: Todos los eventos quedan registrados en `EventLogger` con timestamp y número de secuencia
5. **Historial de jugadas**: `GameHistory` guarda las últimas 500 jugadas con posiciones de rodillo y semilla RNG para reproducción

---

## Licencia

Proyecto de uso privado para industria gaming. Todos los derechos reservados.

