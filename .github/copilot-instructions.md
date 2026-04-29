# Copilot instructions for SerialCommunication

Build, test and lint commands
- C# (Windows/.NET Framework 4.7.2): build the solution with MSBuild or Visual Studio:
  - msbuild "SerialCommunication.slnx" /t:Build /p:Configuration=Debug
  - Or open SerialCommunication.slnx in Visual Studio and build.
- Arduino sketch (SerialCommunication.ino): use Arduino CLI. Specify your board FQBN and port:
  - arduino-cli compile --fqbn <FQBN> "SerialCommunication.ino"
  - arduino-cli upload -p <PORT> --fqbn <FQBN> "SerialCommunication.ino"
- Tests: No automated test suite present in this repository.
- Linting: No project-specific lint tooling configured. For C#, use Visual Studio analyzers/Resharper/dotnet-format locally.

High-level architecture
- Dual-purpose repo:
  - Arduino firmware: SerialCommunication.ino, SerialCommand.cpp/h, analog.c implement a simple serial-command parser and handlers for digital/analog I/O and PWM. Commands are registered in setup() and handled by functions named onSet, onGet, onToggle, onPing, onHelp, onDebug.
  - Windows app: SerialCommunication/ is a .NET (likely WinForms) project (Form1.cs, Program.cs) packaged in SerialCommunication.slnx. It appears to be a GUI/utility for interacting with the Arduino over serial.
- Interaction flow: Arduino sketch exposes textual commands over Serial. SerialCommand.* tokenizes input and dispatches to handler functions. The Windows app likely opens a serial port and exchanges those commands with the device.

Key conventions and repo-specific notes
- SerialCommand limits and behavior:
  - Buffer sizes and limits are defined in SerialCommand.h: SERIALCOMMANDBUFFER (32), MAXSERIALCOMMANDS (10), MAXDELIMETER (2).
  - SERIALCOMMANDDEBUG is defined then immediately undefined; to enable verbose serial debug, comment out the `#undef SERIALCOMMANDDEBUG` line in SerialCommand.h.
  - The header comments say default terminator is '\r' but SerialCommand.cpp sets term='\n'. Be mindful of terminator when interoperating with host tools.
- Command naming in the sketch:
  - Digital pins: commands use prefix 'd' (e.g., d2). Allowed ranges are enforced in code (d2..d4 for set/toggle, d2..d7 for get).
  - Analog pins: prefix 'a' (a0..a5) and read via analogReadDelay.
  - PWM: prefix 'pwm' followed by pin (9..11) accepting 0..255 values.
- Code ownership & license: LICENSE.txt contains the project license (LGPL for SerialCommand portion). Keep license header intact when editing library files.
- Visual Studio artifacts: .vs/ and obj/ are present; use .gitignore to avoid committing local IDE state.

Files and entry points to focus on with Copilot
- Arduino/firmware work: SerialCommunication.ino, SerialCommand.h, SerialCommand.cpp, analog.c
- Windows app work: SerialCommunication\Form1.cs, Program.cs, SerialCommunication.slnx

AI assistant guidance
- When making changes to command parsing or handlers, update both SerialCommand.* and the sketch handlers together.
- Prefer minimal, surgical edits — buffer and array sizes are explicitly constrained.
- For debugging serial protocol issues, toggle SERIALCOMMANDDEBUG and match the terminator used by the host.

Other AI configs
- No CLAUDE.md, AGENTS.md, .cursorrules, or other known AI-assistant config files detected.


'
