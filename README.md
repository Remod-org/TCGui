# TCGui
## Tool Cupboard GUI for Rust (Remod original)

Provides a GUI to manage your tool cupboard and area autoturret authorization.  Also adds a button which appears above the TC loot table for accessing the GUI.

![](https://i.imgur.com/XknT4uc.png)

Click on Manage at the top of the TC Loot table for the GUI:

![](https://i.imgur.com/IvKZtYm.png)

Once opened by clicking the button, you can remove players by clicking the Remove button next to their name.  Select additional players to add to the TC or turret by clicking the associated Select button:

![](https://i.imgur.com/xf3kRgH.png)

The user must be within range of the cupboard to access the GUI.  They must also be authorized to the cupboard.

### Configuration
```json
{
  "Settings": {
    "cupboardRange": 3.0,
    "turretRange": 30.0
    "limitToFriends": false,
    "useFriends": false,
    "useClans": false,
    "useTeams": false
  },
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 12
  }
}
```

- `cupboardRange` -- Sets the minimum distance for interacting with a cupboard.  3f was the original default.  5f might work better for you.  Don't set it too high or you may see overlap and odd behavior.
- `turretRange` -- Sets the maximum distance from a cupboard to locate turrets.  30f is the default, which should be close to actual cupboard protection range.  You can adjust higher as needed if the plugin fails to find your local turrets.
- `limitToFriends` -- If true, the list of available players to select will be limited depending on the value of useFriends, useClans, and useTeams.

### Permissions

- `tcgui.use` -- Allows player to see the Manage button and use the GUI

## Chat Commands

- `/tc` - Parent function which will display the authorized players for the TC in front of you.
- `/tc gui` - Alternate way to open the GUI for the TC in front of you.

Most of this is only useful to and used by the GUI but could be used for scripting from other plugins or via RCON, perhaps...
- `/tc add {player.userID} {player.displayName}` - Add player to TC authorized list
- `/tc remove {player.userID}` - Remove player from TC authorized list
- `/tc tadd {player.userID} {player.displayName} {turret.net.ID.ToString()}` - Add player to turret authorized list
- `/tc tremove {theplayer.userID} {turret.net.ID.ToString()}` - Remove player from turret authorized list
