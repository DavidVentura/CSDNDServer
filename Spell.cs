using System;
using System.Collections.Generic;

namespace Server
{
	public class Spell
	{
		string description;
		List<Equation> equations = new List<Equation>();
		public Spell (string desc)
		{
			description = desc;
		}
		public void AddEquation(Equation e) {
			equations.Add (e);
		}
	}
}

