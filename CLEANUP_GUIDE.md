# Build Artifacts Cleanup Guide

## Status
✅ **Successfully Cleaned:**
- `SerialCommunication/bin/` - Removed (MSBuild outputs)
- `SerialCommunication/obj/` - Removed (Object files)
- All `*.log` files - Removed

⚠️ **Requires Manual Cleanup:**
- `.vs/` directory - Locked by running Visual Studio process

## Why `.vs` is Locked
Visual Studio (devenv.exe) currently has the `.vs/` cache directory open. This directory contains:
- File content index cache
- Project-specific IDE settings
- Temporary build state

## How to Fully Clean

### Option 1: Close Visual Studio and Re-run Cleanup (Recommended)
```powershell
# 1. Close Visual Studio completely
# 2. Run the cleanup script with -Force flag:
.\cleanup-artifacts.ps1 -Force

# 3. Verify:
Get-ChildItem .vs -ErrorAction SilentlyContinue
# (should return nothing if successful)
```

### Option 2: Manual Deletion After Closing VS
```powershell
# 1. Close Visual Studio
# 2. Delete the directory manually:
Remove-Item .\.vs -Recurse -Force

# 3. Visual Studio will regenerate .vs on next load (this is normal)
```

### Option 3: Scheduled Deletion on Next Reboot
If you want to keep VS open, you can schedule deletion for next reboot:
```cmd
rmdir /s /q ".vs"
# If "access denied", the deferred deletion scheduled for reboot
```

## About `.vs` Directory
The `.vs/` directory is **safe to delete** at any time when Visual Studio is closed:
- It's automatically regenerated when you open the solution
- It's included in `.gitignore`
- It contains only IDE cache and metadata, not source code

## Cleanup Script Usage
```powershell
# View what will be deleted (dry-run):
.\cleanup-artifacts.ps1

# Permanently delete artifacts:
.\cleanup-artifacts.ps1 -Force
```

## Verification
After cleanup, the repository should only contain:
- Source files (.cs, .h, .cpp, .c, .ino)
- Project files (.slnx, .csproj, etc.)
- Configuration files
- Documentation

Check with:
```powershell
# These should be empty or non-existent:
Get-ChildItem bin -ErrorAction SilentlyContinue
Get-ChildItem obj -ErrorAction SilentlyContinue
Get-ChildItem .\.vs -ErrorAction SilentlyContinue
Get-ChildItem TestResults -ErrorAction SilentlyContinue
```

## Notes
- Run cleanup script **from repository root** where `cleanup-artifacts.ps1` is located
- The script requires PowerShell 5.0+ (Windows 10+)
- Administrator privileges may be required for some locked files
