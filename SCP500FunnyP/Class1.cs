using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Loader.Features.Plugins;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;
using MEC;
namespace SCP500FunnyP
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        public int ScientistDropChance { get; set; } = 20;
        public float ScientistDropInterval { get; set; } = 10f;
    }
    public sealed class SCP500Chaos : Plugin<Config>
    {
        public override string Name => "SCP500Chaos";
        public override string Author => "adasjusk";
        public override Version Version => new Version(2, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 7);
        public override string Description => "SCP-500 random-effects plugin";
        private readonly Random _rng = new Random();
        private readonly List<CoroutineHandle> _handles = new List<CoroutineHandle>();
        private bool _sciDrop;
        private readonly List<(string id, int w)> _effects = new List<(string, int)>
        {
            // neutral
            ("NORMAL", 25),
            // good
            ("COCACOLA", 25),
            ("HEALTH_BOOST", 50),
            ("INCREASE_MAX_HP", 25),
            ("GIVE_ITEMS", 20),
            ("SPEED_BOOST", 40),
            ("ARTIFICIAL_HEALTH", 20),
            ("REGENERATION", 30),
            ("ADRENALINE_RUSH", 30),
            ("IRON_SKIN", 25),
            ("CANDY_RUSH", 20),
            ("MEDIC_KIT", 25),
            // scp gifts
            ("KEYCARD_O5", 8),
            ("SCP127_GUN", 15),
            ("SCP268", 10),
            ("SCP2176", 8),
            ("SCP018", 10),
            ("SCP1853", 12),
            // team / pvp
            ("SPAWN_ALLY", 30),
            ("SWITCH_TEAM", 25),
            ("STEAL_HEALTH", 30),
            ("VAMPIRE", 18),
            // very OP (rare)
            ("GODMODE", 5),
            ("JUGGERNAUT", 7),
            ("ARSENAL", 8),
            ("O5_PACKAGE", 6),
            ("SUPER_SPEED", 8),
            // bad
            ("POISONED", 25),
            ("BLEEDING", 25),
            ("ON_FIRE", 25),
            ("CONCUSSION", 20),
            ("BLINDED", 20),
            ("SLOWED", 25),
            ("ROOTED", 15),
            ("EXHAUSTED", 20),
            ("HEALTH_DRAIN", 20),
            ("CORRODING", 8),
            ("HEART_ATTACK", 6),
            ("AMNESIA", 15),
            ("GLASS_CANNON", 10)
        };
        public override void Enable()
        {
            Logger.Info("Enabling plugin...");
            PlayerEvents.UsedItem += OnItemUsed;
            PlayerEvents.Spawned += OnSpawned;
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundRestarted += OnRoundRestarted;
        }
        public override void Disable()
        {
            PlayerEvents.UsedItem -= OnItemUsed;
            PlayerEvents.Spawned -= OnSpawned;
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            KillRoutines();
            Logger.Info("Plugin disabled!");
        }
        private void OnRoundStarted()
        {
            KillRoutines();
            _sciDrop = _rng.Next(100) < Config.ScientistDropChance;
            if (_sciDrop)
                Logger.Info("SCP-500: leaky scientists are active this round.");
        }
        private void OnRoundRestarted()
        {
            _sciDrop = false;
            KillRoutines();
        }
        private void KillRoutines()
        {
            foreach (var h in _handles)
                Timing.KillCoroutines(h);
            _handles.Clear();
        }
        private void OnSpawned(PlayerSpawnedEventArgs ev)
        {
            if (!_sciDrop || ev?.Player == null)
                return;
            if (ev.Player.Role != RoleTypeId.Scientist)
                return;
            _handles.Add(Timing.RunCoroutine(ScientistDropRoutine(ev.Player)));
        }
        private IEnumerator<float> ScientistDropRoutine(Player player)
        {
            bool announced = false;
            while (_sciDrop)
            {
                yield return Timing.WaitForSeconds(Config.ScientistDropInterval);
                if (!_sciDrop)
                    yield break;
                if (player == null || !player.IsAlive || player.Role != RoleTypeId.Scientist)
                    yield break;
                try
                {
                    var pos = player.Position + new UnityEngine.Vector3(0f, 0.4f, 0f);
                    var pickup = Pickup.Create(ItemType.SCP500, pos, UnityEngine.Quaternion.identity);
                    if (pickup != null && !pickup.IsSpawned)
                        pickup.Spawn();
                    if (!announced)
                    {
                        player.SendBroadcast("SCP-500: Your pockets are leaking pills...", 4);
                        announced = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in ScientistDropRoutine: {ex}");
                }
            }
        }
        private void OnItemUsed(PlayerUsedItemEventArgs ev)
        {
            try
            {
                if (ev?.Player == null || ev.UsableItem == null)
                    return;
                var itemClassName = ev.UsableItem.GetType().Name;
                if (!itemClassName.Contains("Scp500") && !itemClassName.Contains("500"))
                    return;
                string effect = GetWeightedEffect();
                ApplyEffect(ev.Player, effect);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnItemUsed: {ex}");
            }
        }
        private void ApplyEffect(Player player, string effect)
        {
            try
            {
                switch (effect)
                {
                    case "NORMAL":
                        player.SendBroadcast("SCP-500: Nothing unusual happened, you got same effect.", 3);
                        break;
                    case "COCACOLA":
                        player.AddItem(ItemType.SCP207);
                        player.SendBroadcast("Fizzy! You feel energized!", 5);
                        break;
                    case "HEALTH_BOOST":
                        player.Health = player.MaxHealth;
                        player.SendBroadcast("SCP-500: You feel completely healed!", 5);
                        break;
                    case "INCREASE_MAX_HP":
                        player.MaxHealth += 100f;
                        player.Health = player.MaxHealth;
                        player.SendBroadcast("You feel stronger! Maximum health increased by 100!", 5);
                        break;
                    case "GIVE_ITEMS":
                        player.AddItem(ItemType.Medkit);
                        player.AddItem(ItemType.Painkillers);
                        player.SendBroadcast("SCP-500: You found valuable supplies!", 5);
                        break;
                    case "SPEED_BOOST":
                        ApplySpeedBoost(player);
                        break;
                    case "ARTIFICIAL_HEALTH":
                        player.ArtificialHealth = Math.Min(player.ArtificialHealth + 50f, player.MaxArtificialHealth);
                        player.SendBroadcast("SCP-500: You gained 50 artificial health!", 5);
                        break;
                    case "REGENERATION":
                        SafeEffect<CustomPlayerEffects.Vitality>(player, 1, 60f);
                        player.Health = Math.Min(player.Health + 50f, player.MaxHealth);
                        player.SendBroadcast("SCP-500: Your wounds slowly close over time!", 5);
                        break;
                    case "ADRENALINE_RUSH":
                        player.AddItem(ItemType.Adrenaline);
                        SafeEffect<CustomPlayerEffects.Invigorated>(player, 1, 30f);
                        SafeEffect<CustomPlayerEffects.MovementBoost>(player, 20, 20f);
                        player.SendBroadcast("SCP-500: Adrenaline floods your veins!", 5);
                        break;
                    case "IRON_SKIN":
                        SafeEffect<CustomPlayerEffects.DamageReduction>(player, 80, 45f);
                        SafeEffect<CustomPlayerEffects.BodyshotReduction>(player, 50, 45f);
                        player.SendBroadcast("SCP-500: Your skin hardens like iron!", 5);
                        break;
                    case "CANDY_RUSH":
                        SafeEffect<CustomPlayerEffects.RainbowTaste>(player, 3, 30f);
                        player.SendBroadcast("SCP-500: A rush of sugar takes over you!", 5);
                        break;
                    case "MEDIC_KIT":
                        player.AddItem(ItemType.Medkit);
                        player.AddItem(ItemType.Adrenaline);
                        player.AddItem(ItemType.Painkillers);
                        player.ArtificialHealth = Math.Min(player.ArtificialHealth + 25f, player.MaxArtificialHealth);
                        player.SendBroadcast("SCP-500: A full field medic kit!", 5);
                        break;
                    case "KEYCARD_O5":
                        player.AddItem(ItemType.KeycardO5);
                        player.SendBroadcast("SCP-500: You found an O5 Council keycard!", 5);
                        break;
                    case "SCP127_GUN":
                        player.AddItem(ItemType.GunSCP127);
                        player.SendBroadcast("SCP-500: You found a powerful weapon!", 5);
                        break;
                    case "SCP268":
                        player.AddItem(ItemType.SCP268);
                        player.SendBroadcast("SCP-500: You found SCP-268! Wear it to become invisible!", 5);
                        break;
                    case "SCP2176":
                        player.AddItem(ItemType.SCP2176);
                        player.SendBroadcast("SCP-500: You found SCP-2176! Ghostly light!", 5);
                        break;
                    case "SCP018":
                        player.AddItem(ItemType.SCP018);
                        player.SendBroadcast("SCP-500: You found SCP-018! The super ball!", 5);
                        break;
                    case "SCP1853":
                        SafeEffect<CustomPlayerEffects.Scp1853>(player, 1, 30f);
                        player.SendBroadcast("SCP-500: Enhanced perception for 30 seconds!", 5);
                        break;
                    case "SPAWN_ALLY":
                        ApplySpawnAlly(player);
                        break;
                    case "SWITCH_TEAM":
                        ApplySwitchTeam(player);
                        break;
                    case "STEAL_HEALTH":
                        ApplyStealHealth(player, 20f, 3);
                        break;
                    case "VAMPIRE":
                        ApplyVampire(player);
                        break;
                    case "GODMODE":
                        ApplyGodmode(player);
                        break;
                    case "JUGGERNAUT":
                        ApplyJuggernaut(player);
                        break;
                    case "ARSENAL":
                        ApplyArsenal(player);
                        break;
                    case "O5_PACKAGE":
                        player.AddItem(ItemType.KeycardO5);
                        player.AddItem(ItemType.SCP268);
                        player.AddItem(ItemType.SCP207);
                        player.AddItem(ItemType.Adrenaline);
                        player.SendBroadcast("SCP-500: O5 emergency package delivered!", 6);
                        break;
                    case "SUPER_SPEED":
                        SafeEffect<CustomPlayerEffects.MovementBoost>(player, 100, 30f);
                        SafeEffect<CustomPlayerEffects.Invigorated>(player, 1, 30f);
                        player.SendBroadcast("SCP-500: You move faster than the eye can follow!", 5);
                        break;
                    case "POISONED":
                        SafeEffect<CustomPlayerEffects.Poisoned>(player, 1, 15f);
                        player.SendBroadcast("SCP-500: That pill was rotten... you feel sick!", 5);
                        break;
                    case "BLEEDING":
                        SafeEffect<CustomPlayerEffects.Hemorrhage>(player, 5, 20f);
                        player.SendBroadcast("SCP-500: You start bleeding heavily!", 5);
                        break;
                    case "ON_FIRE":
                        SafeEffect<CustomPlayerEffects.Burned>(player, 5, 15f);
                        player.SendBroadcast("SCP-500: Your insides are burning!", 5);
                        break;
                    case "CONCUSSION":
                        SafeEffect<CustomPlayerEffects.Concussed>(player, 1, 20f);
                        SafeEffect<CustomPlayerEffects.Deafened>(player, 1, 15f);
                        player.SendBroadcast("SCP-500: Your head is spinning!", 5);
                        break;
                    case "BLINDED":
                        SafeEffect<CustomPlayerEffects.Blindness>(player, 5, 15f);
                        SafeEffect<CustomPlayerEffects.Flashed>(player, 1, 3f);
                        player.SendBroadcast("SCP-500: Everything goes dark!", 5);
                        break;
                    case "SLOWED":
                        SafeEffect<CustomPlayerEffects.Disabled>(player, 1, 20f);
                        player.SendBroadcast("SCP-500: Your legs feel like lead!", 5);
                        break;
                    case "ROOTED":
                        SafeEffect<CustomPlayerEffects.Ensnared>(player, 1, 6f);
                        player.SendBroadcast("SCP-500: You can't move your feet!", 5);
                        break;
                    case "EXHAUSTED":
                        SafeEffect<CustomPlayerEffects.Exhausted>(player, 1, 30f);
                        SafeEffect<CustomPlayerEffects.Disabled>(player, 1, 15f);
                        player.SendBroadcast("SCP-500: A wave of exhaustion hits you!", 5);
                        break;
                    case "HEALTH_DRAIN":
                        player.Health = Math.Max(1f, player.Health / 3f);
                        player.SendBroadcast("SCP-500: The pill drained your life force!", 5);
                        break;
                    case "CORRODING":
                        SafeEffect<CustomPlayerEffects.Corroding>(player, 1, 5f);
                        player.SendBroadcast("SCP-500: The pocket dimension calls to you!", 5);
                        break;
                    case "HEART_ATTACK":
                        SafeEffect<CustomPlayerEffects.CardiacArrest>(player, 1, 12f);
                        player.SendBroadcast("SCP-500: Your heart skips... and stops!", 5);
                        break;
                    case "AMNESIA":
                        SafeEffect<CustomPlayerEffects.AmnesiaVision>(player, 1, 20f);
                        player.SendBroadcast("SCP-500: You forget where you are!", 5);
                        break;
                    case "GLASS_CANNON":
                        player.MaxHealth = 15f;
                        player.Health = 15f;
                        player.AddItem(ItemType.GunRevolver);
                        player.AddAmmo(ItemType.Ammo44cal, 50);
                        player.SendBroadcast("SCP-500: Glass cannon! Huge firepower, paper skin!", 6);
                        break;
                    default:
                        player.SendBroadcast("SCP-500: Unknown effect!", 3);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying effect '{effect}': {ex}");
            }
        }
        private void SafeEffect<T>(Player player, byte intensity, float duration) where T : CustomPlayerEffects.StatusEffectBase
        {
            try
            {
                player.EnableEffect<T>(intensity, duration);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to apply effect {typeof(T).Name}: {ex.Message}");
            }
        }
        private void ApplyGodmode(Player player)
        {
            player.MaxHealth = 500f;
            player.Health = 500f;
            player.ArtificialHealth = player.MaxArtificialHealth;
            SafeEffect<CustomPlayerEffects.DamageReduction>(player, 200, 60f);
            SafeEffect<CustomPlayerEffects.MovementBoost>(player, 30, 60f);
            SafeEffect<CustomPlayerEffects.Invigorated>(player, 1, 60f);
            player.SendBroadcast("SCP-500: YOU ARE A GOD! 500 HP and near invincibility!", 7);
        }
        private void ApplyJuggernaut(Player player)
        {
            player.MaxHealth += 300f;
            player.Health = player.MaxHealth;
            SafeEffect<CustomPlayerEffects.BodyshotReduction>(player, 100, 60f);
            SafeEffect<CustomPlayerEffects.DamageReduction>(player, 60, 60f);
            player.AddItem(ItemType.GunLogicer);
            player.AddAmmo(ItemType.Ammo762x39, 300);
            player.SendBroadcast("SCP-500: JUGGERNAUT! Massive health and a heavy gun!", 7);
        }
        private void ApplyArsenal(Player player)
        {
            player.AddItem(ItemType.GunE11SR);
            player.AddAmmo(ItemType.Ammo556x45, 200);
            player.AddItem(ItemType.GunRevolver);
            player.AddAmmo(ItemType.Ammo44cal, 50);
            player.AddItem(ItemType.GrenadeHE);
            player.AddItem(ItemType.Medkit);
            player.AddItem(ItemType.Adrenaline);
            player.SendBroadcast("SCP-500: Full arsenal unlocked!", 6);
        }
        private void ApplyVampire(Player player)
        {
            float totalStolen = 0f;
            int hit = 0;
            foreach (var target in Player.List)
            {
                if (target == player || target.Team == player.Team || !target.IsAlive)
                    continue;
                float stolen = Math.Min(40f, target.Health - 1f);
                if (stolen <= 0f)
                    continue;
                target.Health -= stolen;
                totalStolen += stolen;
                hit++;
            }
            if (totalStolen > 0f)
            {
                player.Health = Math.Min(player.Health + totalStolen, player.MaxHealth);
                player.ArtificialHealth = Math.Min(player.ArtificialHealth + totalStolen * 0.5f, player.MaxArtificialHealth);
                player.SendBroadcast($"SCP-500: VAMPIRE! Drained {totalStolen:0} HP from {hit} enemies!", 6);
            }
            else
            {
                player.SendBroadcast("SCP-500: No blood to drain nearby...", 3);
            }
        }
        private void ApplySpawnAlly(Player player)
        {
            try
            {
                var spectator = Player.List.FirstOrDefault(p => p.Role == RoleTypeId.Spectator);
                if (spectator == null)
                {
                    player.SendBroadcast("No spectators available to summon!", 3);
                    return;
                }
                RoleTypeId targetRole = RoleTypeId.ChaosRifleman;
                if (player.Role == RoleTypeId.ClassD || player.Team == Team.ChaosInsurgency)
                    targetRole = RoleTypeId.ChaosRifleman;
                else if (player.Role == RoleTypeId.Scientist || player.Team == Team.FoundationForces)
                    targetRole = RoleTypeId.NtfSergeant;
                spectator.SetRole(targetRole);
                spectator.SendBroadcast("You have been summoned as an ally!", 5);
                player.SendBroadcast("An ally has spawned to assist you!", 5);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in ApplySpawnAlly: {ex}");
            }
        }
        private void ApplySwitchTeam(Player player)
        {
            RoleTypeId currentRole = player.Role;
            RoleTypeId newRole = currentRole;
            if (player.Team == Team.FoundationForces)
                newRole = RoleTypeId.ChaosRifleman;
            else if (player.Team == Team.ChaosInsurgency)
                newRole = RoleTypeId.NtfSergeant;
            else if (currentRole == RoleTypeId.ClassD)
                newRole = RoleTypeId.Scientist;
            else if (currentRole == RoleTypeId.Scientist)
                newRole = RoleTypeId.ClassD;
            if (newRole != currentRole)
            {
                player.SetRole(newRole);
                player.SendBroadcast("Your allegiance has been shifted!", 5);
            }
            else
            {
                player.SendBroadcast("Your allegiance remains unchanged.", 3);
            }
        }
        private void ApplyStealHealth(Player player, float perTarget, int maxTargets)
        {
            float totalStolen = 0f;
            int targetsAffected = 0;
            foreach (var target in Player.List)
            {
                if (target == player || target.Team == player.Team || !target.IsAlive)
                    continue;
                if (targetsAffected < maxTargets)
                {
                    float stealAmount = Math.Min(perTarget, target.Health);
                    target.Health -= stealAmount;
                    totalStolen += stealAmount;
                    targetsAffected++;
                }
            }
            if (totalStolen > 0)
            {
                player.Health = Math.Min(player.Health + totalStolen, player.MaxHealth);
                player.SendBroadcast($"You siphoned {totalStolen:0} health from {targetsAffected} enemies!", 4);
            }
            else
            {
                player.SendBroadcast("No enemies were in range to siphon health from.", 3);
            }
        }
        private void ApplySpeedBoost(Player player)
        {
            SafeEffect<CustomPlayerEffects.MovementBoost>(player, 20, 10f);
            player.SendBroadcast("You feel a sudden burst of speed for 10 seconds!", 5);
        }
        private string GetWeightedEffect()
        {
            int total = _effects.Sum(e => e.w);
            int roll = _rng.Next(total);
            int cum = 0;
            foreach (var (id, w) in _effects)
            {
                cum += w;
                if (roll < cum)
                    return id;
            }
            return _effects[0].id;
}   }   }