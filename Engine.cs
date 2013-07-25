using System;
using System.Data;
using System.Text.RegularExpressions;
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

	public struct Equation {
		static Regex parser = new Regex ("[+-]?((?<dice>(?<mult>[0-9]+)[dD](?<cat>[0-9]+))|(?<number>[0-9]+))", RegexOptions.Compiled);
		string val;
		string multiplier;
		public Equation(string mult, string eq) {
			val = eq;
			multiplier = mult;
		}
		public string Value() {
			int parsedMultiplier = Equation.Parse (multiplier);
			string ret="";
			for (int i =0; i<parsedMultiplier; i++)
				ret += "";
			return ret;
		}
		public static int Parse(string v) {
			MatchCollection matches = parser.Matches (v);
			foreach (Match m in matches)
				foreach(Group g in m.Groups)
				Console.WriteLine ("matches={0}", g);

			return 0;
			//2d4||2||//1d8+2d6+4
		}
	}
	public struct Tile {
		int id;
		string description;
		public Tile (int i, string d)
		{
			id = i;
			description = d;
		}
		public string Get() {
			return id.ToString()+"-"+description+",";
		}
	}

	public static class Engine
	{
		public static int curTurn;
		public static int TotalChars=0;
		public static List<Character> playerIDInit= new List<Character>();
		public static List<Character> Mobs=new List<Character>();
		static List<Tile> Tiles = new List<Tile>();

		public static string Objects;


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
			Console.WriteLine ("Started loading map from DB");
			Equation.Parse ("2d6+7+4d8-16");
			#if LINUX
				dbcon = (IDbConnection)new SqliteConnection (ConnectionString);
			#else
				dbcon = (IDbConnection)new SQLiteConnection (ConnectionString);
			#endif
			dbcon.Open ();

			Console.WriteLine("Loading Tiles");
			GetTiles();
			Console.WriteLine("Tiles loaded");
			Console.WriteLine("Loading Objects");
			GetObjects();
			Console.WriteLine("Objects loaded");


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
			Console.WriteLine("Loading Mobs");
			GetAllMobs();
			Console.WriteLine("Mobs loaded");
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
			TotalChars+=chars.Count;
			return new Player(id,chars,name,dm);
		}
		private static void GetAllMobs ()
		{
			string name;
			int sprite,visionrange,size,id;
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "SELECT ID,NAME,SPRITE,VISIONRANGE,SIZE,WILL,REFLEX,FORTITUDE,CHA,WIS,INT,CON,DEX,STR,INITIATIVE FROM MOBS";
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				id = reader.GetInt16(0);
				name = reader.GetString(1);
				sprite = reader.GetInt32(2);
				visionrange = reader.GetInt32(3);
				size = reader.GetInt32(4);

				Mobs.Add (new Character(id,name,sprite,visionrange,size,
				                    reader.GetInt16(5),reader.GetInt16(6),reader.GetInt16(7), //will, reflex,fort
				                    reader.GetInt16(8),reader.GetInt16(9),reader.GetInt16(10),reader.GetInt16(11),reader.GetInt16(12),reader.GetInt16(13),reader.GetInt16(14)));
			}
		}
		private static void GetTiles ()
		{
			string desc;
			int texture;
			dbcmd = dbcon.CreateCommand ();
			dbcmd.CommandText = "SELECT TEXTURE,DESCRIPTION FROM TILES";
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				texture=reader.GetInt16(0);
				desc=reader.GetString(1);
				Tiles.Add(new Tile(texture,desc));
			}
		}
		public static string SerializeTiles() {
			string ret ="TILE";
			foreach(Tile t in Tiles)
				ret+=t.Get();
			return ret.TrimEnd(',');
		}
		private static void GetObjects ()
		{
			string desc;
			int texture;
			dbcmd = dbcon.CreateCommand ();
			dbcmd.CommandText = "SELECT TEXTURE,DESCRIPTION FROM OBJECTS";
			reader = dbcmd.ExecuteReader ();
			Objects="OBJS";
			while (reader.Read ()) {
				texture=reader.GetInt16(0);
				desc=reader.GetString(1);
				Objects+=texture+"-"+desc+",";
			}
			Objects=Objects.TrimEnd(',');
		}
		public static Character GetMob (int id)
		{
			foreach (Character mob in Mobs)
				if (mob.ID == id) {
					return new Character(mob,curMobID++); //unique char id
				}
			return null;
		}

		internal static void RollInitiative ()
		{
			bool inserted;
			playerIDInit.Clear();
			foreach (Player p in Network.Players)
				foreach (Character c in p.chars) {
					inserted=false;
					c.RollInitiative ();
					for (int i=0; i < playerIDInit.Count;i++)
						if (c.currentInitiative>playerIDInit[i].currentInitiative){
							inserted=true;
							playerIDInit.Insert(i,c);
							break;
						}
					if (!inserted)
						playerIDInit.Add(c);
				}
			curTurn=0;
		}
		public static string InitiativeString ()
		{
			string data="INIT";
			for (int i=0; i<playerIDInit.Count; i++)
				data += playerIDInit [i].Name + " " + playerIDInit [i].currentInitiative + "(" + playerIDInit [i].initiative + "),";
			return data;
		}
		public static void Delay (Character c)
		{
			Character aux;
			for (int i=0; i< playerIDInit.Count-1; i++) { // -1 because I won't switch anything if he is the last one
				if (playerIDInit[i].ID == c.ID){
					aux = playerIDInit[i];
					playerIDInit[i]=playerIDInit[i+1];
					playerIDInit[i+1]=aux;
					return;
				}
			}
		}

	}
}

