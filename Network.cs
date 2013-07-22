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
		public static List<Player> Players = new List<Player>();
		private static ASCIIEncoding encoder = new ASCIIEncoding();
		public static void Init ()
		{
			tcpListener = new TcpListener(IPAddress.Any,30000);
			Console.WriteLine ("Server up");
			new Thread(new ThreadStart(ListenForClients)).Start();
			Console.ReadLine();
		}
		private static void ListenForClients(){
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
					case "LOGI": //login
						curPlayer = AddPlayer (tcpClient,args[0]);
						if (curPlayer==null){
							SendData(tcpClient.GetStream(), "ERROAlready logged in or inexistent user");
							tcpClient.Close();
							return;
						}
						Console.WriteLine (String.Format("{0} connected",curPlayer.Name));	
						SendInitialData (curPlayer);
						if (curPlayer.chars.Count > 0){
							SendData(clientStream, "SWCH" + curPlayer.chars[0].ID);//assign the first player upon logging in
							curChar=curPlayer.chars[0];
						}
						break;
					case "SPWN"://spawn mob, ID,x ,y
						Character mob = Engine.GetMob(Int32.Parse(args[0]));
						if (mob ==null) break; //invalid
						mob.Position=new Coord(Int32.Parse(args[1]),Int32.Parse(args[2]));
						if (!Map.ValidPosition(mob.Position,mob)) break;
						Engine.TotalChars++;
						curPlayer.chars.Add(mob);
						SendData(clientStream,String.Format("LOGI{0},{1},{2},{3},{4},{5},{6}",mob.ID,mob.Position.X,mob.Position.Y,mob.textureID,mob.Name,mob.Size,mob.VisionRange));
						SendNewPlayer(curPlayer);
						break;
					case "TILE":
						Map.ChangeTile(Int16.Parse(args[0]),Int16.Parse(args[1]),Int16.Parse(args[2])); //ID,X,Y
						SendData(String.Format("CTIL{0},{1},{2}",args[0],args[1],args[2]));
						break;
					case "SOBJ": //set the TILE, blocking?, on x,y
						if (curPlayer.isDM) {
							Map.ChangeObject(Int16.Parse(args[0]),Int16.Parse(args[1]),Int16.Parse(args[2]),Int16.Parse(args[3]));
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
						foreach (Character c in curPlayer.chars)
							if (c.ID == Int32.Parse(args[0])){
								SendData(curPlayer,"SWCH"+c.ID);
								curChar=c;
								break;
							}
						break;
					case "INIT": //initiative
						Engine.RollInitiative();
						SendData(Engine.InitiativeString());
						SendData("CURT0");
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
					case "DMMD": //DM mode
						curChar = null;
						break;
					case "NEXT": //Next turn
						Engine.curTurn++;
						if (Engine.curTurn >= Engine.TotalChars)
							Engine.curTurn=0;
						SendData("CURT"+Engine.curTurn);
						break;
					case "DELA": //delay
						Engine.Delay(curChar);
						SendData(Engine.InitiativeString());
						break;

				}
			}
			tcpClient.Close();
			RemovePlayer(curPlayer);
		}
		static void SendInitialData (Player p)
		{
			NetworkStream clientStream = p.socket.GetStream ();
			foreach (Character c in p.chars)
				SendData (clientStream, String.Format ("LOGI{0},{1},{2},{3},{4},{5},{6}", c.ID, c.Position.X, c.Position.Y, c.textureID, c.Name, c.Size, c.VisionRange));


			if (p.isDM) {
				SendData (clientStream, "DMOK");
				string s="MOBS";
				foreach(Character mob in Engine.Mobs)
					s+=String.Format("{0}-{1},",mob.ID,mob.Name);
				SendData(clientStream,s);
				SendData(clientStream,Engine.Objects);
				SendData(clientStream,Engine.SerializeTiles());
			}
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
			if (!data.EndsWith("|")) data+="|";
			byte[] buffer = encoder.GetBytes(data);
			ns.Write(buffer,0,buffer.Length);
			ns.Flush();
			Thread.Sleep(10);
		}
		internal static void SendData (string data)
		{
			foreach (Player p in Players) {
				if (p.socket.Connected)
					SendData (p.socket.GetStream (), data);
			}
		}
		static void RemovePlayer (Player p)
		{
			foreach (Character c in p.chars)
				foreach (Player curPlayer in Players)
					if (curPlayer != p)
						SendData (curPlayer, String.Format ("RPLR{0}", c.ID));			
			
			SendData(String.Format("MESS{0} disconnected.",p.Name));
			Console.WriteLine(String.Format("{0} Disconnected",p.Name));
			Engine.TotalChars-=p.chars.Count;
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
			string n;
			if (current == null)
				n = "DM";
			else
				n = current.Name;
			foreach (Player p in Players)
					SendData(p,String.Format("TALK{0},{1}",n,text));
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