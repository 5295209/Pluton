﻿using System;

namespace Pluton.Events {
	public class DeathEvent {

		private HitInfo _info;

		public DeathEvent(HitInfo info) {
			_info = info;
		}

		public float DamageAmount {
			get {
				return _info.damageAmount;
			}
		}

		public string DamageType {
			get {
				return _info.damageType.ToString();
			}
		}

		public string IName {
			get {
				return _info.Initiator.name;
			}
		}

		public string IPrefab {
			get {
				return _info.Initiator.sourcePrefab;
			}
		}
	}
}
