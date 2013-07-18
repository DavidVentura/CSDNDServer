using System;

namespace Server
{
	public class Buff
	{
		public Character Caster;
		private int RoundsLeft;
		string Description;
		public Buff (Character caster, int Duration, string description)
		{
			this.Caster=caster;
			this.RoundsLeft=Duration;
			this.Description=description;
		}

		public void RoundEnds() {
			RoundsLeft--;
		}
		public bool IsActive() {
			return (RoundsLeft>0);
		}

	}
}

