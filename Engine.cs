using System;
using System.Data;
#if LINUX
using Mono.Data.Sqlite;
#else
using System.Data.SQLite;
#endif
using System.Collections.Generic;

namespace Server
{
	public enum LayerType {
		Ground=0,
		Blocking=1,
		Object=2
	}
	public struct Coord {
		public int X;
		public int Y;
		public Coord (int x, int y)
		{
			X=x;
			Y=y;
		}
		public static bool operator ==(Coord c, Coord c2){
			return (c.X==c2.X && c.Y==c2.Y);
		}
		public static bool operator !=(Coord c, Coord c2){
			return (c.X!=c2.X || c.Y!=c2.Y);
		}

		public static Coord operator +(Coord c, Coord c2){
			return new Coord(c.X+c2.X, c.Y+c2.Y);
		}
		public static Coord operator -(Coord c, Coord c2){
			return new Coord(c.X-c2.X, c.Y-c2.Y);
		}

	}

	public static class Engine
	{
		public static string GlobalTextures;
		private static List<int> textures = new List<int>();
		private const string ConnectionString = "URI=file:database.db";
		private const int MAPID = 1;
		private static IDbConnection dbcon;
		private static IDataReader reader;
		private static IDbCommand dbcmd;
		private static int curMobID=2000;

		public static void Initialize ()
		{
			LoadDatabase();
		}

		private static void LoadDatabase ()
		{
			Console.WriteLine ("Started loading from DB");

			#if LINUX
				dbcon = (IDbConnection)new SqliteConnection (ConnectionString);
			#else
				dbcon = (IDbConnection)new SQLiteConnection (ConnectionString);
			#endif
			dbcon.Open ();
			dbcmd = dbcon.CreateCommand ();

			dbcmd.CommandText = "SELECT WIDTH,HEIGHT,SPAWNX,SPAWNY FROM MAP WHERE ID=" + MAPID;
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				Map.Initialize(reader.GetInt16 (0), reader.GetInt16 (1), new Coord(reader.GetInt32(2),reader.GetInt16(3)));
			} else {
				return; //die
			}
			Console.WriteLine ("Map loaded");
			dbcmd.Dispose ();
			dbcmd.CommandText = "SELECT TYPE,DATA FROM LAYERS WHERE MAPID=" + MAPID;
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				Map.AddLayer(ParseMapLayer ((LayerType)reader.GetInt16 (0),  Map.Width, Map.Height, reader.GetString (1)));
			}
			Console.WriteLine ("Layers loaded");
			GlobalTextures="TXTR"; //TODO: move this?
			for (int i=0;i<textures.Count;i++)
				GlobalTextures+=textures[i]+",";
			GlobalTextures=GlobalTextures.TrimEnd(',');
			Console.WriteLine ("Texture list loaded");
			Console.WriteLine ("Finished loading");
		}

		private static MapLayer ParseMapLayer (LayerType type,int width, int height, string parseData)
		{
			int[,] data = new int[width, height];
			string[] rows = parseData.Split ('|');
			string[] cols;
			for (int y =0; y < rows.Length;y++) {
				cols = rows[y].Split(',');
				for (int x = 0; x < cols.Length;x++) {
					data[x,y] = Int16.Parse(cols[x]);
					if (!textures.Contains(data[x,y]))
						if (data[x,y]!=0)
							textures.Add (data[x,y]);
				}
			}
			return new MapLayer(type,width,height,data);
		}
		public static void Unload() { //TODO:Call this
			reader.Close();
			reader = null;
			dbcon.Close();
			dbcon = null;
		}

		public static Player Login (string name)
		{
			int id=0;
			bool dm=false;
			List<Character> chars = new List<Character>();
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = string.Format("SELECT ID,DM FROM PLAYER WHERE NAME='{0}'",name.ToUpper());
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				id = reader.GetInt32(0);
				dm = reader.GetBoolean(1);
			}
			if (id==0) return null;
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = string.Format("SELECT ID,NAME,SPRITE,VISIONRANGE,SIZE FROM CHARACTERS WHERE PLAYER='{0}'",id);
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				chars.Add(new Character(reader.GetInt16(0),reader.GetString(1),reader.GetInt16(2),reader.GetInt16(3),reader.GetInt16(4)));
			}
			return new Player(id,chars,name,dm);
		}
		public static Character GetMob (int id, int x, int y)
		{
			Character ret=null;
			string name;
			int sprite,visionrange,size;
			Coord pos = new Coord(x,y);
			if (!Map.withinBounds(pos)) return null;
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = string.Format("SELECT NAME,SPRITE,SIZE,VISIONRANGE FROM MOBS WHERE ID='{0}'",id);
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				name = reader.GetString(0);
				sprite = reader.GetInt32(1);
				size = reader.GetInt32(2);
				visionrange = reader.GetInt32(3);
				ret = new Character(curMobID++,name,sprite,visionrange,size);
				ret.Position= pos;
			}
			return ret;
		}


	}
}

