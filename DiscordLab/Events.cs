using AdminToys;
using CommandSystem;
using Footprinting;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables;
using LiteNetLib;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using RemoteAdmin;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static RoundSummary;

namespace DiscordLab
{
	public class Events
	{
		public static DateTime RoundEndTime = new DateTime(), RoundStartTime = new DateTime();
		public static bool RoundInProgress = false;

		[PluginEvent(ServerEventType.PlayerJoined)]
		public void PlayerJoinedEvent(Player plr) => DiscordLab.Bot.SendMessage(new Msg($"**{plr.Nickname} ({plr.UserId} from ||~~{plr.IpAddress}~~||) has joined the server**"));

		[PluginEvent(ServerEventType.PlayerLeft)]
		public void PlayerLeftEvent(Player plr)
		{
			if (plr.IsServer || !string.IsNullOrEmpty(plr.UserId))
				DiscordLab.Bot.SendMessage(new Msg($"{plr.Nickname} ({plr.UserId}) has disconnected from the server"));
		}

		[PluginEvent(ServerEventType.PlayerDying)]
		public void PlayerDeathEvent(Player victim, Player attacker, DamageHandlerBase damageHandler)
		{
			if (victim == null || !RoundInProgress)
				return;
			var uDH = damageHandler as UniversalDamageHandler;

			if (damageHandler is AttackerDamageHandler aDH)
			{
				if (aDH.IsSuicide)
					DiscordLab.Bot.SendMessage(new Msg($"{victim.ToLogString()} killed themselves using {aDH.GetDamageSource()}"));
				else if (aDH.IsFriendlyFire || IsFF(victim, attacker))
					DiscordLab.Bot.SendMessage(new Msg($"**Teamkill** \n```autohotkey\nPlayer: {attacker.Role} {attacker.ToLogString()} \nKilled: {victim.Role} {victim.ToLogString()}\nUsing: {aDH.GetDamageSource()}```"));
				else if (victim.IsDisarmed && !attacker.ReferenceHub.IsSCP())
					DiscordLab.Bot.SendMessage(new Msg($"__Disarmed Kill__\n```autohotkey\nPlayer: {attacker.Role} {attacker.ToLogString()} \nKilled: {victim.Role} {victim.ToLogString()}\nUsing: {aDH.GetDamageSource()}```"));
				else
					DiscordLab.Bot.SendMessage(new Msg($"{attacker.Role} {attacker.Nickname} killed {victim.Role} {victim.Nickname} using {aDH.GetDamageSource()}"));
			}
			else if (damageHandler is WarheadDamageHandler wDH)
				DiscordLab.Bot.SendMessage(new Msg($"{victim.ToLogString()} was vaporized by alpha warhead"));
			else
				DiscordLab.Bot.SendMessage(new Msg($"{victim.ToLogString()} died to {DeathTranslations.TranslationsById[uDH.TranslationId].LogLabel}"));
		}

		[PluginEvent(ServerEventType.GrenadeExploded)]
		public void GrenadeExplodeEvent(Footprint thrower, Vector3 position, ItemPickupBase grenade)
		{
			if (!thrower.IsSet)
				DiscordLab.Bot.SendMessage(new Msg($"Frag grenade of (UNKNOWN) has exploded"));

			if (grenade is ExplosionGrenade expGrenade)
				DiscordLab.Bot.SendMessage(new Msg($"Frag grenade of {thrower.Nickname} has exploded"));
			else if (grenade is FlashbangGrenade flshGrenade)
				DiscordLab.Bot.SendMessage(new Msg($"Flashbang of {thrower.Nickname} has exploded"));
			else if (grenade is Scp018Projectile scp018)
				DiscordLab.Bot.SendMessage(new Msg($"SCP-018 of {thrower.Nickname} has exploded"));
			else
				DiscordLab.Bot.SendMessage(new Msg($"Grenade of unknown type thrown by {thrower.Nickname} has exploded"));
		}

		[PluginEvent(ServerEventType.PlayerThrowProjectile)]
		public void ThrowProjectileEvent(Player thrower, ThrowableItem item, ThrowableItem.ProjectileSettings projectileSettings, bool fullForce)
		{
			DiscordLab.Bot.SendMessage(new Msg($"{thrower.Nickname} threw grenade {item.ItemTypeId}"));
		}

		[PluginEvent(ServerEventType.PlayerBanned)]
		public void PlayerBannedEvent(Player player, CommandSender commandSender, string reason, long duration)
		{
			if (!(commandSender is PlayerCommandSender pCS))
				return;

			var admin = Player.Get(pCS.PlayerId);

			DiscordLab.Bot.SendMessage(new Msg($"**New Ban!**```autohotkey\nUser: {player.ToLogString()}\nAdmin: {admin.ToLogString()}\nDuration: {duration / 60} {(duration / 60 > 1 ? "minutes" : "minute")}\nReason: {(string.IsNullOrEmpty(reason) ? "No reason provided" : reason)}```"));
		}

		[PluginEvent(ServerEventType.PlayerEscape)]
		public void PlayerEscapeEvent(Player player, RoleTypeId newRole) => DiscordLab.Bot.SendMessage(new Msg($"{player.Nickname} escaped the facility and became {newRole}"));

		[PluginEvent(ServerEventType.PlayerHandcuff)]
		public void PlayerDisarmEvent(Player player, Player target) => DiscordLab.Bot.SendMessage(new Msg($"**{player.Nickname} has disarmed {target.Nickname}**"));

		[PluginEvent(ServerEventType.PlayerRemoveHandcuffs)]
		public void PlayerUndisarmEvent(Player player, Player target) => DiscordLab.Bot.SendMessage(new Msg($"**{player.Nickname} has freed {target.Nickname}**"));

		[PluginEvent(ServerEventType.PlayerDamage), PluginPriority(LoadPriority.Lowest)]
		public void PlayerDamageEvent(Player victim, Player attacker, DamageHandlerBase damageHandler)
		{
			if (attacker == null || victim == null || !RoundInProgress)
				return;

			if (damageHandler is AttackerDamageHandler aDH)
			{
				if (aDH.IsSuicide || attacker.UserId == victim.UserId)
				{
					DiscordLab.Bot.SendMessage(new Msg($"{victim.ToLogString()} damaged themselves using {aDH.GetDamageSource()}"));
				}
				else if (aDH.IsFriendlyFire || IsFF(victim, attacker))
				{
					if (attacker.TemporaryData.Contains("ffdstop") && attacker.UserId != victim.UserId)
					{
						DiscordLab.Bot.SendMessage(new Msg($"FFD Blocked {attacker.Nickname} -> {victim.Nickname} ({aDH.GetDamageSource()})"));
						attacker.TemporaryData.Remove("ffdstop");
					}
					else
					{
						DiscordLab.Bot.SendMessage(new Msg($"**{attacker.Role} {attacker.ToLogString()} damaged {victim.Role} {victim.ToLogString()} using {aDH.GetDamageSource()} for {Math.Round(aDH.Damage, 1)}**"));
					}
				}
				else if (victim.IsDisarmed && !attacker.ReferenceHub.IsSCP())
				{
					DiscordLab.Bot.SendMessage(new Msg($"__{attacker.Role} {attacker.ToLogString()} damaged {victim.Role} {victim.ToLogString()} using {aDH.GetDamageSource()} for {Math.Round(aDH.Damage, 1)}__"));
				}
				else
				{
					DiscordLab.Bot.SendMessage(new Msg($"{attacker.Nickname} -> {victim.Nickname} -> {Math.Round(aDH.Damage, 1)} ({aDH.GetDamageSource()})"));
				}
			}
			else if (damageHandler is WarheadDamageHandler wDH)
			{
				DiscordLab.Bot.SendMessage(new Msg($"{victim.ToLogString()} was partially vaporized by alpha warhead"));
			}
			else if (damageHandler is UniversalDamageHandler uDH)
			{
				//DiscordLab.Bot.SendMessage(new msgMessage($"{victim.ToLogString()} was damaged by {DeathTranslations.TranslationsById[uDH.TranslationId].LogLabel}"));
			}
		}
		public bool IsFF(Player victim, Player attacker)
		{
			var victimRole = victim.ReferenceHub.roleManager.CurrentRole;
			var attackerRole = attacker.ReferenceHub.roleManager.CurrentRole;

			if (victimRole.Team == Team.SCPs || attackerRole.Team == Team.SCPs)
				return false;

			if ((victimRole.RoleTypeId == RoleTypeId.ClassD || isChaos(victim.Role)) && (attackerRole.Team == Team.ClassD || isChaos(attacker.Role)))
			{
				if (victim.Role == RoleTypeId.ClassD && attacker.Role == RoleTypeId.ClassD)
					return false;
				return true;
			}
			else if ((victimRole.RoleTypeId == RoleTypeId.Scientist || isMtf(victim.Role)) && (attacker.Role == RoleTypeId.Scientist || isMtf(attacker.Role)))
				return true;

			return false;
		}

		private bool isChaos(RoleTypeId role)
		{
			switch (role)
			{
				case RoleTypeId.ChaosConscript:
				case RoleTypeId.ChaosRifleman:
				case RoleTypeId.ChaosRepressor:
				case RoleTypeId.ChaosMarauder:
					return true;
				default:
					return false;
			}
		}

		private bool isMtf(RoleTypeId role)
		{
			switch (role)
			{
				case RoleTypeId.FacilityGuard:
				case RoleTypeId.NtfCaptain:
				case RoleTypeId.NtfSpecialist:
				case RoleTypeId.NtfPrivate:
				case RoleTypeId.NtfSergeant:
					return true;
				default:
					return false;
			}
		}

		private bool isSCP(RoleTypeId role)
		{
			switch (role)
			{
				case RoleTypeId.Scp173:
				case RoleTypeId.Scp106:
				case RoleTypeId.Scp049:
				case RoleTypeId.Scp079:
				case RoleTypeId.Scp096:
				case RoleTypeId.Scp0492:
				case RoleTypeId.Scp939:
					return true;
				default:
					return false;
			}
		}

		[PluginEvent(ServerEventType.PlayerKicked)]
		public void PlayerKickedEvent(Player player, ICommandSender sender, string reason)
		{
			if (!(sender is PlayerCommandSender pCS))
			{
				DiscordLab.Bot.SendMessage(new Msg($"**Player Kicked!**```autohotkey\nUser: {player.ToLogString()}\nAdmin: SERVER\nReason: {(string.IsNullOrEmpty(reason) ? "No reason provided" : reason)}```"));
			}
			else
			{
				var admin = Player.Get(pCS.PlayerId);
				DiscordLab.Bot.SendMessage(new Msg($"**Player Kicked!**```autohotkey\nUser: {player.ToLogString()}\nAdmin: {admin.ToLogString()}\nReason: {(string.IsNullOrEmpty(reason) ? "No reason provided" : reason)}```"));
			}
		}

		public void PlayerPreauthEvent(string userid, string ipAddress, long expiration, CentralAuthPreauthFlags flags, string region, byte[] signature, ConnectionRequest connectionRequest, int readerStartPosition) { }

		[PluginEvent(ServerEventType.PlayerChangeRole)]
		public void RoleChangeEvent(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (newRole == RoleTypeId.Spectator || newRole == RoleTypeId.None || oldRole.RoleTypeId == RoleTypeId.Spectator || oldRole.RoleTypeId == RoleTypeId.None)
				return;

			DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} changed from {oldRole.RoleTypeId} to {newRole} ({reason})"));
		}

		[PluginEvent(ServerEventType.PlayerSpawn)]
		public void PlayerSpawnEvent(Player player, RoleTypeId role)
		{
			if (role == RoleTypeId.None || role == RoleTypeId.Spectator || role == RoleTypeId.Overwatch || !RoundInProgress)
				return;
			DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} spawned as {role}"));
		}

		[PluginEvent(ServerEventType.RoundEnd)]
		public void RoundEndEvent(LeadingTeam team)
		{
			RoundInProgress = false;
			RoundEndTime = DateTime.Now;
			DiscordLab.Bot.SendMessage(new Msg($"**Round Ended**\n```Round Time: {new DateTime(TimeSpan.FromSeconds((DateTime.Now - RoundStartTime).TotalSeconds).Ticks):HH:mm:ss}"
				+ $"\nEscaped Class-D: {EscapedClassD}"
				+ $"\nRescued Scientists: {EscapedScientists}"
				+ $"\nSurviving SCPs: {SurvivingSCPs}"
				+ $"\nWarhead Status: {(!Warhead.IsDetonated ? "Not Detonated" : "Detonated")}"
				+ $"\nDeaths: {Kills} ({KilledBySCPs} by SCPs)```"));
		}

		[PluginEvent(ServerEventType.RoundStart)]
		public void RoundStartEvent()
		{
			RoundInProgress = true;
			RoundStartTime = DateTime.Now;
			DiscordLab.Bot.SendMessage(new Msg("**A new round has begun**"));
		}

		[PluginEvent(ServerEventType.WaitingForPlayers)]
		public void WaitingForPlayersEvent() => DiscordLab.Bot.SendMessage(new Msg("The server is ready and waiting for players"));

		[PluginEvent(ServerEventType.WarheadStart)]
		public void WarheadStartEvent(bool isAutomatic, Player player, bool isResumed)
		{
			if (!isAutomatic)
			{
				if (!isResumed)
					DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} has started the alpha warhead countdown"));
				else
					DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} has resumed the alpha warhead countdown. Remaining time: {Warhead.DetonationTime.ToString("00")} seconds"));
			}
			else
				DiscordLab.Bot.SendMessage(new Msg($"The automatic alpha warhead countdown has begun"));
		}

		[PluginEvent(ServerEventType.WarheadStop)]
		public void WarheadStopEvent(Player player) => DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} has suspended the alpha warhead countdown"));

		[PluginEvent(ServerEventType.WarheadDetonation)]
		public void WarheadDetonateEvent() => DiscordLab.Bot.SendMessage(new Msg($"The alpha warhead has detonated"));

		[PluginEvent(ServerEventType.PlayerMuted)]
		public void PlayerMuteEvent(Player player, bool isIntercom) => DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} has been {(isIntercom ? "intercom" : "")}muted"));

		[PluginEvent(ServerEventType.PlayerUnmuted)]
		public void PlayerUnmuteEvent(Player player, bool isIntercom) => DiscordLab.Bot.SendMessage(new Msg($"{player.ToLogString()} is no longer {(isIntercom ? "intercom " : "")}muted"));

		[PluginEvent(ServerEventType.RemoteAdminCommand)]
		public void RemoteAdminCommandEvent(CommandSender commandSender, string command, string[] arguments)
		{
			Console.WriteLine(commandSender.SenderId);

			if (!(commandSender is PlayerCommandSender pCS))
				return;

			var admin = Player.Get(pCS.PlayerId);

			DiscordLab.Bot.SendMessage(new Msg($"{admin.ToLogString()} has run the following command: **{(arguments.Length > 0 ? $"{command} {string.Join(" ", arguments)}" : $"{command}")}**"));
		}

		[PluginEvent(ServerEventType.ConsoleCommand)]
		public void ConsoleCommandCommandEvent(CommandSender commandSender, string command, string[] arguments)
		{
			Console.WriteLine(commandSender.SenderId);

			//if (!(commandSender is PlayerCommandSender pCS))
			//	return;

			//var admin = Player.Get(pCS.PlayerId);

			//DiscordLab.Bot.SendMessage(new Msg($"{admin.ToLogString()} has run the following command: **{(arguments.Length > 0 ? $"{command} {string.Join(" ", arguments)}" : $"{command}")}**"));
		}

		[PluginEvent(ServerEventType.Scp079UseTesla)]
		public void TeslaTriggerEvent(Player player, TeslaGate tesla) => DiscordLab.Bot.SendMessage(new Msg($"{player.Nickname} triggered a tesla gate as SCP-079"));
	}
}
