using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;


namespace TFTPreader
{
	class TFTPreader
	{
		public static void Main (string[] args)
		{
			int packetposition = 0;
			byte[] RequestPacket = new byte[516];
			String mode = args [0];
			String host = args [1];
			String filename = args [2];
			FileStream result = new FileStream (filename, FileMode.Create);
			byte[] store = new byte[516];
			UdpClient client = new UdpClient ();
			RequestPacket [0] = 0;
			packetposition++;
			RequestPacket [1] = 1; 
			packetposition = packetposition + 2;
			packetposition = packetposition + Encoding.ASCII.GetBytes (filename, 0, filename.Length, RequestPacket, 2);
			RequestPacket [packetposition++] = 0;
			packetposition = packetposition + Encoding.ASCII.GetBytes (mode, 0, mode.Length, RequestPacket, packetposition);
			RequestPacket [packetposition++] = 0;



			//int pos = 0;


			/*
			// Set first Opcode of packet to indicate
			// if this is a read request or write request
			RequestPacket[pos++] = 0;
			RequestPacket[pos++] = 1;
			
			// Convert Filename to a char array
			pos += Encoding.ASCII.GetBytes(filename, 0,filename.Length, RequestPacket, pos);
			RequestPacket[pos++] = 0;
			pos += Encoding.ASCII.GetBytes(mode, 0, mode.Length, RequestPacket, pos);
			RequestPacket[pos] = 0;
			*/




			Console.WriteLine(host);
			client.Send (RequestPacket, RequestPacket.Length, host, 6969);
			IPEndPoint RemoteIpEndPoint = new IPEndPoint (IPAddress.Any, 0);

			Console.WriteLine(Encoding.ASCII.GetString(RequestPacket));






			while (true) {

				store = client.Receive (ref RemoteIpEndPoint);
				Console.WriteLine("here");
				byte[] AckPacket = new byte[4];
				AckPacket [0] = 0;
				AckPacket [1] = 4;
				AckPacket [2] = store [2];
				AckPacket [3] = store [3];
				result.Write (store, 4, store.Length - 4);
				result.Flush ();
				client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);

				if(store[1]==5)
				{
					Console.WriteLine("Error "+Encoding.ASCII.GetString(store));
					break;
				}



				if (store.Length < 516) 
				{
					Console.WriteLine ("Transfer Complete");
					AckPacket [0] = 0;
					AckPacket [1] = 4;
					AckPacket [2] = store [2];
					AckPacket [3] = store [3];
					client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);
					break;
					//store = client.Receive (ref RemoteIpEndPoint);

				}
			}
		}
	}
}







