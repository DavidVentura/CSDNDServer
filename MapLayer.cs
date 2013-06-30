using System;
using Microsoft.Xna.Framework;

namespace Server
{
	public class MapLayer
	{
		private LayerType Type;
		public LayerType type {
			get { return Type; }
		}
		private int[,] Tiles;
		public int[,] tiles {
			get { return Tiles; }
		}

		public MapLayer (LayerType t, int width, int height, int[,] data)
		{
			this.Type = t;
			Tiles = data;
		}
		public int TileAt (Vector2 position)
		{
			if (Tiles[(int)position.X,(int)position.Y]!=0)
				return Tiles[(int)position.X,(int)position.Y];
			return -1;
		}

	}
}

