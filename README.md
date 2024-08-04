# TinCanBoom

Adds (timed) explosive to trigger for TinCanAlarm.  Can be switched on or off for each deployment using the /ente command.

The explosive will be invisible and silent.  On triggering, the TinCanAlarm will also be destroyed.

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

