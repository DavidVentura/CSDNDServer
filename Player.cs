using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Server
{
	public class Player
	{
		public TcpClient socket;
		public List<Character> chars = new List<Character>();
		public int ID;
		public string Name;
		public Player (int id, List<Character> chars, string name)
		{
			this.chars=chars;
			this.ID=id;
			this.Name = name;
		}
	}
}

