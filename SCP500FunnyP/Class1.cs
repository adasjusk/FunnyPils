using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Loader.Features.Plugins;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;
namespace SCP500FunnyP
{
    public sealed class SCP500Chaos : Plugin<SCP500Chaos>
    {
        public override string Name => "SCP500Chaos";
        public override string Author => "adasjusk";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 4);
        public override string Description => "SCP-500 random-effects plugin";
        private readonly Random _rng = new Random();
        private readonly List<(string id, int w)> _effects = new List<(string, int)>
        {
            ("NORMAL", 30),
            ("COCACOLA", 30),
            ("SPAWN_ALLY", 40),
            ("SWITCH_TEAM", 40),
            ("INCREASE_MAX_HP", 30),
            ("STEAL_HEALTH", 40),
            ("HEALTH_BOOST", 60),
            ("GIVE_ITEMS", 20),
            ("SPEED_BOOST", 50),
            ("KEYCARD_O5", 9),
            ("SCP127_GUN", 20),
            ("SCP1853", 9),
            ("SCP268", 10),
            ("SCP2176", 9),
            ("SCP018", 10),
            ("ARTIFICIAL_HEALTH", 20)
        };
        public override void Enable()
        {
            Logger.Info("[SCP500Chaos] Enabling plugin...");
            PlayerEvents.UsedItem += OnItemUsed;
        }
        public override void Disable()
        {
            PlayerEvents.UsedItem -= OnItemUsed;
            Logger.Info("[SCP500Chaos] Plugin disabled!");
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
                Logger.Error($"[SCP500Chaos] Error in OnItemUsed: {ex}");
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
                    case "SPAWN_ALLY":
                        ApplySpawnAlly(player);
                        break;
                    case "SWITCH_TEAM":
                        ApplySwitchTeam(player);
                        break;
                    case "INCREASE_MAX_HP":
                        player.MaxHealth += 100f;
                        player.Health = player.MaxHealth;
                        player.SendBroadcast("You feel stronger! Maximum health increased by 100!", 5);
                        break;
                    case "STEAL_HEALTH":
                        ApplyStealHealth(player);
                        break;
                    case "HEALTH_BOOST":
                        player.Health = player.MaxHealth;
                        player.SendBroadcast("SCP-500: You feel completely healed!", 5);
                        break;
                    case "GIVE_ITEMS":
                        player.AddItem(ItemType.Medkit);
                        player.AddItem(ItemType.Painkillers);
                        player.SendBroadcast("SCP-500: You found valuable supplies!", 5);
                        break;
                    case "SPEED_BOOST":
                        ApplySpeedBoost(player);
                        break;
                    case "KEYCARD_O5":
                        player.AddItem(ItemType.KeycardO5);
                        player.SendBroadcast("SCP-500: You found an O5 Council keycard!", 5);
                        break;
                    case "SCP127_GUN":
                        player.AddItem(ItemType.GunSCP127);
                        player.SendBroadcast("SCP-500: You found a powerful weapon!", 5);
                        break;
                    case "SCP1853":
                        try
                        {
                            player.EnableEffect<CustomPlayerEffects.Scp1853>(1, 30f);
                            player.SendBroadcast("SCP-500: Enhanced perception for 30 seconds!", 5);
                        }
                        catch (Exception)
                        {
                            player.SendBroadcast("SCP-500: Enhanced perception!", 5);
                        }
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
                    case "ARTIFICIAL_HEALTH":
                        player.ArtificialHealth = Math.Min(player.ArtificialHealth + 50f, player.MaxArtificialHealth);
                        player.SendBroadcast($"SCP-500: You gained {50} artificial health!", 5);
                        break;
                    default:
                        player.SendBroadcast("SCP-500: Unknown effect!", 3);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SCP500Chaos] Error applying effect '{effect}': {ex}");
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
                Logger.Error($"[SCP500Chaos] Error in ApplySpawnAlly: {ex}");
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

        private void ApplyStealHealth(Player player)
        {
            float totalStolen = 0f;
            float perTarget = 20f;
            int targetsAffected = 0;
            foreach (var target in Player.List)
            {
                if (target == player || target.Team == player.Team || !target.IsAlive)
                    continue;
                if (targetsAffected < 3)
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
            try
            {
                player.EnableEffect<CustomPlayerEffects.MovementBoost>(1, 10f);
                player.SendBroadcast("You feel a sudden burst of speed for 10 seconds!", 5);
            }
            catch (Exception)
            {
                player.SendBroadcast("The speed effect failed to apply!", 3);
            }
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
        }
    }
}