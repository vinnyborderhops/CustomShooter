using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomShooter", "vinny borderhops", "1.0.0")]
    [Description("Mess around with guns.")]
    public class CustomShooter : RustPlugin
    {
        private const string permUse = "customshooter.use";

        private CombinedConfig config;
        private Dictionary<ulong, PlayerShootConfig> playerConfigs = new();

        #region Config Classes
        public class CombinedConfig
        {
            public List<GunConfig> Guns = new();
            public List<AmmoConfig> AmmoList = new();
        }

        public class GunConfig
        {
            public string BaseShortname { get; set; }
            public string CustomName { get; set; }
            public string AmmoName { get; set; }
            public bool Consumable { get; set; } = false;
            public int ShotsPerAttack { get; set; } = 1;
            public ulong SkinID { get; set; } = 0;

            [System.NonSerialized] public AmmoConfig Ammo;
        }


        public class AmmoConfig
        {
            public string AmmoName { get; set; }
            public string PrefabName { get; set; }
            public string AmmoShort { get; set; }
            public bool Consumable { get; set; } = false;
            public float Velocity { get; set; } = 15f;
        }


        public class PlayerShootConfig
        {
            public AmmoConfig Ammo;
            public int Shots;
        }
        #endregion

        protected override void LoadDefaultConfig()
        {
            config = new CombinedConfig
            {
                Guns = new()
                {
                    new() { BaseShortname = "pistol.revolver", CustomName = "Rusty Launcher", AmmoName = "Rocket", ShotsPerAttack = 2, SkinID = 0 },
                    new() { BaseShortname = "rifle.bolt", CustomName = "Explosive Touch", AmmoName = "C4", Consumable = true, SkinID = 0 }
                },
                AmmoList = new()
                {
                    new() { AmmoName = "Rocket", PrefabName = "assets/prefabs/ammo/rocket/rocket_basic.prefab", AmmoShort = "ammo.rocket.basic", Velocity = 21f },
                    new() { AmmoName = "Heli", PrefabName = "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab", AmmoShort = "ammo.rocket.basic", Velocity = 50f },
                    new() { AmmoName = "MLRS", PrefabName = "assets/content/vehicles/mlrs/rocket_mlrs.prefab", AmmoShort = "ammo.rocket.mlrs", Velocity = 45f },
                    new() { AmmoName = "C4", PrefabName = "assets/prefabs/tools/c4/explosive.timed.deployed.prefab", AmmoShort = "explosive.timed", Consumable = true }
                }
            };
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<CombinedConfig>() ?? new CombinedConfig();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        void Init()
        {
            permission.RegisterPermission(permUse, this);

            foreach (var gun in config.Guns)
            {
                gun.Ammo = config.AmmoList.FirstOrDefault(a => a.AmmoName.Equals(gun.AmmoName, System.StringComparison.OrdinalIgnoreCase));

                if (gun.Ammo == null)
                {PrintWarning($"Ammo not found for gun {gun.CustomName} (AmmoName: {gun.AmmoName})");}
            }
        }


        object OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            Item item = player.GetActiveItem();
            if (item == null) return null;

            if (HandleRocketGun(item, player)) return null;
            if (HandleCustomShooter(player)) return null;

            return null;
        }

        private bool HandleRocketGun(Item item, BasePlayer player)
        {
            var gun = config.Guns.FirstOrDefault(g => g.BaseShortname == item.info.shortname && item.name == g.CustomName);
            if (gun == null || gun.Ammo == null) return false;

            ShootProjectile(player, gun.Ammo.AmmoShort, gun.Ammo.PrefabName, gun.Ammo.Consumable, gun.ShotsPerAttack, gun.Ammo.Velocity);
            return true;
        }

        private bool HandleCustomShooter(BasePlayer player)
        {
            if (!playerConfigs.TryGetValue(player.userID, out var shootConfig) || shootConfig.Ammo == null) return false;

            ShootProjectile(player, shootConfig.Ammo.AmmoShort, shootConfig.Ammo.PrefabName, shootConfig.Ammo.Consumable, shootConfig.Shots, shootConfig.Ammo.Velocity);
            return true;
        }

        void ShootProjectile(BasePlayer player, string ammoShortname, string prefab, bool consumable, int requestedShots, float velocity = 15f)
        {
            if (!TryConsumeAmmo(player, ammoShortname, requestedShots, out int shots)) return;

            for (int i = 0; i < shots; i++)
                FireProjectile(player, prefab, consumable, consumable ? 15f : velocity, shots);
        }

        bool TryConsumeAmmo(BasePlayer player, string ammoShortname, int requested, out int shots)
        {
            shots = requested;

            if (permission.UserHasPermission(player.UserIDString, permUse)) 
                return true;

            var def = ItemManager.FindItemDefinition(ammoShortname);
            if (def == null)
            {
                SendReply(player, $"Item '{ammoShortname}' does not exist.");
                return false;
            }

            string displayName = def.displayName.english;

            int available = player.inventory.GetAmount(def.itemid);
            if (available <= 0)
            {
                SendReply(player, $"You have no {displayName}s.");
                return false;
            }

            if (available < shots)
            {
                SendReply(player, $"Only {available} of {shots} {displayName}s Fired.");
            }

            shots = Mathf.Min(shots, available);
            player.inventory.Take(null, def.itemid, shots);
            player.Command("note.inv", def.itemid, -shots);

            return true;
        }

        void FireProjectile(BasePlayer player, string prefab, bool consumable, float velocity, int shots = 1)
        {
            Vector3 dir = player.eyes.HeadForward().normalized;
            Vector3 final;

            if (shots > 1) 
            {
                Quaternion spread = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
                final = spread * dir;
            }
            else
            {
                final = dir;
            }

            Vector3 pos = player.eyes.position + (dir * 0.75f);
            var ent = GameManager.server.CreateEntity(prefab, pos, player.eyes.rotation) as BaseEntity;
            if (ent == null) return;

            ent.Spawn();

            float appliedVelocity = velocity > 0f ? velocity : 15f;

            if (consumable)
            {
                ent.gameObject.AddComponent<ExplodeOnContact>();
                ent.SetVelocity(final * appliedVelocity);
            }
            else
            {
                ent.SendMessage("InitializeVelocity", final * appliedVelocity);
                var rb = ent.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = final * appliedVelocity;
            }
        }   

        [ChatCommand("rocketgun")]
        void CmdRocketGun(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            { SendReply(player, "No permission."); return; }

            if (args.Length == 0) { SendReply(player, "Usage: /rocketgun <customname> | list | all"); return; }

            if (args[0].Equals("list", System.StringComparison.OrdinalIgnoreCase))
            {
                SendReply(player, "Guns: " + string.Join(", ", config.Guns.Select(g => g.CustomName)));
                return;
            }
            if (args[0].Equals("all", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!permission.UserHasPermission(player.UserIDString, permUse))
                { SendReply(player, "No permission."); return; }

                config.Guns.ForEach(gun =>
                {
                    var item = ItemManager.CreateByName(gun.BaseShortname, 1, gun.SkinID);
                    if (item != null)
                    {
                        item.name = gun.CustomName;
                        player.GiveItem(item);
                    }
                });
                SendReply(player, "All rocket guns given.");
                return;
            }
            string gunName = string.Join(" ", args).Trim('"');
            var single = config.Guns.FirstOrDefault(g => g.CustomName.Equals(gunName, System.StringComparison.OrdinalIgnoreCase));
            if (single == null) { SendReply(player, "Gun not found."); return; }
            var it = ItemManager.CreateByName(single.BaseShortname, 1, single.SkinID);
            if (it != null)
            {
                it.name = single.CustomName;
                player.GiveItem(it);
                SendReply(player, $"Given: {single.CustomName}");
            }
        }

        [ChatCommand("shoot")]
        void CmdShoot(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            { SendReply(player, "No permission."); return; }

            if (args.Length == 0)
            {
                SendReply(player, "Usage: /shoot <ammo> [amount] | <amount> | list | clear");
                return;
            }

            if (args[0].Equals("list", System.StringComparison.OrdinalIgnoreCase))
            {
                SendReply(player, "Ammo: " + string.Join(", ", config.AmmoList.Select(a => a.AmmoName)));
                return;
            }

            if (args[0].Equals("clear", System.StringComparison.OrdinalIgnoreCase))
            {
                playerConfigs.Remove(player.userID);
                SendReply(player, "Cleared.");
                return;
            }

            if (args.Length == 1 && int.TryParse(args[0], out int newAmount))
            {
                if (playerConfigs.TryGetValue(player.userID, out var existingConfig) && existingConfig.Ammo != null)
                {
                    existingConfig.Shots = Mathf.Clamp(newAmount, 1, 100);
                    SendReply(player, $"Set shots to x{existingConfig.Shots} (Ammo: {existingConfig.Ammo.AmmoName}).");
                    return;
                }

                var item = player.GetActiveItem();
                if (item == null)
                {
                    SendReply(player, "No item in hand.");
                    return;
                }

                var gun = config.Guns.FirstOrDefault(g => g.BaseShortname == item.info.shortname && item.name == g.CustomName);
                if (gun == null || gun.Ammo == null)
                {
                    SendReply(player, "This weapon has no associated ammo config. Use /shoot <ammo> instead.");
                    return;
                }

                playerConfigs[player.userID] = new PlayerShootConfig
                {
                    Ammo = gun.Ammo,
                    Shots = Mathf.Clamp(newAmount, 1, 100)
                };

                SendReply(player, $"Using {gun.Ammo.AmmoName} from equipped gun. Shots set to x{newAmount}.");
                return;
            }

            string ammoName = string.Join(" ", args.Take(args.Length - (int.TryParse(args.Last(), out _) ? 1 : 0))).Trim('"');
            var ammo = config.AmmoList.FirstOrDefault(a => a.AmmoName.Equals(ammoName, System.StringComparison.OrdinalIgnoreCase));

            if (ammo == null)
            {
                SendReply(player, "Ammo not found.");
                return;
            }

            int shots = 1;
            if (args.Length > 1 && int.TryParse(args[1], out int s))
                shots = Mathf.Clamp(s, 1, 100);

            playerConfigs[player.userID] = new PlayerShootConfig { Ammo = ammo, Shots = shots };
            SendReply(player, $"Set to shoot {ammo.AmmoName} x{shots}.");
        }

        public class ExplodeOnContact : MonoBehaviour
        {
            private TimedExplosive explosive;
            void Awake() => explosive = GetComponent<TimedExplosive>();
            void OnCollisionEnter(Collision col) { if (explosive != null) explosive.Explode(); }
        }
    }
}