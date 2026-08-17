# Surface Input Bridge

**Surface Input Bridge** is a Windows-specific input bridge for forwarding
Microsoft Surface touchpad and keyboard input to a Linux system over UDP.

It is designed primarily for **Windows → Linux/Niri** workflows.

> **Windows-specific**
>
> The mouse input implementation relies on Windows Raw Input and is designed
> specifically around Microsoft Surface HID touchpads.
>
> This project is not intended to be a generic cross-platform input bridge.

## Overview

```text
┌──────────────────────────────┐
│            Windows           │
│                              │
│   Microsoft Surface          │
│   HID Touchpad               │
│          │                   │
│          ▼                   │
│      Windows Raw Input       │
│          │                   │
│          ▼                   │
│   Surface Input Bridge       │
│          │                   │
│          │ UDP :5000         │
└──────────┼───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│            Linux             │
│                              │
│        receiver.py           │
│             │                │
│       ┌─────┴─────┐          │
│       ▼           ▼          │
│  Virtual Mouse  Virtual      │
│                 Keyboard     │
│       │           │          │
│       └─────┬─────┘          │
│             ▼                │
│           evdev              │
│             │                │
│             ▼                │
│            Niri              │
└──────────────────────────────┘
```

The Windows side captures input and forwards events over UDP.

The Linux receiver converts those events into independent virtual evdev
devices.

## Current Status

The current version is considered **stable and highly usable**.

### Mouse

* [x] Touchpad movement
* [x] Left button
* [ ] Right button
* [ ] Mouse wheel
* [ ] Additional touchpad gestures

### Keyboard

* [x] Windows keyboard input
* [x] Key press/release
* [x] Modifier keys
* [x] Alt + number combinations
* [x] Function keys
* [x] Virtual Linux keyboard
* [x] macOS keycode protocol compatibility

Mouse and keyboard are intentionally kept as separate implementations on the
Windows side.

The Linux receiver combines both protocols into one UDP service, while still
creating separate virtual mouse and keyboard devices.

## Why Surface Input Bridge?

This project was created for a specific Windows/Linux workflow involving a
Microsoft Surface device and a Linux desktop running Niri.

The Surface touchpad is captured directly through Windows Raw Input instead of
trying to emulate or inject normal Windows mouse input.

The resulting relative movement and button events are transmitted to Linux,
where `evdev.UInput` creates a virtual mouse.

This allows the Linux desktop to treat the input as a normal local input
device.

## Requirements

### Windows

* Windows 10/11
* Microsoft Surface device
* Surface HID touchpad
* .NET
* Network connectivity to the Linux host

The mouse bridge uses:

* Windows Raw Input
* Win32 `RegisterRawInputDevices`
* Win32 `GetRawInputData`

The current implementation does **not** require:

* HidHide
* Administrator privileges
* A custom Windows HID driver

### Linux

* Linux
* Python 3
* `python-evdev`
* `/dev/uinput`
* A desktop/session capable of consuming evdev input

On Arch Linux:

```bash
sudo pacman -S python-evdev
```

Depending on the system's udev permissions, the receiver may need to be run
with elevated privileges:

```bash
sudo python3 receiver.py
```

## Network Protocol

The bridge uses UDP port `5000`.

### Mouse movement

Mouse movement packets use:

```text
[3][dx:int32][dy:int32]
```

Total packet size:

```text
9 bytes
```

The integers are little-endian signed 32-bit values.

### Mouse buttons

Mouse button packets use:

```text
[4][button][state]
```

Button values:

```text
1 = left
2 = right
3 = middle
```

State values:

```text
0 = released
1 = pressed
```

### Windows keyboard

The Windows keyboard protocol is intentionally simple:

```text
"key value"
```

For example:

```text
56 1
56 0
```

represents a key press and release.

The key value is already a Linux evdev key code.

This allows the Windows keyboard side to remain independent from the
touchpad-specific mouse implementation.

### macOS keyboard

The receiver also supports the original macOS keyboard protocol:

```text
<Bii>
```

where:

```text
event_type
key_code
value
```

Event types:

```text
1 = normal key
2 = modifier
```

macOS keycodes are translated into Linux evdev keycodes using the mappings in
`receiver.py`.

## Virtual Devices

The Linux receiver creates two independent virtual input devices.

### Virtual Mouse

```text
Surface Input Bridge Mouse
```

Capabilities currently include:

```text
EV_REL
    REL_X
    REL_Y
    REL_WHEEL

EV_KEY
    BTN_LEFT
    BTN_RIGHT
    BTN_MIDDLE
```

### Virtual Keyboard

```text
Surface Input Bridge Keyboard
```

The keyboard exposes the supported Linux evdev keyboard codes defined by the
receiver.

Keeping these devices separate allows the mouse implementation to evolve
without affecting the keyboard bridge.

## Testing

List the virtual devices:

```bash
sudo libinput list-devices
```

You should see something similar to:

```text
Device:                  Surface Input Bridge Mouse
Capabilities:            pointer
```

and:

```text
Device:                  Surface Input Bridge Keyboard
Capabilities:            keyboard
```

To inspect the resulting input events:

```bash
sudo libinput debug-events
```

Mouse movement should produce events such as:

```text
POINTER_MOTION
```

Keyboard input should produce corresponding keyboard events.

## Project Structure

A typical project layout is:

```text
surface-input-bridge/
├── Program.cs
├── MouseHook.cs
├── receiver.py
├── .gitignore
└── README.md
```

The Windows side is responsible for capturing and forwarding input.

The Linux side is responsible for converting UDP packets into virtual evdev
devices.

## Design

The project intentionally separates keyboard and mouse processing.

### Mouse

The mouse path is hardware-specific.

It directly processes the Microsoft Surface touchpad's Raw Input reports.

This allows the bridge to receive touchpad movement without depending on the
normal Windows cursor pipeline.

The mouse implementation can therefore be extended independently with:

* Right button
* Mouse wheel
* Additional touchpad controls
* Gesture support

### Keyboard

The keyboard path is deliberately simpler.

Windows sends key press/release events directly using Linux evdev key codes.

The Linux receiver then exposes those events through a separate virtual
keyboard.

This separation is intentional.

Changes to the Surface touchpad parser should not affect keyboard input.

## Platform Scope

The project is specifically designed around:

```text
Microsoft Surface
        │
        ▼
Windows HID Touchpad
        │
        ▼
Windows Raw Input
        │
        ▼
Surface Input Bridge
        │
        ▼
Linux / evdev / Niri
```

The current mouse implementation should **not** be assumed to work with:

* Generic Windows touchpads
* Other HID touchpad implementations
* macOS touchpads
* Linux touchpads directly
* Arbitrary USB mice

Support for other devices would require additional input-report handling.

## Stability

The current stable baseline should be preserved before making changes to the
mouse protocol.

The keyboard and mouse paths should remain independent.

When extending mouse functionality, changes should preferably be limited to
the Windows Raw Input parser and the corresponding mouse packet handling on
the Linux receiver.

## License

Choose an appropriate license before publishing this project.
