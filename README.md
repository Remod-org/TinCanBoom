# TinCanBoom

Adds (timed) explosive to trigger for TinCanAlarm.  Can be switched on or off for each deployment using the /ente command.

The explosive will be invisible and silent.  On triggering, the TinCanAlarm will also be destroyed.

This Plugin uses harmony to add a new hook, OnTinCanAlarmTrigger.

```cs
    OnTinCanAlarmTrigger(TinCanAlarm alarm)
```

The plugin includes an example of this hook, which is used to enable and fire the explosive.

The hook itself can be used for additional signaling, etc., and we may integrate that as well.

### Permissions

  - `tincanboom.use` -- If configuration has RequirePermission set to true, this is the required permission.

### Configuration
```json
{
  "Options": {
    "RequirePermission": true,
    "startEnabled": false,
    "fireDelay": 0.0,
    "debug": false
  },
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```

  - `RequirePermission` -- If true, players must have the tincanboom.use permission.
  - `startEnabled` -- Set default after load for whether a standard TinCanAlarm or a TinCanBoom will be deployed.
  - `fireDelay` -- Delay firing of the explosive for X seconds.  0 means immediately upon trigger.  Can set higher to delay the firing of the explosive.

### Commands

  - `/ente` -- Toggles enablement of TinCanBoom when deploying a TinCanAlarm.  Will display current state.

