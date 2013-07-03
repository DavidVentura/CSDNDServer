using System.Net.Sockets;


namespace Server
{
	public class Player
	{
		private int id=1;
		public int ID {
			get { return id; }
		}
		private int texture=7;//TODO: Load from databse
		public int textureID {
			get { return texture; }
		}
		private string name;
		public string Name {
			get { return name; }
		}

		public TcpClient socket;
		public Coord position;

		public Coord Position {
			get { return position; }
		}
		public Player (TcpClient t, int id, string name)
		{
			this.id=id;
			socket=t;
			this.name=name;
			position = Map.Spawnpoint;
		}

		public bool Move (Coord targetPos)
		{
			if (Map.ValidPosition (position+targetPos)) {
				position += targetPos;
				return true;
			}
			return false;
		}

	}
}

