# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PLUTO-HOMER is a Unity-based hand/wrist rehabilitation therapy application. It connects to a physical robotic device (PLUTO) via serial COM port, guides patients through gamified exercises, and implements an "Assist-As-Needed" (AAN) algorithm that provides motor assistance only when needed. Session data is uploaded to AWS for remote monitoring.

## Build & Development

This is a **Unity project** — there are no command-line build scripts. Development is done through the Unity Editor:

- Open with Unity Hub or Unity Editor (check `ProjectSettings/ProjectVersion.txt` for required version)
- Build via **File > Build Settings** in the Unity Editor
- The physical PLUTO device connects on a COM port configured in `C:/lapconfig.json`
- Data uploads require a Python script at the path specified in `C:/lapconfig.json`

## Architecture

### Scene Flow

The app is a linear scene state machine. Each scene has exactly one MonoBehaviour handler. Main therapy flow:

```
LOGIN -> MAIN -> CHMECH -> CALIB -> [ASSESS -> ASSISTPROFILE] -> SETDUR -> CHGAME -> [GAME] -> CHMECH -> SUMM -> DATAUPLOAD
```

Admin/therapist flow: `Ctrl+Shift+X` on CHMECH loads **PLANSETUP** for configuring therapy plans.

### Core Static Classes (no `new`, no `GetComponent`)

- **`PlutoComm`** ([Assets/scripts/PlutoComm.cs](Assets/scripts/PlutoComm.cs)): All PLUTO device communication. Fires C# events (`OnButtonReleased`, `OnNewPlutoData`, `OnControlModeChange`, `OnMechanismChange`) that scene handlers subscribe to. All state is in static properties (`angle`, `torque`, `control`, `calibration`, `button`).
- **`JediComm`** ([Assets/scripts/JediComm.cs](Assets/scripts/JediComm.cs)): Low-level serial port using JEDI protocol. Runs a dedicated `AboveNormal`-priority reader thread. Calls `PlutoComm.parseByteArray()` which fires events — **these events fire from the reader thread, not the Unity main thread**.
- **`DataManager`** ([Assets/scripts/DataManager.cs](Assets/scripts/DataManager.cs)): All file I/O. CSV read/write, directory structure under `Assets/data/{userID}/data/`. Reads device config from `C:/lapconfig.json`.
- **`awsManager`** ([Assets/scripts/awsManager.cs](Assets/scripts/awsManager.cs)): Spawns `pythonw.exe` to upload data to AWS.

### AppData Singleton

[Assets/scripts/AppData.cs](Assets/scripts/AppData.cs) is a lazy singleton (`AppData.Instance`) holding all cross-scene runtime state: `userID`, `selectedMechanism`, `selectedGame`, `currentSessionNumber`, `aanController`, `trainingSide`, `userData`. `Initialize()` is called once from the MAIN scene — it connects the robot and starts logging.

### AAN Algorithm

`PlutoAANController` ([Assets/scripts/plutoaan.cs](Assets/scripts/plutoaan.cs)) is the production therapy algorithm. It runs a state machine across discrete movement trials, adapting `currentCtrlBound` using a forgetting factor (0.9) based on actual vs. desired success rate. `Update(angle, deltaT, trialDone)` is called every game frame. A `Stopwatch` triggers assistance after 1.5 seconds of patient stall.

### Scene Handler Pattern

Every scene handler follows this exact pattern — preserve it when adding new scenes:

```csharp
void Start() {
    PlutoComm.sendHeartbeat();
    AppLogger.SetCurrentScene(...);
    PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
    // UI init
}

void Update() {
    PlutoComm.sendHeartbeat(); // every frame
    if (changeScene) { SceneManager.LoadScene(...); changeScene = false; }
}

void OnDestroy() {
    if (ConnectToRobot.isPLUTO) PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
}
```

The `changeScene` boolean defers scene transitions to the Unity main thread (since `PlutoComm` events fire from the serial reader thread). Use `ConcurrentQueue<Action>` when you need to queue multiple actions from background threads (see `summarySceneHandler.cs`).

### Data Storage

All patient data lives as CSV files under `Assets/data/{userID}/data/`:
- `configdata.csv` — therapy plan (mechanism durations, ROM limits, FME types, dates)
- `sessions/sessions.csv` — one row per trial (26 fields: session#, trial#, game, mechanism, score, control bound, success rate, etc.)
- `rawdata/raw-sess{N}-trial{N}-{game}-{mechanism}.csv` — raw sensor stream ~100 Hz
- `rom/` — range-of-motion per mechanism
- `applog/` — three log streams: AppLogger, PlutoComLogger, PlutoAanLogger

### Assessment Scripts

Located in [Assets/Assessment/scripts/](Assets/Assessment/scripts/). These handle the ROM measurement workflow before therapy begins:
- `AROMsceneHandler.cs` — active ROM: patient moves freely, reversal detection tracks range
- `PROMsceneHandler.cs` — passive ROM: validates PROM ≥ AROM
- `AssistSceneHandler.cs` — APROM assessment using progressive torque via coroutine

### Games

Each game lives in its own folder under `Assets/Games/` (HAT, Ping Pong, RNR, FruitBasket, Hatrick, FlappyBird). Games interact with the AAN controller via `AppData.Instance.aanController` and read device state from `PlutoComm` static properties.

## Key Constraints

- `PlutoComm` events fire from the serial reader thread — never call Unity APIs directly in event handlers; use `changeScene` flag or `ConcurrentQueue<Action>` to marshal to main thread.
- `PlutoComm.sendHeartbeat()` must be called every `Update()` frame or the device will time out.
- Always unsubscribe from `PlutoComm` events in `OnDestroy()` guarded by `ConnectToRobot.isPLUTO` to avoid null ref when running without hardware.
- Device config (COM port, Python path) comes from `C:/lapconfig.json` via `DataManager.getLapConfig()` — do not hardcode paths.
