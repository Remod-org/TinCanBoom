# TinCanBoom

Adds (timed) explosive to trigger for TinCanAlarm.  Can be switched on or off for each deployment using the /ente command.

The explosive will be invisible and silent.  On triggering, the TinCanAlarm will also be destroyed.

This Plugin uses harmony to add a new hook, OnTinCanAlarmTrigger.

```cs
    OnTinCanAlarmTrigger(TinCanAlarm alarm, RFTimedExplosive te)
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
    "debug": false
  },
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 0
  }
}
```

  - `RequirePermission` -- If true, players must have the tincanboom.use permission.
  - `startEnabled` -- Set default after load for whether a standard TinCanAlarm or a TinCanBoom will be deployed.

### Commands

  - `/ente` -- Toggles enablement of TinCanBoom when deploying a TinCanAlarm.  Will display current state.

