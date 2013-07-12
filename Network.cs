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
						if (curPlayer.chars.Count>0) //DM might have 0 players
							curChar = curPlayer.chars[curCharIndex];
						break;
					case "SPWN"://spawn mob, ID,x ,y
						Character mob = Engine.GetMob(Int32.Parse(args[0]),Int32.Parse(args[1]),Int32.Parse(args[2]));
						if (mob ==null) return; //invalid
						curPlayer.chars.Add(mob);
						if (curChar==null)
							curChar = curPlayer.chars[curCharIndex];
						SendData(clientStream,String.Format("LOGI{0},{1},{2},{3},{4},{5},{6}",mob.Position.X,mob.Position.Y,mob.textureID,mob.ID,mob.Name,mob.VisionRange,mob.Size));
						SendNewPlayer(curPlayer);
						break;
					case "SOBJ": //set the TILE, blocking?, on x,y
						if (curPlayer.isDM) {
							Map.ChangeTile(Int16.Parse(args[0]),Int16.Parse(args[1]),Int16.Parse(args[2]),Int16.Parse(args[3]));
							SendData(String.Format("SOBJ{0},{1},{2}",args[0],args[2],args[3])); //id and pos, blocking is handled server-side
						}
					break;
					case "MOVE":
					if (curChar.Move(new Coord(Int16.Parse(args[0]),Int16.Parse(args[1]))))
						SendMovement(curChar);					
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
					case "INIT": //initiative
						SendInitiative ();
						break;
					case "REFL": //reflexes
						SendReflexes ();
						break;
					case "FORT":
						SendFort ();
						break;
					case "WILL":
						SendWill ();
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
				SendData(clientStream,String.Format("LOGI{0},{1},{2},{3},{4},{5},{6}",c.Position.X,c.Position.Y,c.textureID,c.ID,c.Name,c.VisionRange,c.Size));

			if(p.isDM)
				SendData(clientStream,"DMOK");
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
			foreach (Player p in Players)
				if (p!=newPlayer){
					foreach (Character c in newPlayer.chars)
						SendData (p, String.Format ("NPLR{0},{1},{2},{3},{4},{5}", c.ID, c.Position.X, c.Position.Y, c.textureID, c.Name, c.Size));
						//send the new player's chars to the old players
					foreach (Character old in p.chars)
						SendData (newPlayer, String.Format ("NPLR{0},{1},{2},{3},{4},{5}", old.ID, old.Position.X, old.Position.Y, old.textureID, old.Name, old.Size)); 
						//send the old player's chars to the new one
				}
		}

		static void SendMovement (Character curPlayer)
		{
			string data = String.Format("MPLR{0},{1},{2}",curPlayer.ID,curPlayer.Position.X,curPlayer.Position.Y,curPlayer.textureID);
			foreach (Player p in Players)
				SendData(p.socket.GetStream(),data);
		}

		static void SendText (Character current, string text)
		{
			foreach (Player p in Players)
					SendData(p,String.Format("TALK{0},{1}",current.Name,text));
		}

		static void SendInitiative() {
			string data = "MESSInitiatives:\n";
			foreach (Player p in Players)
				foreach (Character c in p.chars)
					data += c.Name + ": "+c.RollInitiative()+"\n";
			SendData (data);
		}
		static void SendReflexes() {
			string data = "MESSReflexes:\n";
			foreach (Player p in Players)
				foreach (Character c in p.chars)
					data += c.Name + ": "+c.RollReflexes() +"\n";
			SendData (data);
		}
		static void SendFort() {
			string data = "MESSFortitude:\n";
			foreach (Player p in Players)
				foreach (Character c in p.chars)
					data += c.Name + ": "+c.RollFort()+"\n";
			SendData (data);
		}
		static void SendWill() {
			string data = "MESSWill:\n";
			foreach (Player p in Players)
				foreach (Character c in p.chars)
					data += c.Name + ": "+c.RollWill()+"\n";
			SendData (data);
		}

	}
}

