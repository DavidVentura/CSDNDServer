using System.Net.Sockets;


namespace Server
{
	public class Player
	{
		private int id=1;
		public int ID {
			get { return id; }
		}
		private int texture;
		public int textureID {
			get { return texture; }
		}
		private string name;
		public string Name {
			get { return name; }
		}

		public bool noclip=false;

		public TcpClient socket;
		public Coord position;

		public Coord Position {
			get { return position; }
		}
		public Player (int id, string name, int sprite)
		{
			this.id=id;
			this.name=name;
			texture = sprite;
			position = Map.Spawnpoint;
		}

		public bool Move (Coord targetPos)
		{
			if (!Map.withinBounds(position+targetPos))
				return false;
			if (noclip || Map.ValidPosition (position+targetPos)) {
				position += targetPos;
				return true;
			}
			return false;
		}

	}
}

