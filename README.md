# TCGui
# Tool Cupboard GUI for Rust (Remod original)

[Download](https://code.remod.org/TCGui.cs)

Provides a GUI to manage your tool cupboard and area autoturret authorization.  Also adds a button which appears above the TC loot table for accessing the GUI.
![](https://i.imgur.com/XknT4uc.png)

Click on Manage at the top of the TC Loot table for the GUI:
![](https://i.imgur.com/IvKZtYm.png)

Once opened by clicking the button, you can select players to add to the TC or turret by clicking the associated Select button:
![](https://i.imgur.com/xf3kRgH.png)

The user must be within range of the cupboard to access the GUI.  They must also be authorized to the cupboard.

There is currently no configuration required for TCGui.

## Permissions

- `tcgui.use` -- Allows player to see the Manage button and use the GUI

## Chat Commands
Most of this is only useful to and used by the GUI but could be used for scripting from other plugins or via RCON, perhaps...

- `/tc` - Parent function which will display the authorized players for the TC you're looking at.

- `/tc add {player.userID} {player.displayName}` - Add player to TC authorized list
- `/tc remove {player.userID}` - Remove player from TC authorized list
- `/tc tadd {player.userID} {player.displayName} {turret.net.ID.ToString()}` - Add player to turret authorized list
- `/tc tremove {theplayer.userID} {turret.net.ID.ToString()}` - Remove player from turret authorized list
