Adds Hotkeys to switch between characters without using the mouse. Can be configured to either use F1-F10 keys or Alt+digit keys 1,2..0.

[b]IMPORTANT:[/b] Lua for Barotrauma client installation is required. Please consult with Lua for Barotraua mod page.

[h1]Features:[/h1]
[list]
 [*] hotkeys that simulate pressing on character item in crew list
 [*] configurable binds (function or digit keys). NOTE: if function keys are enabled (as per default), it will rebind in-game console from F3 to F11 and Ctrl+F3 while in-game. You can still open console using F3 while in menu or sub editor
 [*] character index prefixes: adds index prefix for characters you can switch to. This will look like "1) John Doe".
 [*] rudimentary ability to switch to bots in multiplayer
[/list]

[h1]Multiplayer support:[/h1]
Multiplayer switching does not require running modded server but does require [b]"setclientcharacter"[/b] console command to be permitted for client. Also, unlike single player, player character you switched off from will not be AI controlled. So your character will be braindead while you are controlling the bot.

[h1]Configuration:[/h1]
This mod is configured in code. To configure it, open [b]"%USERPROFILE%\AppData\Local\Daedalic Entertainment GmbH\Barotrauma\WorkshopMods\Installed\2967485314\Lua\ForcedAutorun\script.lua"[/b] (you can paste this path as is in Explorer)

Source: https://github.com/amadare42/BarotraumaMods/tree/master/SwitchCharactersHotkeys