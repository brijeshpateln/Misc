using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LANShr
{
    public class Server
    {
        
        public Server(Socket s,Socket d,bool f,int i)
        {
            this.flag = f;
            this.clientSocket = s;
            this.destinationSocket = d;
            this.Run();
            this.index = i;
        }
        
        public struct Header
        {
            public string method;
            public string url;
            public string version;
            public string host;
            public string userAgent;
            public string accept;
            public string acceptLanguage;
            public string acceptEncoding;
            public string acceptCharSet;
            public string cookies;
            public string proxyConnection;
            public string range;
            public string contentLength;
            public string contentType;
            public string referrer;
        };
        private Header hdr = new Header();
        string strmsg="";
        private int index;
        bool flag;
        private Socket clientSocket;
        private Socket destinationSocket;
        private Byte[] Buffer = new Byte[4096];
        private Byte[] remoteBuffer = new Byte[4096];
        private Byte[] localBuffer = new Byte[4096];
        private bool IsDisposed = false;
        public void Run()
        {
            if (destinationSocket == null)
            {
                try
                {
                    ReadMessage(this.clientSocket, ref strmsg, this.Buffer);
                    Console.WriteLine(strmsg);
                    parseQuery(strmsg);
                    int port = 80;
                    /*  if (hdr.host == null)
                    {
                        hdr.host = hdr.url.Substring(7, hdr.url.IndexOf('/', 7) - 7) + '\r';
                    }*/
                    if (hdr.method == "CONNECT")
                    {
                        if (hdr.url.IndexOf(':') >= 0)
                        {
                            hdr.host = hdr.url.Substring(0, hdr.url.IndexOf(':')) + '\r';
                            port = int.Parse(hdr.url.Substring(hdr.url.IndexOf(':') + 1, hdr.url.Length - (hdr.url.IndexOf(':') + 1)));
                        }
                        else
                        {
                            hdr.host = hdr.host.Substring(0,hdr.host.IndexOf(':'));
                            port = 443;
                        }
                    }
                    else if (hdr.url.IndexOf(':', 7) > 0 && hdr.url.IndexOf(':', 7) < hdr.url.IndexOf('/', 7))
                    {
                        port = int.Parse(hdr.url.Substring(hdr.url.IndexOf(':', 7) + 1, hdr.url.IndexOf('/', 8) - hdr.url.IndexOf(':', 7) - 1));
                    }
                    IPEndPoint endpnt;
                    if (port == 8090)
                    {
                        endpnt = new IPEndPoint(IPAddress.Parse("10.100.56.55"), port);
                    }
                    else
                    {
                       
                        endpnt = new IPEndPoint(Dns.Resolve(hdr.host.Substring(0, hdr.host.Length - 1)).AddressList[0], port);
                       
                    }
                    destinationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    destinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    destinationSocket.BeginConnect(endpnt, new AsyncCallback(this.OnConnect), this.destinationSocket);
                }
                catch
                {
                    if (!IsDisposed)
                    { Dispose(); }
            
                }
            }
            else
            {
                string x;
                ReadMessage(this.clientSocket, ref strmsg, this.Buffer);
                x = getQuery();
                this.destinationSocket.BeginSend(Encoding.ASCII.GetBytes(x), 0, x.Length, SocketFlags.None, new AsyncCallback(this.OnQuerySent), this.destinationSocket);
            }
        }
        private void OnConnect(IAsyncResult ar)
        {
                this.destinationSocket.EndConnect(ar);
                string x;
                if (hdr.method == "CONNECT")
                {
                    x = "HTTP/1.1 200 Connection-Established\r\n\r\n";
                    this.clientSocket.BeginSend(Encoding.ASCII.GetBytes(x), 0, x.Length, SocketFlags.None, new AsyncCallback(this.OnOkSent), this.destinationSocket);
                  
                }
                else
                {
                    
                    //HTTP
                    x = getQuery();
                    this.destinationSocket.BeginSend(Encoding.ASCII.GetBytes(x), 0, x.Length, SocketFlags.None, new AsyncCallback(this.OnQuerySent), this.destinationSocket);
                }

        }
        private string getQuery()
        {
            string str = "";
            string[] tokens = this.strmsg.Split('\n');
            foreach (string sc in tokens)
            {
                
                if (sc.Length>6 && (sc.Substring(0,6).Equals("Proxy-") || sc.Substring(0,5).Equals("Keep-")))
                {
                    continue;
                }
                if (!sc.Contains("Referer:") && !sc.Contains("Host:"))
                {
                    str += sc.Replace(hdr.host.Substring(0, hdr.host.Length - 1), destinationSocket.RemoteEndPoint.ToString()) + "\n";
                }
                else
                {
                    str += sc + "\n";
                }
            }
            return str;
        }
        private void OnOkSent(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
                startRelay();
            }
            catch
            {
                if (!IsDisposed)
                { Dispose(); }
            }
        }
        private void OnQuerySent(IAsyncResult ar)
        {
            try
            {
                if (this.destinationSocket.EndSend(ar) == -1)
                {
                    if (!IsDisposed)
                    { Dispose(); }
                }
                else
                {
                    this.startRelay();
                }
            }
            catch
            {
                if (!IsDisposed)
                { Dispose(); }
            }
            
        }
        private void startRelay()
        {
            try
            {
                this.destinationSocket.BeginReceive(remoteBuffer, 0, 4096, SocketFlags.None, new AsyncCallback(this.OnRemoteRecieve), this.destinationSocket);
                this.clientSocket.BeginReceive(localBuffer, 0, 4096, SocketFlags.None, new AsyncCallback(this.OnClientRecieve), this.clientSocket);
            }
            catch
            {
                if (!IsDisposed)
                { Dispose(); }
            }
        }
        private void OnRemoteRecieve(IAsyncResult ar)
        {
            try
            {
                int Ret = this.destinationSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    if (!IsDisposed)
                    { Dispose(); }
                    return;
                }
                if (flag)
                {
                    Form1.updateshareDownload(Ret);
                }
                else
                {
                    Form1.updatelocalDownload(Ret);
                }
            
                this.clientSocket.BeginSend(remoteBuffer, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnClientSent), this.clientSocket);
            }
            catch
            {
                if (!IsDisposed)
                { Dispose(); }
            }
        }
        private void OnClientRecieve(IAsyncResult ar)
        {
            try
            {
                int Ret = this.clientSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    if (!IsDisposed)
                    { Dispose(); }
                    return;
                }
                this.destinationSocket.BeginSend(localBuffer, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), this.destinationSocket);
               
                
            }
            catch
            {
                if (!IsDisposed)
                { Dispose(); }
            }
        }
        protected void OnClientSent(IAsyncResult ar)
        {
            try
            {
                int Ret = this.clientSocket.EndSend(ar);
                if (Ret > 0)
                {
                   
                    this.destinationSocket.BeginReceive(remoteBuffer, 0, 4096, SocketFlags.None, new AsyncCallback(this.OnRemoteRecieve), this.destinationSocket);
                    return;
                }
            }
            catch { }
            if (!IsDisposed)
            { Dispose(); }
        }
        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                int Ret = this.destinationSocket.EndSend(ar);
                if (Ret > 0)
                {
       
                    this.clientSocket.BeginReceive(localBuffer, 0, 4096, SocketFlags.None, new AsyncCallback(this.OnClientRecieve), this.clientSocket);
                    return;
                }
            }
            catch { }
            if (!IsDisposed)
            { Dispose(); }
        }


        public void Dispose()
        {
            try
            {
                this.clientSocket.Shutdown(SocketShutdown.Both);
                this.destinationSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                this.clientSocket.Close();
                this.destinationSocket.Close();
                if (flag == true)
                {
                    LANShr.Form1.snds.Push(index);
                }
                else
                {
                    LANShr.Form1.lnds.Push(index);
                }
                Form1.updateotherClients();
            }
            catch { }
            this.clientSocket = null;
            this.destinationSocket = null;
            this.remoteBuffer = null;
            this.localBuffer = null;
            this.IsDisposed = true;
        }
        private int ReadMessage(Socket s, ref string strmsg, Byte[] buff)
        {
            int bytesRead = 0;
            int n = 1;
            while (n != 0)
            {
                try
                {
                    n = s.Receive(buff);
                }
                catch (SocketException)
                {
                    strmsg += Encoding.ASCII.GetString(buff, 0, n);
                    bytesRead += n;
                    break;
                }
                strmsg += Encoding.ASCII.GetString(buff, 0, n);
                bytesRead += n;
                if (buff[n] == '\0')
                {
                    break;
                }
            }
            return bytesRead;
        }
        private void parseQuery(string query)
        {
            char[] delim = { '\n' };
            string[] lns = query.Split(delim);
            int index1 = lns[0].IndexOf(' ');
            int index2 = lns[0].IndexOf(' ', index1 + 1);
            this.hdr.method = lns[0].Substring(0, index1);
            this.hdr.url = lns[0].Substring(index1 + 1, index2 - index1);
            this.hdr.version = lns[0].Substring(index2 + 1);
            int i = 1;
            while (i < lns.Length)
            {
                int x = lns[i].IndexOf(' ');
                if (lns[i]!="" && lns[i] != "\r" && lns[i][0] != '\0' && x>0)
                {
                    switch (lns[i].Substring(0, x))
                    {
                        case "Host:": { hdr.host = lns[i].Substring(x + 1); break; }
                        case "User-Agent:": { hdr.userAgent = "User-Agent: " + lns[i].Substring(x + 1); break; }
                        case "Accept:": { hdr.accept = "Accept: "+lns[i].Substring(x + 1); break; }
                        case "Accept-Language:": { hdr.acceptLanguage = "Accept-Language: "+lns[i].Substring(x + 1); break; }
                        case "Accept-Encoding:": { hdr.acceptEncoding = "Accept-Encoding: "+lns[i].Substring(x + 1); break; }
                        case "Accept-Charset:": { hdr.acceptCharSet = "Accept-CharSet: "+lns[i].Substring(x + 1); break; }
                        case "Cookie:": { hdr.cookies = "Cookie: "+lns[i].Substring(x + 1); break; }
                        case "Proxy-Connection:": { hdr.proxyConnection = "Proxy-Connection: "+lns[i].Substring(x + 1); break; }
                        case "Range:": { hdr.range = "Range: "+lns[i].Substring(x + 1); break; }
                        case "Content-Length:": { hdr.contentLength = "Content-Length: "+lns[i].Substring(x + 1); break; }
                        case "Content-Type:": { hdr.contentType = "Content-Type: "+lns[i].Substring(x + 1); break; }
                        case "Referer:": { hdr.referrer = "Referer: "+lns[i].Substring(x + 1); break; }
                    }
                }
                i++;

            }
        }
    }
}

