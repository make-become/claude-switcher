# Safe login routes the browser's claude:// sign-in to the profile signing in

Without the Switcher, a claude:// launch from outside the package (rundll32 url.dll,FileProtocolHandler) started the package's Claude.exe with the URL and no --user-data-dir; it lived ~2.3 s before handing over to the default profile. With --login <test profile>, the same launch lived ~16 ms and the URL was relaunched as claude-desktop.exe --user-data-dir=<path>\Claude-SwitcherTest "claude://…", which handed it to the test profile's running instance.
