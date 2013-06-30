using System;
using Microsoft.Xna.Framework;
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

		private static MapLayer Ground;
		private static MapLayer Objects;
		private static MapLayer Blocks;
		public static void Initialize (int h, int w)
		{
			height = h;
			width=w;
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

		public static bool withinBounds (Vector2 position)
		{
			if (position.X < 0 || position.Y < 0 || position.X >= width || position.Y >= height)
				return false;
			return true;
		}

		public static bool ValidPosition (Vector2 position)
		{
			if (!withinBounds(position))
				return false;

			foreach(Player p in Network.getPlayers)
				if (p.Position==position)
					return false;

			if (Blocks.TileAt(position) >-1)
				return false;
			return true;

		}

	}
}

