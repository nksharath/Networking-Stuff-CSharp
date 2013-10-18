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
*@Problem       : A FTP Client implementation with several basic commands
*@Author        : Sharath Navalpakkam Krishnan : sxn9447@rit.edu
*@Version       : 1.0.3
*@LastModified  : 09/02/2013 7.45 PM
*
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;


namespace FTP
{
    class FTP
    {
        //Creating a TCPClient connection object
        static TcpClient conn;
        //Creating a TCPClient conenction object for local data transfers
        static TcpClient localconn;
        //Creating a stream reader for communicating with Server
        static StreamReader reader;
        //Creating a stream writer for communicating with Server
        static StreamWriter writer;

        //A debug count to toggle on/off
        static int debugcount = 0;
        //Debug toggle
        static bool debug = false;
        //Commands recieved from user
        static string[] argv=null;
        //Count to toggle between passive and active modes
        static int passivecount = 0;
        //passive toggle
        static bool pass = false;
        //Ascii mode toggle
        static bool ascii = false;

        //Binary mode toggle
        static bool binary = true;
        // static int binarycount = 0;

        //Host name
        static String host = null;
        //Port 
        static int port = 0;
        //A bool to check white space
        static bool checkspace = false;

        //To hold response from server
        static String response;
        //To track code return by server
        static String startcode;
        //Username for ftp
        static String username;
        //Password for ftp
        static String password;

        public const string PROMPT = "FTP> ";

        public static readonly string[] COMMANDS = { "ascii",
					      "binary",
					      "cd",
					      "cdup",
					      "debug",
					      "dir",
					      "get",
					      "help",
					      "passive",
                          "put",
                          "pwd",
                          "quit",
                          "user" };

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;

        public static readonly String[] HELP_MESSAGE = {
	"ascii      --> Set ASCII transfer type",
	"binary     --> Set binary transfer type",
	"cd <path>  --> Change the remote working directory",
	"cdup       --> Change the remote working directory to the",
        "               parent directory (i.e., cd ..)",
	"debug      --> Toggle debug mode",
	"dir        --> List the contents of the remote directory",
	"get path   --> Get a remote file",
	"help       --> Displays this text",
	"passive    --> Toggle passive/active mode",
    "put path   --> Transfer the specified file to the server",
	"pwd        --> Print the working directory on the server",
    "quit       --> Close the connection to the server and terminate",
	"user login --> Specify the user name (will prompt for password" };

        //This function performs initial authentication at startup
        static void F_INIT()
        {
            

            readresponse(reader);

            Console.Write("User<buckaroo.cs.rit.edu:<none>");
            username = Console.ReadLine();
            writer.WriteLine("USER " + username);
            writer.Flush();

            readresponse(reader);
            if (username.Length != 0)
            {
                Console.Write("Password:");
                password = Console.ReadLine();
                writer.WriteLine("PASS " + password);
                writer.Flush();
                readresponse(reader);
            }
        }

        //This function is invoked for command USER
        static void F_USER()
        {

            Console.Write("Username ");
            username = Console.ReadLine();
            if (debug)
                Console.WriteLine("-->USER " + username);
            writer.WriteLine("USER " + username);
            writer.Flush();
            String local = reader.ReadLine();
            Console.WriteLine(local);
            if (local[0] == '3' && local[1] == '3' && local[2] == '1')
            {
                Console.Write("Password ");
                password = Console.ReadLine();
                writer.WriteLine("PASS " + password);
                writer.Flush();
                readresponse(reader);
            }

        }
        //This function is invoked for Command ASCII
        static void F_ASCII()
        {
            if (debug)
                Console.WriteLine("-->TYPE A");
            writer.WriteLine("TYPE A");
            writer.Flush();
            ascii = true;
            binary = false;
            readresponse(reader);
        }
        //This function is invoked for Command BINARY
        static void F_BINARY()
        {
            if (debug)
                Console.WriteLine("-->TYPE I");
            writer.WriteLine("TYPE I");
            writer.Flush();
            ascii = false;
            binary = true;
            readresponse(reader);
        }
        //This function is invoked for Command CD
        static void F_CD()
        {
            String remotedirectory=null;
            if (argv.Length == 1)
            {
                Console.WriteLine("Remote Directory");
                remotedirectory = Console.ReadLine();
            }

            if (debug)
            {
                if (argv.Length == 1)
                    Console.WriteLine("-->CWD " + remotedirectory);
                else
                Console.WriteLine("-->CWD " + argv[1]);
            }
            if (argv.Length == 1)
                writer.WriteLine("CWD " + remotedirectory);
            else
            writer.WriteLine("CWD " + argv[1]);
            writer.Flush();
            readresponse(reader);
        }
        //This function is invoked for Command CDUP
        static void F_CDUP()
        {
            if (debug)
                Console.WriteLine("-->CDUP");
            writer.WriteLine("CDUP");
            writer.Flush();
            readresponse(reader);
        }
        //This function is invoked for Command PWD - Present working directory
        static void F_PWD()
        {
            if (debug)
                Console.WriteLine("-->XPWD ");
            writer.WriteLine("XPWD ");
            writer.Flush();
            readresponse(reader);
        }
        //This function is invoked to display the help message
        static void F_HELP()
        {
            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                Console.WriteLine(HELP_MESSAGE[i]);
        }
        //This function is invoked for Command DEBUG
        static void F_DEBUG()
        {
            if (debugcount == 0)
            {
                debug = true;
                debugcount++;
                Console.WriteLine("Debugging On");
            }
            else
            {
                debug = false;
                debugcount--;
                Console.WriteLine("Debugging off");
            }
        }
        //This function is invoked for Command DIR
        static void F_DIR()
        {
            //When mode is passive
            if (pass)
            {
                writer.WriteLine("PASV");
                if (debug)
                    Console.WriteLine("-->PASV");
                writer.Flush();
                String tempresp = reader.ReadLine();
                String[] ip = tempresp.Split("()".ToCharArray())[1].Split(",".ToCharArray());
                port = ((Convert.ToInt32(ip[4]) * 256) + (Convert.ToInt32(ip[5])));
                for (int i = 0; i < 4; i++)
                {


                    host += ip[i];

                    if (i != 3)
                        host += ".";
                }
                // Console.WriteLine(host+ "\n" +port);
                localconn = new TcpClient(host, port);
                StreamReader localreader = new StreamReader(localconn.GetStream());
                //StreamWriter localwriter = new StreamWriter(localconn.GetStream());
                if (debug)
                    Console.WriteLine("-->LIST");
                if (argv.Length == 1)
                    writer.WriteLine("LIST ");
                else
                    writer.WriteLine("LIST " + argv[1]);
                writer.Flush();
                try
                {
                    //String localresponse = localreader.ReadLine();
                    String localresponse;
                    do
                    {

                        localresponse = localreader.ReadLine();
                        Console.WriteLine(localresponse);

                    } while (!localreader.EndOfStream);
                    localconn.Close();
                    localconn = null;
                    host = null;
                    port = 0;

                    readresponse(reader);
                    readresponse(reader);
                }
                catch (Exception)
                {
                    readresponse(reader);
                    localconn.Close();
                    host = null;
                    port = 0;
                }
            }
            
            //When mode is active
            else
            {
                TcpListener listener = new TcpListener(IPAddress.Any,0);
                listener.Start();
                String tempresp = listener.Server.LocalEndPoint.ToString();




                String[] ip = tempresp.Split(":".ToCharArray());

                String quo = Convert.ToString(Convert.ToInt32(ip[1]) / 256);
                String rem = Convert.ToString(Convert.ToInt32(ip[1]) % 256);

                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                ip = Convert.ToString((localIPs[0])).Split(".".ToCharArray());
                String sendip = null;
                for (int i = 0; i < ip.Length; i++)
                {
                    sendip += ip[i];
                    if (i != 3)
                        sendip += ",";
                }
                sendip += ',' + quo + ',' + rem;

                //Console.WriteLine(sendip);
                if(debug)
                    if (debug)
                        Console.WriteLine("-->PORT "+sendip);
                writer.WriteLine("PORT " + sendip);
                writer.Flush();
                readresponse(reader);
                if (debug)
                    Console.WriteLine("-->LIST");
                if (argv.Length == 1)
                    writer.WriteLine("LIST ");
                else
                    writer.WriteLine("LIST " + argv[1]);
                writer.Flush();
                //readresponse(reader);
                String code = reader.ReadLine();
                Console.WriteLine(code);
                if (code[0] == '4' && code[1] == '5' && code[2] == '0')
                    return;
                //Console.WriteLine("here");
                TcpClient clientSocket = listener.AcceptTcpClient();
                StreamReader localreader = new StreamReader(clientSocket.GetStream());
                String localresponse;
                try
                {
                    do
                    {
                        //Console.WriteLine("here in loop");

                        localresponse = localreader.ReadLine();
                        Console.WriteLine(localresponse);

                    } while (!localreader.EndOfStream);
                    clientSocket.Close();
                    listener.Stop();
                    Console.WriteLine(reader.ReadLine());

                }
                catch (Exception)
                {
                    // readresponse(reader);

                    clientSocket.Close();
                    listener.Stop();
                }
                //readresponse(reader);
            }
        }

        //This function is invoked for Command PASSIVE
        static void F_PASSIVE()
        {
            if (passivecount == 0)
            {
                Console.WriteLine("Passive Mode On");
                pass = true;
                passivecount++;
            }
            else
            {
                Console.WriteLine("Passive Mode Off");
                pass = false;
                passivecount--;
            }

        }

        //This function is invoked for Command GET : To retrieve a file
        static void F_GET()
        {
            //When mode is passive
            if (pass)
            {
                if (debug)
                    Console.WriteLine("-->PASV");
                writer.WriteLine("PASV");
                writer.Flush();
                String tempresp = reader.ReadLine();
                String[] ip = tempresp.Split("()".ToCharArray())[1].Split(",".ToCharArray());
                port = ((Convert.ToInt32(ip[4]) * 256) + (Convert.ToInt32(ip[5])));
                for (int i = 0; i < 4; i++)
                {


                    host += ip[i];

                    if (i != 3)
                        host += ".";
                }
                // Console.WriteLine(host+ "\n" +port);
                localconn = new TcpClient(host, port);
                StreamReader localreader = new StreamReader(localconn.GetStream());
                BufferedStream breader = new BufferedStream(localconn.GetStream());
                //StreamWriter localwriter = new StreamWriter(localconn.GetStream());
                String localfile = null;
                String remotefile = null;
                if (argv.Length == 1)
                {
                    Console.WriteLine("Remote file :");
                    remotefile = Console.ReadLine();
                    Console.WriteLine("Local file : ");
                    localfile = Console.ReadLine();
                    if (debug)
                        Console.WriteLine("-->RETR");
                    writer.WriteLine("RETR " + remotefile);
                }
                else
                {
                    if (debug)
                        Console.WriteLine("-->RETR");
                    writer.WriteLine("RETR " + argv[1]);
                }
                writer.Flush();
                String code = reader.ReadLine();
                Console.WriteLine(code);
                if (code[0] == '5' && code[1] == '5' && code[2] == '0')
                {
                    localconn.Close();
                    host = null;
                    port = 0;
                    return;
                }
                StreamWriter asciifilewriter = null;
                FileStream binaryfilewriter = null;
                if (argv.Length == 1 && ascii)
                    asciifilewriter = new StreamWriter(localfile, true);
                else if (argv.Length > 1 && ascii)
                    asciifilewriter = new StreamWriter(argv[1], true);

                if (argv.Length == 1 && binary)
                    binaryfilewriter = new FileStream(localfile, FileMode.Create);
                else if (argv.Length > 1 && binary)
                    binaryfilewriter = new FileStream(argv[1], FileMode.Create);

                try
                {
                    //String localresponse = localreader.ReadLine();
                    String localresponse;
                    if (ascii)
                    {
                        do
                        {


                            localresponse = localreader.ReadLine();
                            asciifilewriter.WriteLine(localresponse);
                            asciifilewriter.Flush();

                            //Console.WriteLine(localresponse);
                        } while (!localreader.EndOfStream);
                        asciifilewriter.Close();
                    }
                    if (binary)
                    {

                        byte[] byteArray = new byte[4096];
                        int byteCount = -1;

                        while ((byteCount = breader.Read(byteArray, 0, 4096)) != 0)
                        {
                            binaryfilewriter.Write(byteArray, 0, byteCount);
                            binaryfilewriter.Flush();
                        }
                        // Console.WriteLine("out of loop");
                        binaryfilewriter.Close();


                        //break
                        //binaryfilewriter.WriteByte(Convert.ToByte(localreader.ReadLine()));
                        //binaryfilewriter.Flush();
                    }


                    localconn.Close();
                    localconn = null;
                    host = null;
                    port = 0;
                    Console.WriteLine(reader.ReadLine());
                    return;
                    // readresponse(reader);
                }
                catch (Exception)
                {
                    readresponse(reader);
                    localconn.Close();
                    host = null;
                    port = 0;
                }
            }
            
            //When mode is active
            else
            {
                TcpListener listener = new TcpListener(IPAddress.Any,0);
                listener.Start();
                String tempresp = listener.Server.LocalEndPoint.ToString();

                String[] ip = tempresp.Split(":".ToCharArray());

                String quo = Convert.ToString(Convert.ToInt32(ip[1]) / 256);
                String rem = Convert.ToString(Convert.ToInt32(ip[1]) % 256);

                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                ip = Convert.ToString((localIPs[0])).Split(".".ToCharArray());
                String sendip = null;
                for (int i = 0; i < ip.Length; i++)
                {
                    sendip += ip[i];
                    if (i != 3)
                        sendip += ",";
                }
                sendip += ',' + quo + ',' + rem;

                //Console.WriteLine(sendip);
                if (debug)
                    Console.WriteLine("-->PORT "+sendip);
                writer.WriteLine("PORT " + sendip);
                writer.Flush();
                readresponse(reader);
                String remotefile = null;
                String localfile = null;
                if (argv.Length == 1)
                {
                    Console.WriteLine("Remote file :");
                    remotefile = Console.ReadLine();
                    Console.WriteLine("Local file : ");
                    localfile = Console.ReadLine();
                    writer.WriteLine("RETR " + remotefile);
                    if (debug)
                        Console.WriteLine("-->RETR");
                }
                else
                {
                    if (debug)
                        Console.WriteLine("-->RETR");
                    writer.WriteLine("RETR " + argv[1]);
                }
                writer.Flush();
                String code = reader.ReadLine();
                Console.WriteLine(code);
                if (code[0] == '5' && code[1] == '5' && code[2] == '0')
                {
                    return;
                }
                //Console.WriteLine("here");
                TcpClient clientSocket = listener.AcceptTcpClient();
                StreamReader localreader = new StreamReader(clientSocket.GetStream());
                BufferedStream breader = new BufferedStream(clientSocket.GetStream());
                String localresponse;
                StreamWriter asciifilewriter = null;
                FileStream binaryfilewriter = null;
                if (argv.Length == 1 && ascii==true)
                    asciifilewriter = new StreamWriter(localfile, true);
                else if (argv.Length > 1 && ascii)
                    asciifilewriter = new StreamWriter(argv[1], true);

                if (argv.Length == 1 && binary==true)
                    binaryfilewriter = new FileStream(localfile, FileMode.Create);
                else if (argv.Length > 1 && binary)
                    binaryfilewriter = new FileStream(argv[1], FileMode.Create);

                try
                {
                    //String localresponse = localreader.ReadLine();
                    //String localresponse;
                    if (ascii)
                    {
                        do
                        {


                            localresponse = localreader.ReadLine();
                            asciifilewriter.WriteLine(localresponse);
                            asciifilewriter.Flush();

                            //Console.WriteLine(localresponse);
                        } while (!localreader.EndOfStream);
                        asciifilewriter.Close();
                    }
                    if (binary)
                    {

                        byte[] byteArray = new byte[4096];
                        int byteCount = -1;

                        while ((byteCount = breader.Read(byteArray, 0, 4096)) != 0)
                        {
                            binaryfilewriter.Write(byteArray, 0, byteCount);
                            binaryfilewriter.Flush();
                        }
                        // Console.WriteLine("out of loop");
                        binaryfilewriter.Close();


                        //break
                        //binaryfilewriter.WriteByte(Convert.ToByte(localreader.ReadLine()));
                        //binaryfilewriter.Flush();
                    }
                    clientSocket.Close();
                    listener.Stop();
                    Console.WriteLine(reader.ReadLine());
                    return;
                }
                catch (Exception)
                {
                    // readresponse(reader);

                    clientSocket.Close();
                    listener.Stop();
                }

            }
        }


        //This function is invoked for Command QUIT
        static void F_QUIT()
        {
            if (debug)
                Console.WriteLine("-->QUIT");
            writer.WriteLine("QUIT");
            writer.Flush();
            readresponse(reader);
            Environment.Exit(1);
        }

        //This function is invoked to establish connection with given hostname and port
       static void connect(String host, int port)
        {
            conn = new TcpClient(host,port);
            reader = new StreamReader(conn.GetStream());
            writer = new StreamWriter(conn.GetStream());
            Console.WriteLine("Connected to buckaroo.cs.rit.edu.");
        }

       //This function is invoked for retrieving all responses from FTP server
        static void readresponse(StreamReader temp)
        {
            while (true)
            {
                response = temp.ReadLine();

                Console.WriteLine(response);
                
                if (response.Length > 4)
                {
                    startcode = response.Substring(0, 4);
                    checkspace = char.IsWhiteSpace(response[3]);

                }
                if (isNumeric(startcode) && checkspace)
                    break;
            }
            //Console.WriteLine("Out of loop");
         
        }

       //This function checks if the given string is numeric or not
       static bool isNumeric(String temp)
        {
            try
            {
                Convert.ToInt32(temp);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        //Invoking Main method
        static void Main(string[] args)
        {
            
            String input=null;
           
            bool eof=false;
            
            if(args.Length!=1)
            {
                Console.Error.WriteLine( "Usage: [mono] Ftp server" );
                
	            Environment.Exit( 1 );
            }
            connect(args[0], 21);
            F_INIT();

            //To initially set to binary mode 
            writer.WriteLine("TYPE I");
            writer.Flush();
            reader.ReadLine();
           
           
            do
            {
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();
                }
                catch (Exception)
                {
                    eof = true;
                }

                if (!eof && input.Length > 0)
                {
                    int cmd = -1;
                    argv = Regex.Split(input, "\\s+");

                    //To get the command entered
                    for (int i = 0; i < COMMANDS.Length && cmd == -1; i++)
                    {
                        if (COMMANDS[i].Equals(argv[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            cmd = i;
                        }
                    }
                    //Switch case Based on the given command : performs respective operation
                    switch (cmd)
                    {

                        case USER:
                            F_USER();
                            break;

                        case ASCII:
                            F_ASCII();
                            break;

                        case BINARY:
                            F_BINARY();
                            break;

                        case CD:
                            F_CD();
                            break;

                        case CDUP:
                            F_CDUP();
                            break;

                        case PWD:
                            F_PWD();
                            break;

                        case HELP:
                            F_HELP();
                            break;

                        case DEBUG:
                            F_DEBUG();
                            break;

                        case DIR:
                            F_DIR();
                            break;

                        case PASSIVE:
                            F_PASSIVE();
                            break;
                        

                        case GET:
                            F_GET();
                            break;
                       
                        case QUIT:
                            F_QUIT();
                            break;

                        default: Console.WriteLine("Invalid Command");
                            break;

                    }
                }
            } while (!eof);
            }
    }
}
