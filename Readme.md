# WIP: TouchPortal.Plugin.AudioMonitor
Audio Monitor Plugin for Touch Portal 2.3+ Windows.

This plugin can be used to monitor your Audio Input Device in TouchPortal.
This plugin is in a **early stage**, and are still under testing.

Right now we only support Input devices like Microphones.<br />
The monitor is made to be similar to what you see on the GoXLR Fader Meter, and the Audacity monitor.

![-2db](./Assets/-60db.png)
![-6db](./Assets/-8db.png)
![-28db](./Assets/-20db.png)
![-28db](./Assets/-30db.png)
![-28db](./Assets/-0db.png)

### Settings edit:

The plugin selects the input if it contains this text. If multiple inputs contains this text, the first one it finds will be selected.

![Settings dialog](./Assets/settings.png)

### Event edit

1. "When Plug-in State changes"<br />
> Choose "Audio Device Monitor" and "does not change to"
2. "Change visuals by plug-in state"<br />
> Change to "Icon" and state to "Audio Device Monitor"<br />
3. (optional) "Change Button Visuals"<br />
> Check "Change title to", and save.
> 
![Event setup](./Assets/events.png)

### The monitor

* Background: Green, then orange (from -12db) and red (-from -6db).
* Red line: is the max db that has been recordet after starting to monitor a source.<br />
* Blue line: is the max monitored the last 3 seconds.<br />
* Dark border: You are under -12db.<br />
* Green border: You are in that -6db to -12db range.<br />
* Red border: You are now over -6db.

### Dependencies

- [NAudio.Wasapi](https://github.com/naudio/NAudio)
- [TouchPortalSDK](https://github.com/oddbear/TouchPortalSDK)
