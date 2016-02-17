﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network.Sim.Core {
	public class SignalCeaseEventArgs : EventArgs {
		public byte[] Data {
			get;
			private set;
		}

		public bool IsJam {
			get {
				return Data == null;
			}
		}

		public SignalCeaseEventArgs(byte[] data) {
			Data = data;
		}
	}
}