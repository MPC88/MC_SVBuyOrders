# MC_SVBuyOrders
  
Backup your save before using any mods.  
  
Uninstall any mods and attempt to replicate issues before reporting any suspected base game bugs on official channels.  
  
Function  
========  
When docked at a station, click Auto Trade button at the top-left above the repair buttons.    
  
Check the box to attempt automatic "full repair" when docking.  If you can't afford it, it will just fail with the usual message as if you clicked the button.

For the remaining items, -1 will mean "no action" for an item type.  Any other positive whole number will be fulfilled if possible (automatic buying and selling).
  
The game's "buyX" and "sellX" functions are used, so normal errors should appear.  Not tested what happens if you can't afford something.
  
If the station doesn't stock something i.e. doesn't trade in that thing, a message will appear in the scrolling menu at the bottom right (same place as crew evolution stuff).  If the station doesn't have enough of a thing, mod will buy what's available.  
  
Install  
=======  
1. Install BepInEx - https://docs.bepinex.dev/articles/user_guide/installation/index.html Stable version 5.4.21 x46.  
2. Run the game at least once to initialise BepInEx and quit.  
3. Download latest mod release.  
4. Place MC_SVBuyOrders.dll and mc_svbuyorders in .\SteamLibrary\steamapps\common\Star Valor\BepInEx\plugins\  
