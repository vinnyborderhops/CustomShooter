# CustomShooter

**CustomShooter** adds custom guns, ammo, explosives, skins, and multi-shot attacks to Rust.

---

## Features

- Fully configurable guns and ammo types.  
- Support for consumables like C4 and F1 Grenades.  
- Multi-shot attacks with random spread.  
- Chat commands for easy use.  
- Ability to skin custom weapons.  
- Permission-based access for controlled use.  
- Consumables detonate on contact.  
- Set custom velocity for projectiles.  

---

## Commands

| Command        | Description                          | Args
|----------------|--------------------------------------|--------------------------------------------|
| `/rocketgun`   | Give yourself custom weapons.        | /rocketgun customname - list - all|
| `/shoot`       | Set your custom ammo type.           | /shoot ammo [amount] - amount - list - clear|

---

## Permissions

| Permission              | Description                                                                 |
|-------------------------|-----------------------------------------------------------------------------|
| `customshooter.use`     | Grants access to chat commands. **Do NOT give to the default group**. Players without permissions can still use custom weapons without the need for permissions as long as it is the proper wepaon with the custom name. |

---

## Configuration

### Default Configuration

```json
{
  "Guns": [
    {
      "BaseShortname": "pistol.revolver",
      "CustomName": "Rusty Launcher",
      "AmmoName": "Rocket",
      "Consumable": false,
      "ShotsPerAttack": 2,
      "SkinID": 0
    },
    {
      "BaseShortname": "rifle.bolt",
      "CustomName": "Explosive Touch",
      "AmmoName": "C4",
      "Consumable": true,
      "ShotsPerAttack": 1,
      "SkinID": 0
    }
  ],
  "AmmoList": [
    {
      "AmmoName": "Rocket",
      "PrefabName": "assets/prefabs/ammo/rocket/rocket_basic.prefab",
      "AmmoShort": "ammo.rocket.basic",
      "Consumable": false,
      "Velocity": 21.0
    },
    {
      "AmmoName": "Heli",
      "PrefabName": "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab",
      "AmmoShort": "ammo.rocket.basic",
      "Consumable": false,
      "Velocity": 50.0
    },
    {
      "AmmoName": "MLRS",
      "PrefabName": "assets/content/vehicles/mlrs/rocket_mlrs.prefab",
      "AmmoShort": "ammo.rocket.mlrs",
      "Consumable": false,
      "Velocity": 45.0
    },
    {
      "AmmoName": "C4",
      "PrefabName": "assets/prefabs/tools/c4/explosive.timed.deployed.prefab",
      "AmmoShort": "explosive.timed",
      "Consumable": true,
      "Velocity": 15.0
    }
  ]
}
```

---
## Notes
- The default configuration shows how to structure custom guns and link them to ammo types.
- Use /shoot to test custom velocities before linking it to a /rocketgun.
  - Default velocity for consumables is 15, similar to player thrown items.
