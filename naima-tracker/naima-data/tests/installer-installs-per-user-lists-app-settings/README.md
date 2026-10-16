# The installer installs per user, lists the app in Settings > Apps, and uninstall removes everything but profile data

docs/install.ps1 run outside the package with CLAUDE_SWITCHER_ZIP pointing at the local build: installed into <path>\Programs\Claude Switcher, started it, and the Uninstall entry read 'Claude Switcher 1.0.2' with UninstallString --uninstall. Running that string: Uninstall entry, install folder, %APPDATA%\Claude Switcher, Start-menu folder, settings key and Run values all gone; no Switcher process left.
