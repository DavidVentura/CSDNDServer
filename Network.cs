using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Server
{
	public static class Network
	{
		private static TcpListener tcpListener;
		private static List<Player> Players = new List<Player>();
		private static ASCIIEncoding encoder = new ASCIIEncoding();
		private static string LastData="";
		public static List<Player> getPlayers {
			get { return Players; }
		}

		public static void Init ()
		{
			tcpListener = new TcpListener(IPAddress.Any,3000);
			Console.WriteLine ("Server up");
			new Thread(new ThreadStart(ListenForClients)).Start();
			Console.ReadLine();
		}
		public static void ListenForClients(){
			tcpListener.Start() ;
			while(true){
				TcpClient client = tcpListener.AcceptTcpClient();

				Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
				clientThread.Start(client);
			}
		}
		private static void HandleClientComm (object client)
		{
			TcpClient tcpClient = (TcpClient)client;
			Player curPlayer = AddPlayer(tcpClient);
			Console.WriteLine("Player connected");
			NetworkStream clientStream = tcpClient.GetStream ();
			SendInitialData(clientStream);
			byte[] message = new byte[4096];
			int bytesRead;
			while (true) {
				bytesRead=0;
				try{
					bytesRead=clientStream.Read (message,0,4096);
				} catch{ //error
					break;
				}
				if (bytesRead==0)
					break; //client disconnected
				string data = encoder.GetString(message,0,bytesRead);
				data=data.Split('|')[0];
				string header = data.Substring (0,4);
				string[] args = data.Substring (4).Split(',');

				switch(header){
					case "INIP": //initial position
						curPlayer.position=new Vector2(Int16.Parse(args[0]),Int16.Parse(args[1]));
						break;
					case "MOVE":
					if (curPlayer.Move(new Vector2(Int16.Parse(args[0]),Int16.Parse(args[1]))))
						Network.SendData(curPlayer.socket.GetStream(),"POSI"+curPlayer.Position.X+","+curPlayer.Position.Y);
					break;

				}
			}
			tcpClient.Close();
			RemovePlayer(curPlayer);
		}

		static void SendInitialData (NetworkStream clientStream)
		{
			SendData(clientStream,LayerToString(LayerType.Ground));
			SendData(clientStream,LayerToString(LayerType.Object));
			SendData(clientStream,Engine.GlobalTextures);
		}

		static string LayerToString (LayerType t)
		{
			int type =(int)t;
			string data = String.Format ("LAYR{0},{1},{2}", Map.Width, Map.Height, type);
			int[,] tiles = Map.GetLayer (t).tiles;
			for (int x=0; x<Map.Width; x++) {
				data += ",";
				for (int y=0; y<Map.Height; y++) {
					data += tiles [x, y] + "-";
				}
				data = data.TrimEnd ('-');
			}
			return data;
		}

		static Player AddPlayer (TcpClient socket)
		{
			Player p = new Player(socket);
			Players.Add (p);
			return p;
		}
		static void SendData(NetworkStream ns, string data) {
			if (data==LastData) return;
			LastData=data;
			data+="|";
			byte[] buffer = encoder.GetBytes(data);
			ns.Write(buffer,0,buffer.Length);
			ns.Flush();
			Thread.Sleep(20);
		}
		static void SendData (string data)
		{
			foreach(Player p in Players)
				SendData(p.socket.GetStream(),data);
		}
		static void RemovePlayer (Player p)
		{
			Players.Remove(p);
		}

	}
}

