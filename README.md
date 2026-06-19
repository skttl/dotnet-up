# dotnet-up

Launch all your local development processes in parallel with a single command.

```
dotnet up
```

## Install

```bash
dotnet tool install -g dotnet-up
```

## Usage

### Start all processes

```bash
dotnet up
```

```
[npm] > npm install

  [npm]    started in new terminal window
  [dotnet] started in new terminal window

Press Ctrl+C to stop all.
```

Finds `dotnet-up.json` in the current directory or any parent. If found in a parent directory, asks for confirmation before starting.

```
Found dotnet-up.json in: C:\Projects\MyApp
Start dev processes from there? [Y/n]
```

### Scaffold a config file

```bash
dotnet up init
```

```
Created C:\Projects\MyApp\dotnet-up.json
Edit the file to define your processes, then run 'dotnet up'.
```

## Configuration

Create a `dotnet-up.json` in your project root:

```json
{
  "$schema": "https://raw.githubusercontent.com/skttl/dotnet-up/main/schema/dotnet-up.schema.json",
  "processes": [
    {
      "name": "npm",
      "cmd": "npm",
      "args": ["run", "watch"],
      "workDir": "{root}/src/Website",
      "runBefore": ["npm install"]
    },
    {
      "name": "dotnet",
      "cmd": "dotnet",
      "args": ["watch", "run"],
      "workDir": "{root}/src/Website"
    }
  ]
}
```

### Process fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | ✅ | Label shown in output |
| `cmd` | ✅ | Executable to run |
| `args` | | Arguments array |
| `workDir` | | Working directory. `{root}` = directory containing `dotnet-up.json` |
| `runBefore` | | Commands run blocking before this process. If any fails, this process is skipped. |
| `env` | | Environment variables for this process |

## Behaviour

- Each process opens in its own terminal window
- `runBefore` commands run sequentially and blocking before the process starts
- If a `runBefore` command fails, that process is skipped (others are unaffected)
- If a process exits unexpectedly, you are asked whether to keep the others running
- Ctrl+C shuts down all processes

## Platform support

| Platform | Terminal |
|----------|----------|
| Windows | New `pwsh` window |
| macOS | New Terminal.app window via `osascript` |
| Linux | `gnome-terminal`, `xterm`, `konsole`, or `xfce4-terminal` (first found) |
