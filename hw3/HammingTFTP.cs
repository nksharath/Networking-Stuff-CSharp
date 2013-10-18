/*
 * Main.cs
 * 
 * Version:
 *          $Id$
 * 
 * Revisions:
 *          $Log$
 * 
 */

/*
*@Problem       : To create a HammingTFTP class to transfer a file from a TFTP server with error codes
*@Author        : Sharath Navalpakkam Krishnan : Batch : 4005-740-02
*@Version       : 1.0.3
*@LastModified  : 01/26/2012 11.45 PM
*
*/
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections;

//Defining Namespace TFTPreader
namespace Hamming
{
	class TFTPreader
	{
		// The filestream to write the output
		static FileStream result; 
		//To store the number of remaining bits
		static int remain=0;
		static string remainingbits = "";
		//static StringBuilder remainder=new StringBuilder();
		
		
		//This function checks performs parity check : Eg take 2 skip 2
		static List<bool> parity (BitArray check, int position)
		{
			List<bool> result = new List<bool> ();
			for (int k=position-1; k<31; k=k+position*2) {
				for (int i=0; i<position; i++) {
					result.Add (check [k + i]);
				}
			}
			return result;
			
			
			
		}
		
		//This function checks for the overall parity of the given block
		static int overall (BitArray check)
		{
			int count = 0;
			for (int i=0; i<check.Count; i++)
				if (check [i])
					count++;
			if (count % 2 == 0)
				return 1;
			else {
				//Console.WriteLine ("here0");
				return 0;
			}
		}

		//This function performs various operations on given block
		static int checkerror (List<byte> temp)
		{
			
			byte[] arr = new byte[4];
			//byte[] arr5 = new byte[4];
			arr = temp.ToArray ();
			//arr5 = temp.ToArray ();
			
			//Storing as bit array
			
			BitArray arr1 = new BitArray (arr);
			
			//Flag bit to hold error index
			int errorindex = 0;
			//Checking parity for each of 1 2 4 8 16 parity bits
			for (int i=0; i<4; i++) {
				int counter = 0;
				double posi;
				posi = Math.Pow ((double)2, (double)i);
				List<bool> ret = parity (arr1, (int)posi);
				//Counting the number of 1 bits
				for (int j=0; j<ret.Count; j++)
					if (ret [j])
						counter++;
				//Checking for even parity
				if (counter % 2 != 0)
					errorindex += (int)posi;
			}
			//Flipping bits for 1 bit error based on errro position
			if (errorindex != 0) {
				//Console.WriteLine ("Index" + errorindex);
				if (arr1 [errorindex -1])
					arr1 [errorindex -1] = false;
				else
					arr1 [errorindex -1] = true;
			}
			
			int error = overall (arr1);
			//Console.WriteLine ("Overall error" + error);
			//If burst error , negative acknowledgment is sent from main.
			if (error == 0)
				return 0;
			else {
				if (error == 1) {
					
					
					//Console.WriteLine ("in if");
					
					
					BitArray arr2 = new BitArray (26); 
					//int pos = 0;
					int count = 0;
					// Striping parity bit positions 
					for (double i=0; i<arr1.Length; i++) {
						if (Math.Pow (2, i) == 1 || Math.Pow (2, i) == 2 || Math.Pow (2, i) == 8 || Math.Pow (2, i) == 128
						    || Math.Pow (2, i) == 32768 || Math.Pow (2, i) == 2147483648) {
							//Console.WriteLine("I="+i);
							continue;
						} else {
							//Console.WriteLine("Count="+count);
							arr2 [count] = arr1 [Convert.ToInt32 (i)];
							count++;
							
						}
						
						
						//	pos =(int) Math.Pow (2, i);
						
					}
					// Reversing the bit array
					BitArray arr10 = new BitArray (arr2);
					int l = 0;
					for (int i=arr2.Length-1; i>=0; i--) {
						arr2 [l] = arr10 [i];
						l++;
					}
					
					
					
					
					//BitArray arrnew=new BitArray(arr2.Length+remain);
					arr2.Length += remain;
					//Array.Resize (ref arr2, (arr2.Length + remain));
					int bitCount = 0;
					//Padding remaining bits
					for (int i = arr2.Length - remain; i < arr2.Length; i++) {
						arr2 [i] = (remainingbits [bitCount++] == '1') ? true : false;
					}
					//Local bit array copy for manipulation
					BitArray arr12 = new BitArray (arr2);
					int w = 0;
					for (int i=arr2.Length-1; i>=0; i--)
						arr2 [w++] = arr12 [i];
					
					remain = arr2.Length % 8;
					remainingbits = "";
					for (int i = arr2.Length - remain; i<arr2.Length; i++) {
						remainingbits += arr2 [i] ? "1" : "0";
					}

					char[] b = remainingbits.ToCharArray ();
					Array.Reverse (b);
					string tempNew2 = new string (b);
					remainingbits = tempNew2;
					//Console.WriteLine("remaiingn bits " +remainingbits);

					//Recording the remaining bits to be written to the file
					int numofbytes = (arr2.Length - remain) / 8;
					ArrayList bytesToWrite = new ArrayList ();
					for (int i=0; i<numofbytes; i++) {
						StringBuilder last = new StringBuilder ();
						
						for (int k=(8*i); k<(8*i)+8; k++) {
							last.Append (arr2 [k] ? "1" : "0");
							
						}
						//Performing reverse for local manipulation
						char[] a = last.ToString ().ToCharArray ();
						Array.Reverse (a);
						string tempNew = new string (a);
						last = new StringBuilder (tempNew);
						bytesToWrite.Add (last);
						//Console.WriteLine("Last = "+last.ToString());
						
					}
					
					//Console.WriteLine("function ="+bytesToWrite.Capacity);
					for (int i=0; i<bytesToWrite.Count; i++) {
						
						//Console.Write("boo"+Convert.ToByte(Convert.ToInt32(bytesToWrite[i].ToString(),2)));
						//Writing data to file
						result.WriteByte (Convert.ToByte (Convert.ToInt32 (bytesToWrite [i].ToString (), 2)));
						result.Flush ();
						//Console.WriteLine("function");
						
					}

					return 1;
				}
			}
			return 1;
		}
		public static void Main (string[] args)
		{
			
			//packetposition tracks the bit positions of the RequestPacket
			int packetposition = 0;
			String mode = "";
			String host = "";
			String filename = "";
			String errortype="";
			//RequestPacket to be sent to the server
			byte[] RequestPacket = new byte[516];
			try {
				//Default octet mode
				mode = "octet";
				//Type of error
				errortype=args[0];
				//getting the host name from the user
				host = args [1];
				//getting the filename from the user
				filename = args [2];
				//Creating an output file to transfer the bytes when recieved from server
				result = new FileStream (filename, FileMode.Create);
			} catch (Exception) {
				Console.WriteLine ("Usage: [mono] TFTPhamming.exe [error|noerror] tftp-host file");
				Environment.Exit (0);
			}
			
			byte[] store = new byte[516];
			//byte[] extract=new byte[512];
			//List<byte> extract=new List<byte>();
			
			UdpClient client = new UdpClient ();
			
			//Building the request packet as per headers defined in protocol
			RequestPacket [0] = 0;
			packetposition++;
			//Based on user , we choose opcode 01 or 02 
			if(errortype=="error")
			RequestPacket [1] = 2; 
			if(errortype=="noerror")
				RequestPacket [1] = 1; 
			packetposition = packetposition + 2;
			//The file name is also represented interms of bytes
			packetposition = packetposition + Encoding.ASCII.GetBytes (filename, 0, filename.Length, RequestPacket, 2);
			RequestPacket [packetposition++] = 0;
			//The mode type is also represented interms of bytes
			packetposition = packetposition + Encoding.ASCII.GetBytes (mode, 0, mode.Length, RequestPacket, packetposition);
			RequestPacket [packetposition++] = 0;
			
			//Communicating through port 7000 : Initial Port
			client.Send (RequestPacket, RequestPacket.Length, host, 7000);
			IPEndPoint RemoteIpEndPoint = new IPEndPoint (IPAddress.Any, 0);
			
			try {
				while (true) {
					store = client.Receive (ref RemoteIpEndPoint);
					List<byte> extract=new List<byte>(store);
					//.store = client.Receive (ref RemoteIpEndPoint);
					Console.Write(".");
					byte[] AckPacket = new byte[4];
					

					
					//Checking if the packet sent is an error packet
					if(store[1]==5)
					{
						Console.WriteLine("Error "+Encoding.ASCII.GetString(store));
						client.Close();
						break;
						
					}
					
					//Checking if the transfer is complete, when last data packet size < 516
					if (store.Length < 516) 
					{
						for(int i=1;i<(extract.Count)/4;i++)
						{//Console.WriteLine("EXTRACT COUNT"+store.Length);
							List<byte> block=new List<byte>();
							//Console.WriteLine("going");
							//Console.WriteLine("VALUE I="+i);
							block=extract.GetRange(i*4,4);
							int error=checkerror(block);
						//	Console.WriteLine("In main"+error);
							if(error==1)
								continue;
							if(error==0)
							{//Building the negative acknowledgment packet as per protocol 
								AckPacket [0] = 0;
								AckPacket [1] = 6;
								AckPacket [2] = store [2];
								AckPacket [3] = store [3];
								client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);
								//Console.WriteLine("Negative");
								store = client.Receive (ref RemoteIpEndPoint);
								extract=new List<byte>(store);
								i=0;
								continue;
							}
							

						}
						Console.WriteLine ("Transfer Complete");
						//Sending acknowledgment
						AckPacket [0] = 0;
						AckPacket [1] = 4;
						AckPacket [2] = store [2];
						AckPacket [3] = store [3];
						client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);

						client.Close();

						break;
						
					}else{
						
						for(int i=1;i<(extract.Count)/4;i++)
						{
							List<byte> block=new List<byte>();
							//Console.WriteLine("going");
							block=extract.GetRange(i*4,4);
							int error=checkerror(block);
							if(error==1)
								continue;
							else
							{//Building the acknowledgment packet as per protocol 
								AckPacket [0] = 0;
								AckPacket [1] = 6;
								AckPacket [2] = store [2];
								AckPacket [3] = store [3];
								client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);
								store = client.Receive (ref RemoteIpEndPoint);
								extract=new List<byte>(store);
								i=0;
							}

						}
						//Sending Acknowledgment
						
						AckPacket [0] = 0;
						AckPacket [1] = 4;
						AckPacket [2] = store [2];
						AckPacket [3] = store [3];

						client.Send (AckPacket, AckPacket.Length, RemoteIpEndPoint);
					}
					
					
					
				}
			} catch (Exception) {
				Console.WriteLine ("Error in transmission");
			}
		}
	}
}







