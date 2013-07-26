using System.Net.Sockets;
using System.Collections.Generic;
using System;

namespace Server
{
	public struct Saves {
		public int REF;
		public int FORT;
		public int WILL;
	}
	public struct Attributes {
		public int STR;
		public int DEX;
		public int CON;
		public int INT;
		public int WIS;
		public int CHA;
	}
	public class Character
	{
		#region Member objects
		private int id=1;
		public int ID {
			get { return id; }
		}

		private int size;
		public int Size {
			get { return size; }
		}

		private int visionRange; //in tiles
		public int VisionRange {
			get { return visionRange; }
		}
		private int texture;
		public int textureID {
			get { return texture; }
		}
		private string name;
		public string Name {
			get { return name; }
		}

		private bool NoClip=false;
		public bool noclip {
			get { return NoClip; }
			set { NoClip = value; }
		}

		private bool Invisible=false;
		public bool invisible {
			get { return Invisible; }
			set { Invisible = value; }
		}

		private Coord position;

		public Coord Position {
			get { return position; }
			set { position = value; }
		}

		public int currentInitiative;
		public List<Buff> Buffs = new List<Buff>();
		public List<Spell> Spells = new List<Spell>();
		public List<Equation> Equations = new List<Equation>();

		public Saves saves;
		public Attributes attributes;
		public int initiative;
		#endregion
		public Character (int id, string name, int sprite, int visionrange, int size,
		                  int will,int reflex,int fortitude,int cha,int wis,int intel,int con,int dex,int str,int init)
		{
			saves.FORT = fortitude;
			saves.REF = reflex;
			saves.WILL = will;

			attributes.CHA = cha;
			attributes.CON = con;
			attributes.INT = intel;
			attributes.WIS = wis;
			attributes.STR = str;
			attributes.DEX = dex;

			initiative = init;

			this.size = size;
			this.id=id;
			this.name=name;
			
			visionRange = visionrange;
			texture = sprite;
			position = Map.Spawnpoint;
		}

		public Character (Character mob, int i)
		{
			saves.FORT = mob.saves.FORT;
			saves.REF = mob.saves.REF;
			saves.WILL = mob.saves.WILL;

			attributes.CHA = mob.attributes.CHA;
			attributes.CON = mob.attributes.CON;
			attributes.INT = mob.attributes.INT;
			attributes.WIS = mob.attributes.WIS;
			attributes.STR = mob.attributes.STR;
			attributes.DEX = mob.attributes.DEX;

			initiative = mob.initiative;

			this.size = mob.size;
			this.id=i;
			this.name=mob.name;
			
			visionRange = mob.visionRange;
			texture = mob.texture;
			id = i;
		}
		public bool Move (Coord targetPos)
		{
			if (!Map.withinBounds (position + targetPos))
				return false;
			if (!noclip) 
			for (int x = 0; x < size; x++)
				for (int y = 0; y < size; y++)
					if (!Map.ValidPosition (position+targetPos+ new Coord(x,y),this))
						return false;
			position += targetPos;
			return true;
		}

		public void RollInitiative ()
		{
			currentInitiative= Engine.D10+initiative;
		}
		public string RollReflexes ()
		{
			int val = Engine.D20;
			return String.Format("({0})+{1}={2}", val,saves.REF,val + saves.REF);
		}
		public string RollFort ()
		{
			int val = Engine.D20;
			return String.Format("({0})+{1}={2}", val,saves.FORT,val + saves.FORT);
		}
		public string RollWill ()
		{
			int val = Engine.D20;
			return String.Format("({0})+{1}={2}", val,saves.WILL,val + saves.WILL);
		}
		public void AddBuff(Character caster,int Duration, string description) {
			Buffs.Add(new Buff(caster,Duration,description));
		}
		public void UpdateBuffsDuration(Character caster){
			foreach(Buff b in Buffs)
				if (b.Caster.ID == caster.ID) b.RoundEnds();
		}
		public void AddEquation(Equation e) {
			Equations.Add (e);
		}
		public string RollEquation (string str)
		{
			foreach (Equation e in Equations)
				if (e.GetDescription () == str)
					return e.Value();
			return null;
		}

	}
}

