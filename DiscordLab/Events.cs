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
using PluginAPI.Events;
using RemoteAdmin;
using System;
using static RoundSummary;

namespace DiscordLab
{
	public class Events
	{
		public static DateTime RoundEndTime = new DateTime(), RoundStartTime = new DateTime();
		public static bool RoundInProgress = false;


		[PluginEvent(ServerEventType.WarheadDetonation)]
		public void WarheadDetonateEvent() => DiscordLab.Bot.SendMessage(new Msg($"The alpha warhead has detonated"));

		[PluginEvent(ServerEventType.WaitingForPlayers)]
		public void WaitingForPlayersEvent() => DiscordLab.Bot.SendMessage(new Msg("The server is ready and waiting for players"));

		[PluginEvent(ServerEventType.PlayerHandcuff)]
		public void PlayerDisarmEvent(PlayerHandcuffEvent args) => DiscordLab.Bot.SendMessage(new Msg($"**{args.Player.Nickname} has disarmed {args.Target.Nickname}**"));

		[PluginEvent(ServerEventType.WarheadStop)]
		public void WarheadStopEvent(WarheadStopEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} has suspended the alpha warhead countdown"));

		[PluginEvent(ServerEventType.PlayerEscape)]
		public void PlayerEscapeEvent(PlayerEscapeEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Player.Nickname} escaped the facility and became {args.NewRole}"));

		[PluginEvent(ServerEventType.PlayerRemoveHandcuffs)]
		public void PlayerUndisarmEvent(PlayerRemoveHandcuffsEvent args) => DiscordLab.Bot.SendMessage(new Msg($"**{args.Player.Nickname} has freed {args.Target.Nickname}**"));

		[PluginEvent(ServerEventType.PlayerThrowProjectile)]
		public void ThrowProjectileEvent(PlayerThrowProjectileEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Thrower.Nickname} threw grenade {args.Item.ItemTypeId}"));

		[PluginEvent(ServerEventType.PlayerMuted)]
		public void PlayerMuteEvent(PlayerMutedEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Issuer.ToLogString()} has {(args.IsIntercom ? "intercom" : "")}muted {args.Player.ToLogString()}"));

		[PluginEvent(ServerEventType.PlayerUnmuted)]
		public void PlayerUnmuteEvent(PlayerUnmutedEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Issuer.ToLogString()} has {(args.IsIntercom ? "intercom" : "")}unmuted {args.Player.ToLogString()}"));

		[PluginEvent(ServerEventType.PlayerJoined)]
		public void PlayerJoinedEvent(PlayerJoinedEvent args) => DiscordLab.Bot.SendMessage(new Msg($"**{args.Player.Nickname} ({args.Player.UserId} from ||~~{args.Player.IpAddress}~~||) has joined the server**"));

		[PluginEvent(ServerEventType.PlayerBanned)]
		public void PlayerBannedEvent(PlayerBannedEvent args) => DiscordLab.Bot.SendMessage(new Msg($"**New Ban!**```autohotkey\nUser: {args.Player.ToLogString()}\nAdmin: {args.Issuer.ToLogString()}\nDuration: {args.Duration / 60} {(args.Duration / 60 > 1 ? "minutes" : "minute")}\nReason: {(string.IsNullOrEmpty(args.Reason) ? "No reason provided" : args.Reason)}```"));



















		[PluginEvent(ServerEventType.PlayerLeft)]
		public void PlayerLeftEvent(PlayerLeftEvent args)
		{
			if (args.Player.IsServer || !string.IsNullOrEmpty(args.Player.UserId))
				DiscordLab.Bot.SendMessage(new Msg($"{args.Player.Nickname} ({args.Player.UserId}) has disconnected from the server"));
		}

		[PluginEvent(ServerEventType.PlayerDying)]
		public void PlayerDeathEvent(PlayerDyingEvent args)
		{
			try
			{

				if (args.Player == null || !RoundInProgress)
					return;
				var uDH = args.DamageHandler as UniversalDamageHandler;

				if (args.DamageHandler is AttackerDamageHandler aDH)
				{
					if (aDH.IsSuicide)
						DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} killed themselves using {aDH.GetDamageSource()}"));
					else if (aDH.IsFriendlyFire || Extensions.IsFF(args.Player, args.Attacker))
						DiscordLab.Bot.SendMessage(new Msg($"**Teamkill** \n```autohotkey\nPlayer: {args.Attacker.Role} {args.Attacker.ToLogString()} \nKilled: {args.Player.Role} {args.Player.ToLogString()}\nUsing: {aDH.GetDamageSource()}```"));
					else if (args.Player.IsDisarmed && !args.Attacker.ReferenceHub.IsSCP())
						DiscordLab.Bot.SendMessage(new Msg($"__Disarmed Kill__\n```autohotkey\nPlayer: {args.Attacker.Role} {args.Attacker.ToLogString()} \nKilled: {args.Player.Role} {args.Player.ToLogString()}\nUsing: {aDH.GetDamageSource()}```"));
					else
						DiscordLab.Bot.SendMessage(new Msg($"{args.Attacker.Role} {args.Attacker.Nickname} killed {args.Player.Role} {args.Player.Nickname} using {aDH.GetDamageSource()}"));
				}
				else if (args.DamageHandler is WarheadDamageHandler wDH)
					DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} was vaporized by alpha warhead"));
				else
					DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} died to {DeathTranslations.TranslationsById[uDH.TranslationId].LogLabel}"));
			}
			catch(Exception e)
			{
				Log.Info(args.DamageHandler.ServerLogsText);
			}
		}

		[PluginEvent(ServerEventType.GrenadeExploded)]
		public void GrenadeExplodeEvent(GrenadeExplodedEvent args)
		{
			if (!args.Thrower.IsSet)
				DiscordLab.Bot.SendMessage(new Msg($"Frag grenade of (UNKNOWN) has exploded"));

			if (args.Grenade is ExplosionGrenade expGrenade)
				DiscordLab.Bot.SendMessage(new Msg($"Frag grenade of {args.Thrower.Nickname} has exploded"));
			else if (args.Grenade is FlashbangGrenade flshGrenade)
				DiscordLab.Bot.SendMessage(new Msg($"Flashbang of {args.Thrower.Nickname} has exploded"));
			else if (args.Grenade is Scp018Projectile scp018)
				DiscordLab.Bot.SendMessage(new Msg($"SCP-018 of {args.Thrower.Nickname} has exploded"));
			else
				DiscordLab.Bot.SendMessage(new Msg($"Grenade of unknown type thrown by {args.Thrower.Nickname} has exploded"));
		}









		[PluginEvent(ServerEventType.PlayerDamage), PluginPriority(LoadPriority.Lowest)]
		public void PlayerDamageEvent(PlayerDamageEvent args)
		{
			if (args.Player == null || args.Target == null || !RoundInProgress)
				return;

			if (args.DamageHandler is AttackerDamageHandler aDH)
			{
				if (aDH.IsSuicide || args.Player.UserId == args.Target.UserId)
				{
					DiscordLab.Bot.SendMessage(new Msg($"{args.Target.ToLogString()} damaged themselves using {aDH.GetDamageSource()}"));
				}
				else if (aDH.IsFriendlyFire || Extensions.IsFF(args.Target, args.Player))
				{
					if (args.Player.TemporaryData.Contains("ffdstop") && args.Player.UserId != args.Target.UserId)
					{
						DiscordLab.Bot.SendMessage(new Msg($"FFD Blocked {args.Player.Nickname} -> {args.Target.Nickname} ({aDH.GetDamageSource()})"));
						args.Player.TemporaryData.Remove("ffdstop");
					}
					else
					{
						DiscordLab.Bot.SendMessage(new Msg($"**{args.Player.Role} {args.Player.ToLogString()} -> {args.Target.Role} {args.Target.ToLogString()} -> {Math.Round(aDH.Damage, 1)} ({aDH.GetDamageSource()})**"));
					}
				}
				else if (args.Target.IsDisarmed && !args.Player.ReferenceHub.IsSCP())
				{
					DiscordLab.Bot.SendMessage(new Msg($"__{args.Player.Role} {args.Player.ToLogString()} -> {args.Target.Role} {args.Target.ToLogString()} -> {Math.Round(aDH.Damage, 1)} ({aDH.GetDamageSource()})__"));
				}
				else
				{
					DiscordLab.Bot.SendMessage(new Msg($"{args.Player.Nickname} -> {args.Target.Nickname} -> {Math.Round(aDH.Damage, 1)} ({aDH.GetDamageSource()})"));
				}
			}
			else if (args.DamageHandler is WarheadDamageHandler wDH)
			{
				DiscordLab.Bot.SendMessage(new Msg($"{args.Target.ToLogString()} was partially vaporized by alpha warhead"));
			}
			else if (args.DamageHandler is UniversalDamageHandler uDH)
			{
				//DiscordLab.Bot.SendMessage(new msgMessage($"{args.Target.ToLogString()} was damaged by {DeathTranslations.TranslationsById[uDH.TranslationId].LogLabel}"));
			}
		}

		[PluginEvent(ServerEventType.PlayerKicked)]
		public void PlayerKickedEvent(PlayerKickedEvent args)
		{
			if (!(args.Issuer is PlayerCommandSender pCS))
			{
				DiscordLab.Bot.SendMessage(new Msg($"**Player Kicked!**```autohotkey\nUser: {args.Player.ToLogString()}\nAdmin: SERVER\nReason: {(string.IsNullOrEmpty(args.Reason) ? "No reason provided" : args.Reason)}```"));
			}
			else
			{
				var admin = Player.Get(pCS.PlayerId);
				DiscordLab.Bot.SendMessage(new Msg($"**Player Kicked!**```autohotkey\nUser: {args.Player.ToLogString()}\nAdmin: {admin.ToLogString()}\nReason: {(string.IsNullOrEmpty(args.Reason) ? "No reason provided" : args.Reason)}```"));
			}
		}

		[PluginEvent(ServerEventType.PlayerChangeRole)]
		public void RoleChangeEvent(PlayerChangeRoleEvent args)
		{
			if (args.NewRole == RoleTypeId.Spectator || args.NewRole == RoleTypeId.None || args.OldRole.RoleTypeId == RoleTypeId.Spectator || args.OldRole.RoleTypeId == RoleTypeId.None)
				return;

			DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} changed from {args.OldRole.RoleTypeId} to {args.NewRole} ({args.ChangeReason})"));
		}


		[PluginEvent(ServerEventType.PlayerSpawn)]
		public void PlayerSpawnEvent(PlayerSpawnEvent args)
		{
			if (args.Role == RoleTypeId.None || args.Role == RoleTypeId.Spectator || args.Role == RoleTypeId.Overwatch || !RoundInProgress)
				return;
			DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} spawned as {args.Role}"));
		}

		[PluginEvent(ServerEventType.RoundEnd)]
		public void RoundEndEvent(RoundEndEvent args)
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



		[PluginEvent(ServerEventType.WarheadStart)]
		public void WarheadStartEvent(WarheadStartEvent args)
		{
			if (!args.IsAutomatic)
			{
				if (!args.IsResumed)
					DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} has started the alpha warhead countdown"));
				else
					DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} has resumed the alpha warhead countdown. Remaining time: {Warhead.DetonationTime.ToString("00")} seconds"));
			}
			else
				DiscordLab.Bot.SendMessage(new Msg($"The automatic alpha warhead countdown has begun"));
		}



		[PluginEvent(ServerEventType.RemoteAdminCommand)]
		public void RemoteAdminCommandEvent(RemoteAdminCommandEvent args)
		{
			if (!(args.Sender is PlayerCommandSender pCS))
				return;

			var admin = Player.Get(pCS.PlayerId);

			DiscordLab.Bot.SendMessage(new Msg($"{admin.ToLogString()} has run the following command: **{(args.Arguments.Length > 0 ? $"{args.Command} {string.Join(" ", args.Arguments)}" : $"{args.Command}")}**"));
		}

		[PluginEvent(ServerEventType.ConsoleCommand)]
		public void ConsoleCommandCommandEvent(ConsoleCommandEvent args)
		{
            DiscordLab.Bot.SendMessage(new Msg($"Server console has run the following command: **{(args.Arguments.Length > 0 ? $"{args.Command} {string.Join(" ", args.Arguments)}" : $"{args.Command}")}**"));
        }

		[PluginEvent(ServerEventType.PlayerGameConsoleCommand)]
		public void PlayerConsoleCommandEvent(PlayerGameConsoleCommandEvent args)
		{
            DiscordLab.Bot.SendMessage(new Msg($"{args.Player.ToLogString()} has run the following console command: **{(args.Arguments.Length > 0 ? $"{args.Command} {string.Join(" ", args.Arguments)}" : $"{args.Command}")}**"));
        }

		[PluginEvent(ServerEventType.Scp079UseTesla)]
		public void TeslaTriggerEvent(Scp079UseTeslaEvent args) => DiscordLab.Bot.SendMessage(new Msg($"{args.Player.Nickname} triggered a tesla gate as SCP-079"));
	}
}
