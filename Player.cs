using Microsoft.Xna.Framework;
using System.Net.Sockets;


namespace Server
{
	public class Player
	{
		private int id=1;
		public int ID {
			get { return id; }
		}
		private int texture=6;
		public int textureID {
			get { return texture; }
		}

		public TcpClient socket;
		public Vector2 position;

		public Vector2 Position {
			get { return position; }
		}
		public Player (TcpClient t, int id)
		{
			this.id=id;
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

