# WIP: TouchPortal.Plugin.AudioMonitor
Audio Monitor Plugin for Touch Portal 2.3+ Windows.

Download and install .tpp file from [Releases](https://github.com/oddbear/TouchPortal.Plugin.AudioMonitor/releases/latest). <br />
**Important:** Some times you need to **refresh** the device page to "Kickstart" the updates.

[Latest Pre-Release](https://github.com/oddbear/TouchPortal.Plugin.AudioMonitor/releases/tag/v2-build-002) supports:
* Configure colors (ex. background to black)
* Configure from square to rectangle ex. 1x2, 1x3, 1x4 etc.

Feedback is welcome.

This plugin can be used to monitor your Audio Input Device in TouchPortal.
This plugin is in a **early stage**, and are still under testing.

Right now we only support Input devices like Microphones.<br />
The monitor is made to be similar to what you see on the GoXLR Fader Meter, and the Audacity monitor.

![-2db](./Assets/-60db.png)
![-6db](./Assets/-8db.png)
![-28db](./Assets/-20db.png)
![-28db](./Assets/-30db.png)
![-28db](./Assets/-0db.png)

### Event edit

1. "When Plug-in State changes"<br />
> Choose "Audio Monitor Current Image Stream" and "does not change to"
2. "Change visuals by plug-in state"<br />
> Change to "Icon" and state to "Audio Monitor Current Image Stream"<br />
3. (optional) "Change Button Visuals"<br />
> Check "Change title to", and save.

![Event setup](./Assets/events.png)

**Important**: `Audio Monitor Current Device Name` should not be used here.
> This state is only updated on source switching (and the image will not be updated).

### Actions

* Toggle Monitoring: Pause / Resume monitoring
* Clear Minitopring: Clear the red and blue line.
* Next Audio Source: Change audio source to the next availible.
* Prev Audio Source: Change audio source to the prev availible.
* Reset Audio Source to Settings: Clears next/prev selection, and uses the default (or the one specified in settings) instead.

### States

* Audio Monitor Current Image Stream: The image that shows the actual monitor.
* Audio Monitor Current Device Name: A text showing the name of the currently selected device.

### Settings edit:

#### Device Name

* If empty: picks the default Windows Input device.
* If not empty: picks the first found Input device with a name containing this text.
* If no match: Nothing is selected, and the image will say "no device"

![Settings dialog](./Assets/settings.png)

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
