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
		//public static string GlobalTextures;
		private static List<int> textures = new List<int>();
		private const string ConnectionString = "URI=file:database.db";
		private const int MAPID = 1;
		private static IDbConnection dbcon;
		private static IDataReader reader;
		private static IDbCommand dbcmd;
		private static int curMobID=2000;
		private static Random rnd=new Random();

		public static int D10 {
			get { return rnd.Next (1, 10); }
		}
		public static int D6 {
			get { return rnd.Next (1, 6); }
		}
		public static int D12 {
			get { return rnd.Next (1, 12); }
		}
		public static int D20 {
			get { return rnd.Next (1, 20); }
		}

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
			while (reader.Read ()) 
				Map.ParseMapLayer((LayerType)reader.GetInt16 (0),  Map.Width, Map.Height, reader.GetString (1));
			Console.WriteLine ("Layers loaded");

			Console.WriteLine ("Finished loading");
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
			dbcmd.CommandText = string.Format("SELECT ID,NAME,SPRITE,VISIONRANGE,SIZE,WILL,REFLEX,FORTITUDE,CHA,WIS,INT,CON,DEX,STR,INITIATIVE FROM CHARACTERS WHERE PLAYER='{0}'",id);
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				chars.Add(new Character(reader.GetInt16(0),reader.GetString(1),reader.GetInt16(2),reader.GetInt16(3),reader.GetInt16(4), //up to size
				                        reader.GetInt16(5),reader.GetInt16(6),reader.GetInt16(7), //will, reflex,fort
				                        reader.GetInt16(8),reader.GetInt16(9),reader.GetInt16(10),reader.GetInt16(11),reader.GetInt16(12),reader.GetInt16(13),reader.GetInt16(14)));
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
			dbcmd.CommandText = string.Format("SELECT ID,NAME,SPRITE,VISIONRANGE,SIZE,WILL,REFLEX,FORTITUDE,CHA,WIS,INT,CON,DEX,STR,INITIATIVE FROM MOBS WHERE ID='{0}'",id);
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				//don't need the ID
				name = reader.GetString(1);
				sprite = reader.GetInt32(2);
				visionrange = reader.GetInt32(3);
				size = reader.GetInt32(4);

				ret = new Character(curMobID++,name,sprite,visionrange,size,
				                    reader.GetInt16(5),reader.GetInt16(6),reader.GetInt16(7), //will, reflex,fort
				                    reader.GetInt16(8),reader.GetInt16(9),reader.GetInt16(10),reader.GetInt16(11),reader.GetInt16(12),reader.GetInt16(13),reader.GetInt16(14));
				ret.Position= pos;
			}
			return ret;
		}


	}
}

