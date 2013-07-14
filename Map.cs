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
		/*
		public static bool ValidPosition (Coord position)
		{
			if (!withinBounds(position))
				return false;

			foreach(Player p in Network.getPlayers)
				foreach (Character c in p.chars)
				if (c.Position==position && c.noclip==false)
					return false;

			if (Blocks.TileAt(position) >-1)
				return false;
			return true;

		}*/
		public static bool ValidPosition (Coord position, Character moving)
		{
			if (!withinBounds (position))
				return false;

			foreach (Player p in Network.Players)
				foreach (Character c in p.chars)
					for (int x = 0; x < c.Size; x++) 
						for (int y = 0; y < c.Size; y++) {
							if (c.Position+new Coord(x,y) == position && c.noclip == false && moving.ID != c.ID)
								return false;
						}

			if (Blocks.TileAt(position) >-1)
				return false;
			return true;

		}

		public static void ParseMapLayer (LayerType type,int width, int height, string parseData)
		{
			int[,] data = new int[width, height];
			string[] rows = parseData.Split ('|');
			string[] cols;
			for (int y =0; y < rows.Length;y++) {
				cols = rows[y].Split(',');
				for (int x = 0; x < cols.Length;x++)
					data[x,y] = Int16.Parse(cols[x]);
			}
			if (type==LayerType.Blocking) Blocks= new MapLayer(type,width,height,data);
			if (type==LayerType.Object) Objects= new MapLayer(type,width,height,data);
			if (type==LayerType.Ground) Ground= new MapLayer(type,width,height,data);
		}
		public static void ChangeTile (int objID, int blocking, int x, int y)
		{
			Blocks.tiles[x,y]=blocking;
			Objects.tiles[x,y]=objID;
		}
	}
}

