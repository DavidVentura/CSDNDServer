using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server
{
	public static class Network
	{
		private static TcpListener tcpListener;
		private static List<Player> Players = new List<Player>();
		private static ASCIIEncoding encoder = new ASCIIEncoding();
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
			Character curChar=null;
			Player curPlayer = null;
			NetworkStream clientStream = tcpClient.GetStream ();
			byte[] message = new byte[4096];
			int bytesRead,curCharIndex=0;
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
					case "LOGI": //login
						curPlayer = AddPlayer (tcpClient,args[0]);
						if (curPlayer==null){
							SendData(tcpClient.GetStream(), "ERROAlready logged in or inexistent user");
							tcpClient.Close();
							return;
						}
						Console.WriteLine (String.Format("{0} connected",curPlayer.Name));	
						SendInitialData (curPlayer);
						curChar = curPlayer.chars[curCharIndex];
						break;
					case "MOVE":
					if (curChar.Move(new Coord(Int16.Parse(args[0]),Int16.Parse(args[1])))){
						SendMovement(curChar);
					}
					break;
					case "TALK":
						SendText(curChar,args[0]);
						break;
					case "NOCL": //noclip
						curChar.noclip=!curChar.noclip;
						break;
					case "VISI": //change visibility
						curChar.invisible =!curChar.invisible;
						SendData("VISI"+curChar.ID);
						break;
					case "SWCH": //switch character
						curCharIndex++;
						if(curCharIndex>=curPlayer.chars.Count)
							curCharIndex=0;
						SendData(curPlayer,"SWCH"+curCharIndex);
						curChar = curPlayer.chars[curCharIndex];
						break;

				}
			}
			tcpClient.Close();
			RemovePlayer(curPlayer);
		}

		static void SendInitialData (Player p)
		{
			NetworkStream clientStream = p.socket.GetStream();
			foreach(Character c in p.chars)
				SendData(clientStream,String.Format("LOGI{0},{1},{2},{3},{4},{5},{6}",c.position.X,c.position.Y,c.textureID,c.ID,c.Name,c.VisionRange,c.Size));

			SendData(clientStream,Engine.GlobalTextures);
			SendData(clientStream,LayerToString(LayerType.Ground));
			SendData(clientStream,LayerToString(LayerType.Object));
			SendNewPlayer(p); //send the newly logged user to everyone else, and every logged user to the new one
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

		static Player AddPlayer (TcpClient socket, string name)
		{

			Player p =  Engine.Login(name);
			if (p==null) 
				return null;
			foreach(Player LoggedIn in Players)
				if (LoggedIn.ID == p.ID)
					return null;
			p.socket=socket;
			Players.Add (p);
			return p;
		}
		static void SendData (Player p, string data)
		{
			SendData (p.socket.GetStream(),data);
		}
		static void SendData(NetworkStream ns, string data) {
			data+="|";
			byte[] buffer = encoder.GetBytes(data);
			ns.Write(buffer,0,buffer.Length);
			ns.Flush();
			Thread.Sleep(10);
		}
		static void SendData (string data)
		{
			foreach(Player p in Players)
				SendData(p.socket.GetStream(),data);
		}
		static void RemovePlayer (Player p)
		{
			foreach (Character c in p.chars) {
				foreach (Player curPlayer in Players) {
					if (curPlayer != p)
						SendData (curPlayer, String.Format ("RPLR{0}", c.ID));
				}
			}
			Console.WriteLine(String.Format("{0} Disconnected",p.ID));
			Players.Remove(p);
		}
		static void SendNewPlayer (Player newPlayer)
		{
			//TODO: wat
			foreach (Character c in newPlayer.chars) {
				string data = String.Format ("NPLR{0},{1},{2},{3},{4},{5}", c.ID, c.position.X, c.position.Y, c.textureID, c.Name, c.Size);
				foreach (Player p in Players){
					if (p!=newPlayer){
						foreach (Character old in p.chars)
							if (old != c) {
								SendData (p, data); //send the new player to the old ones
								SendData (newPlayer, String.Format ("NPLR{0},{1},{2},{3},{4},{5}", old.ID, old.position.X, old.position.Y, old.textureID, old.Name, old.Size)); //and the old players to the new one
							}
					}
				}
			}
		}

		static void SendMovement (Character curPlayer)
		{
			string data = String.Format("MPLR{0},{1},{2}",curPlayer.ID,curPlayer.position.X,curPlayer.position.Y,curPlayer.textureID);
			foreach (Player p in Players)
				SendData(p.socket.GetStream(),data);
		}

		static void SendText (Character current, string text)
		{
			foreach (Player p in Players)
					SendData(p,String.Format("TALK{0},{1}",current.Name,text));
		}


	}
}

