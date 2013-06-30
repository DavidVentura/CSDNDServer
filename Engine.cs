using System;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;

namespace Server
{
	public enum LayerType {
		Ground=0,
		Blocking=1,
		Object=2
	}

	public static class Engine
	{
		public static string GlobalTextures;
		private static List<int> textures = new List<int>();
		private const string ConnectionString = "URI=file:database.db";
		private const int MAPID = 1;

		public static void Initialize ()
		{
			LoadDatabase();
			Map.Initialize(6,6);
		}

		private static void LoadDatabase ()
		{
			IDbConnection dbcon;
			IDataReader reader;
			dbcon = (IDbConnection)new SqliteConnection (ConnectionString);
			dbcon.Open ();
			IDbCommand dbcmd = dbcon.CreateCommand ();

			dbcmd.CommandText = "SELECT WIDTH,HEIGHT FROM MAP WHERE ID=" + MAPID;
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				Map.Initialize(reader.GetInt16 (0), reader.GetInt16 (1));
			} else {
				return; //die
			}
			dbcmd.Dispose ();
			dbcmd.CommandText = "SELECT TYPE,DATA FROM LAYERS WHERE MAPID=" + MAPID;
			reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				Map.AddLayer(ParseMapLayer ((LayerType)reader.GetInt16 (0),  6, 6, reader.GetString (1)));
			}

			GlobalTextures="TXTR"; //TODO: move this?
			for (int i=0;i<textures.Count;i++)
				GlobalTextures+=textures[i]+",";
			GlobalTextures=GlobalTextures.TrimEnd(',');

			dbcmd.Dispose ();
			reader.Close();
			reader = null;
			dbcmd.Dispose();
			dbcmd = null;
			dbcon.Close();
			dbcon = null;
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

	}
}

