﻿using System;
using UnityEngine;

namespace Pluton {
	public class Hooks {

		#region Events

		public static event ChatDelegate OnChat;

		public static event CommandDelegate OnCommand;

		public static event NPCDiedDelegate OnNPCDied;

		public static event NPCHurtDelegate OnNPCHurt;

		public static event PlayerConnectedDelegate OnPlayerConnected;

		public static event PlayerDisconnectedDelegate OnPlayerDisconnected;

		public static event PlayerDiedDelegate OnPlayerDied;

		public static event PlayerHurtDelegate OnPlayerHurt;

		public static event GatheringDelegate OnGathering;

		#endregion

		#region Handlers

		// chat.say().Hooks.Chat()
		public static void Command(Player player, string[] args) {
			string cmd = args[0].Replace("/", "");
			string[] args2 = new string[args.Length - 1];
			Array.Copy(args, 1, args2, 0, args.Length - 1);
			OnCommand(player, cmd, args2);
		}

		// chat.say()
		public static void Chat(ConsoleSystem.Arg arg){
			if (arg.ArgsStr.StartsWith("\"/")) {
				Command(new Player(arg.Player()), arg.Args);
				return;
			}

			if (!chat.enabled) {
				arg.ReplyWith("Chat is disabled.");
			} else {
				BasePlayer basePlayer = ArgExtension.Player(arg);
				if (!(bool) ((UnityEngine.Object) basePlayer))
					return;

				string str = arg.GetString(0, "text");

				if (str.Length > 128)
					str = str.Substring(0, 128);

				if (chat.serverlog)
					Debug.Log((object) (basePlayer.displayName + ": " + str));

				ConsoleSystem.Broadcast("chat.add " + StringExtensions.QuoteSafe(basePlayer.displayName) + " " + StringExtensions.QuoteSafe(str));
				arg.ReplyWith("chat.say was executed");
			}
			Debug.Log(arg.Player().displayName + " said: " + arg.ArgsStr);
			OnChat(arg);
		}

		// BaseResource.OnAttacked()
		public static void Gathering(BaseResource res, HitInfo info) {
			if (!Realm.Server())
				return;

			OnGathering(new Events.GatherEvent(info, res));

			res.health -= info.damageAmount * info.resourceGatherProficiency;
			if ((double) res.health <= 0.0)
				res.Kill(ProtoBuf.EntityDestroy.Mode.None, 0, 0.0f, new Vector3());
			else
				res.Invoke("UpdateNetworkStage", 0.1f);
		}

		// BaseAnimal.OnAttacked()
		public static void NPCHurt(BaseAnimal animal, HitInfo info) {
			if (!Realm.Server() || (double) animal.myHealth <= 0.0)
				return;

			if ((animal.myHealth - info.damageAmount) > 0.0f)
				OnNPCHurt(new Events.NPCHurtEvent(new NPC(animal), info));

			animal.myHealth -= info.damageAmount;
			if ((double) animal.myHealth > 0.0)
				return;
			animal.Die(info);
		}

		// BaseAnimal.Die()
		public static void NPCDied(BaseAnimal animal, HitInfo info) {
			Debug.Log("A '" + (animal.modelPrefab == null ? "null" : animal.modelPrefab) + "' died");
			OnNPCDied(new Events.NPCDeathEvent(new NPC(animal), info));
		}

		// BasePlayer.PlayerInit()
		public static void PlayerConnected(Network.Connection connection) {
			var player = connection.player as BasePlayer;
			Debug.Log(player.displayName + " joined the fun");
			OnPlayerConnected(new Player(player));
		}

		// BasePlayer.Die()
		public static void PlayerDied(BasePlayer player, HitInfo info) {
			Debug.Log(player.displayName + " just died");
			OnPlayerDied(new Events.PlayerDeathEvent(new Player(player), info));
		}

		// BasePlayer.OnDisconnected()
		public static void PlayerDisconnected(BasePlayer player) {
			Debug.Log(player.displayName + " left the reality");
			OnPlayerDisconnected(new Player(player));
		}

		// BasePlayer.OnAttacked()
		public static void PlayerHurt(BasePlayer player, HitInfo info) {
			Debug.Log("Player hurt...");
			if (!player.TestAttack(info) || !Realm.Server() || (info.damageAmount <= 0.0f))
				return;
			player.metabolism.bleeding.Add(Mathf.InverseLerp(0.0f, 100f, info.damageAmount));
			player.metabolism.SubtractHealth(info.damageAmount);
			player.TakeDamageIndicator(info.damageAmount, player.transform.position - info.PointStart);
			player.CheckDeathCondition(info);

			if (!player.IsDead())
				OnPlayerHurt(new Events.PlayerHurtEvent(new Player(player), info));

			player.SendEffect("takedamage_hit");
		}

		// BasePlayer.TakeDamage()
		public static void PlayerTakeDamage(BasePlayer player, float dmgAmount, Rust.DamageType dmgType) {
			try { Debug.Log(player.displayName + " is taking: " + dmgAmount.ToString() + " dmg (" + dmgType.ToString() + ")"); } catch (Exception ex) { Debug.Log("crap"); Debug.LogException(ex);}
			try { ConsoleSystem.Broadcast("broadcasttest"); } catch (Exception ex) { Debug.Log("crap"); Debug.LogException(ex);}
		}

		public static void PlayerTakeDamageOverload(BasePlayer player, float dmgAmount) {
			PlayerTakeDamage(player, dmgAmount, Rust.DamageType.Generic);
		}

		// BasePlayer.TakeRadiation()
		public static void PlayerTakeRadiation(BasePlayer player, float dmgAmount) {
			Debug.Log(player.displayName + " is taking: " + dmgAmount.ToString() + " RAD dmg");
		}

		/*
		 * bb.deployerUserName seems to be null
		 * 
		 */

		// BuildingBlock.OnAttacked()
		public static void EntityAttacked(BuildingBlock bb, HitInfo info) {
			try { 
				Debug.Log(info.Initiator.ToPlayer().displayName + " just hit a " + bb.blockDefinition.name + "(" + bb.blockDefinition.fullname + ")");
				Debug.Log("built by: " + bb.deployerUserName);
				Debug.Log("item's id base: " + bb.ItemIDBase.ToString());
			} catch (Exception ex) {
				Debug.Log("crap"); Debug.LogException(ex);
			}
		}

		// BuildingBlock.BecomeFrame()
		public static void EntityFrameDeployed(BuildingBlock bb) {
			// FIXME: null reference here
			try {
				Debug.Log(bb.deployerUserName + " started to build a " + bb.blockDefinition.name);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// BuildingBlock.BecomeBuilt()
		public static void EntityBuilt(BuildingBlock bb) {
			try {
				Debug.Log(bb.deployerUserName + " has built a " + bb.blockDefinition.name);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// BuildingBlock.DoBuild()
		public static void EntityBuildingUpdate(BuildingBlock bb, BasePlayer player, float proficiency) {
			try {
				Debug.Log(player.displayName + " is bulding " + bb.deployerUserName + "'s " + bb.blockDefinition.name + " with " + proficiency.ToString() + " proficiency");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// BaseCorpse.InitCorpse()
		public static void CorpseInit(BaseCorpse corpse, BaseEntity parent) {
			try {
				Debug.Log(corpse.ragdollPrefab + " has been initialized");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// BaseCorpse.OnAttacked()
		public static void CorpseHit(BaseCorpse corpse, HitInfo info) {
			try {
				Debug.Log(corpse.ragdollPrefab + " got a hit");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// PlayerLoot.StartLootingEntity()
		public static void StartLootingEntity(PlayerLoot playerLoot, BasePlayer looter, BaseEntity entity) {
			try {
				Debug.Log(looter.displayName + " is looting this: " + entity.sourcePrefab + " in pluton");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// PlayerLoot.StartLootingPlayer()
		public static void StartLootingPlayer(PlayerLoot playerLoot, BasePlayer looter, BasePlayer looted) {
			try {
				Debug.Log(looter.displayName + " is looting: " + looted.displayName + " in pluton");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// PlayerLoot.StartLootingItem()
		public static void StartLootingItem(PlayerLoot playerLoot, BasePlayer looter, Item item) {
			try {
				Debug.Log(looter.displayName + " is looting an item in pluton");
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		#endregion

		#region Delegates

		public delegate void ChatDelegate(ConsoleSystem.Arg arg);

		public delegate void CommandDelegate(Player player, string cmd, string[] args);

		public delegate void NPCDiedDelegate(Events.NPCDeathEvent de);

		public delegate void NPCHurtDelegate(Events.NPCHurtEvent he);

		public delegate void PlayerConnectedDelegate(Player player);

		public delegate void PlayerDiedDelegate(Events.PlayerDeathEvent de);

		public delegate void PlayerDisconnectedDelegate(Player player);

		public delegate void PlayerHurtDelegate(Events.PlayerHurtEvent he);

		public delegate void GatheringDelegate(Events.GatherEvent ge);

		#endregion

		public Hooks () { }
	}
}
