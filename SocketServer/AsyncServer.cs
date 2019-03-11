using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{
    public class AsyncServer
    {
        //private string ClassName = "[MySocketAsync]";
        private int port;
        //private IUnityContainer container;
        //private ILogger log;

        Socket listener;                        //Usado para reconectar
        IPEndPoint localEndPoint;               //Usado para reconectar
        Socket Sender;                          //Usado para enviar msg do controler
        protected object locker = new object(); //Usado para enfileirar as msg de resposta

        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

        public AsyncServer(int port)
        {
            this.port = port;
            //this.container = container;
            //this.log = container.Resolve<ILogger>();
        }

        public void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = null;
            foreach (var ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ip;
                }
            }
            localEndPoint = new IPEndPoint(ipAddress, port);
            //log.Info($"{ClassName} Initing server...{ipAddress.ToString()}:{port}");

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            WaitForConnections();
        }

        public void WaitForConnections()
        {
            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    //log.Info($"{ClassName} Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                //log.Error(ClassName + " " + e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();
            //log.Info($"{ClassName} Connection with Client established!");

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Sender = state.workSocket;

            int bytesRead = Sender.EndReceive(ar);
            //try
            //{
            //    // Read data from the client socket.
            //    bytesRead = Sender.EndReceive(ar);
            //}
            //catch (Exception e)
            //{
            //    //log.Info($"{ClassName} Lost Connection with Client, Reconecting... {e.Message}");
            //    WaitForConnections();
            //}

            if (bytesRead > 0)
            {
                state.sb.Clear();
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString();
                //log.Info($"{ClassName} New message from Client");

                //DefaultComunicationProtocol msg = null;
                //try
                //{
                //    msg = JsonConvert.DeserializeObject<DefaultComunicationProtocol>(content);
                //}
                //catch (Exception jsonEx)
                //{
                //    log.Error($"Não foi possível interpretar a mensagem\n {jsonEx.Message}");
                //}
                //log.Info($"Evento: {msg.Evento}");


                //IComunicationProtocol request = DefineRequest(msg);

                //if (content.IndexOf("<EOF>") > -1)
                //{
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    Console.WriteLine(DateTime.Now+" Read {0} bytes from socket. \nData : {1}",
                        content.Length, content);
                    // Echo the data back to the client. 
                    Send(content);
                //}
                //else
                //{
                    // Not all data received. Get more.  
                    Sender.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                //}
            }
        }

        public void Send(String data)
        {
            lock (locker)
            {
                if (Sender != null)
                {
                    // Convert the string data to byte data using ASCII encoding.  
                    byte[] byteData = Encoding.ASCII.GetBytes(data);

                    Console.WriteLine(DateTime.Now+" Enviando para client: "+data);                    
                    try
                    {
                        // Begin sending the data to the remote device.  
                        Sender.BeginSend(byteData, 0, byteData.Length, 0,
                            new AsyncCallback(SendCallback), Sender);
                        
                    }
                    catch (Exception e)
                    {
                        //log.Error($"{ClassName} Not possible send response: {byteData.Length} bytes, {e.Message}");
                    }
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //log.Info($"{ClassName} Sent {bytesSent} bytes to client.");
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
                

            }
            catch (Exception e)
            {
                //log.Error(ClassName + e.ToString());
                Console.WriteLine(e.ToString());
            }
        }

    }

    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 99999;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
}
