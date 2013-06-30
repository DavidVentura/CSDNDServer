using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections.Generic;

namespace Server
{


	class MainClass
	{


		public static void Main (string[] args)
		{
			Engine.Initialize();
			Network.Init ();

		}
	
	}
}
