using System;
using System.Collections.Generic;


namespace Server
{
	public static class Map
	{
		static int height, width;
		public static int Width {
			get { return width; }
		}
		public static int Height {
			get { return height; }
		}
		private static Coord SpawnPoint;
		public static Coord Spawnpoint{
			get { return SpawnPoint; }
		}
		private static MapLayer Ground;
		private static MapLayer Objects;
		private static MapLayer Blocks;
		public static void Initialize (int h, int w, Coord spawnpoint)
		{
			height = h;
			width=w;
			SpawnPoint = spawnpoint;
		}

		public static void AddLayer (MapLayer mapLayer)
		{
			switch (mapLayer.type) {
			case LayerType.Blocking:
				Blocks = mapLayer;
				break;
			case LayerType.Ground:
				Ground=mapLayer;
				break;
			case LayerType.Object:
				Objects = mapLayer;
				break;
			}
		}
		public static MapLayer GetLayer (LayerType t)
		{
			switch (t) {
			case LayerType.Ground:
				return Ground;
			case LayerType.Object:
				return Objects;
			}
			return null;
		}

		public static bool withinBounds (Coord position)
		{
			if (position.X < 0 || position.Y < 0 || position.X >= width || position.Y >= height)
				return false;
			return true;
		}

		public static bool ValidPosition (Coord position)
		{
			if (!withinBounds(position))
				return false;

			foreach(Player p in Network.getPlayers)
				if (p.Position==position && p.noclip==false)
					return false;

			if (Blocks.TileAt(position) >-1)
				return false;
			return true;

		}
		public static bool ValidPosition (Coord position, Player moving)
		{
			if (!withinBounds (position))
				return false;

			foreach (Player p in Network.getPlayers)
				for (int x = 0; x < p.Size; x++) 
					for (int y = 0; y < p.Size; y++) {
						if (p.Position+new Coord(x,y) == position && p.noclip == false && moving.ID != p.ID)
							return false;
					}

			if (Blocks.TileAt(position) >-1)
				return false;
			return true;

		}

	}
}

