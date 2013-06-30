using Microsoft.Xna.Framework;
using System.Net.Sockets;


namespace Server
{
	public class Player
	{
		public TcpClient socket;
		public Vector2 position;

		public Vector2 Position {
			get { return position; }
		}
		public Player (TcpClient t)
		{
			socket=t;
		}

		public bool Move (Vector2 targetPos)
		{
			if (Map.ValidPosition (position+targetPos)) {
				position += targetPos;
				return true;
			}
			return false;
		}

	}
}

