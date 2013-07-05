using System.Net.Sockets;


namespace Server
{
	public class Player
	{
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

		public bool noclip=false;
		public TcpClient socket;
		public Coord position;

		public Coord Position {
			get { return position; }
		}
		public Player (int id, string name, int sprite, int visionrange, int size)
		{
			this.size = size;
			this.id=id;
			this.name=name;
			visionRange = visionrange;
			texture = sprite;
			position = Map.Spawnpoint;
		}

		public bool Move (Coord targetPos)
		{
			if (!Map.withinBounds (position + targetPos))
				return false;
			if (!noclip) 
			for (int x = 0; x < size; x++) 
				for (int y = 0; y < size; y++) {
					if (!Map.ValidPosition (position+targetPos+ new Coord(x,y),this)) {
						return false;
					}
				}

			position += targetPos;
			return true;
		}

	}
}

