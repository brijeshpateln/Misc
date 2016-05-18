using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace LANShr
{
    public partial class Form1 : Form
    {
        public struct acc
        {
            public string username;
            public string password;
        }
        public int maxShareLimit;
        public int shareUsed;
        static public int shareDownload;
        static public int localDownload;
        public int reloginTimeOut;
        public int refreshTime;
        public bool automaticSharing;
        public int strttime;
        public int stptime;
        public int maxslots;
        public bool forceGet;
        public acc[] accountList;
        public int numberOfAccounts=0;
        Socket slocal, sshare;
        Server[] lNodes;
        Server[] sNodes;
        public static Pool lnds;
        public static Pool snds;
        public static int otherclients;
        public string loginStat;
        public bool acceptFlag;
        System.Timers.Timer time;
        UdpClient uclient;
        bool useFlag;
        int count;
        public Form1()
        {
            InitializeComponent();
            sharingBtn.Enabled = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Height = this.BackgroundImage.Height;
            this.Width = this.BackgroundImage.Width;
            this.Region = BitmapToRegion.getRegionFast((Bitmap)this.BackgroundImage, Color.FromArgb(0, 255, 0), 100);
            this.closeBtn.Height = this.closeBtn.BackgroundImage.Height;
            this.closeBtn.Width = this.closeBtn.BackgroundImage.Width;
            this.closeBtn.Region = BitmapToRegion.getRegionFast((Bitmap)this.closeBtn.BackgroundImage, Color.FromArgb(0, 255, 0), 100);
            this.miniBtn.Height = this.miniBtn.BackgroundImage.Height;
            this.miniBtn.Width = this.miniBtn.BackgroundImage.Width;
            this.miniBtn.Region = BitmapToRegion.getRegionFast((Bitmap)this.miniBtn.BackgroundImage, Color.FromArgb(0, 255, 0), 100);
            accountList = new acc[20];
            this.loadUser();
            this.loadSettings();
            this.loadAccounts();
            lNodes = new Server[50];
            lnds = new Pool(50);
            sNodes = new Server[25];
            snds = new Pool(25);
            otherclients = 0;
            shareDownload = 0;
            localDownload = 0;
            loginStat = "Login";
            acceptFlag = false;
            useFlag = false;
            count = 0;
            time = new System.Timers.Timer();
            time.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
            time.Interval = 1000;
            time.Start();
            CheckForIllegalCrossThreadCalls = false;
            uclient = new UdpClient(4001, AddressFamily.InterNetwork);
            Thread udpthread = new Thread(UDPHandler);
            udpthread.Start();
            this.startLocalService();
            maxslots = 50;
        }
        Point lastPoint;
        public void loadSettings()
        {
            string temppath = Path.GetTempPath();
            string[] lines = new string[10]; 
            if (!File.Exists(temppath + ".lanshar.conf"))
            {
                File.Create(temppath + ".lanshar.conf").Close();
                loadDefault(null,null);
            }
            else
            {
                StreamReader confReader = new StreamReader(temppath + ".lanshar.conf");
                for (int i = 0; i < 10; i++)
                {
                    lines[i] = confReader.ReadLine();
                }
                shareLimit.Text = lines[0];
                maxShareLimit = int.Parse(lines[0]);
                loginTimeOut.Text = lines[1];
                reloginTimeOut = int.Parse(lines[1]);
                refreshTimeOut.Text = lines[2];
                refreshTime = int.Parse(lines[2]);
                if (int.Parse(lines[3]) == 1)
                {
                    automaticSharing = true;
                    doStartSharing.Checked = true;
                }
                else { automaticSharing = false; doStartSharing.Checked = false; }
                numericUpDown1.Value = int.Parse(lines[5]);
                if (lines[6] == "PM")
                {
                    listBox1.SelectedIndex = 1;
                    strttime = int.Parse(lines[5]) + 12;
                }
                else { listBox1.SelectedIndex = 0; strttime = int.Parse(lines[5]); }
                numericUpDown2.Value = int.Parse(lines[7]);
                if (lines[8] == "PM")
                {
                    listBox2.SelectedIndex = 1;
                    stptime = int.Parse(lines[7]) + 12;
                }
                else { listBox2.SelectedIndex = 0; stptime = int.Parse(lines[7]); }
                if (int.Parse(lines[4]) == 1)
                {
                    timeIntervalChk.Checked = true;
                }
                else
                {
                    timeIntervalChk.Checked = false;
                    timeLabel.Enabled = false;
                    numericUpDown1.Enabled = false;
                    listBox1.Enabled = false;
                    numericUpDown2.Enabled = false;
                    listBox2.Enabled = false;
                }
                slots.Value = int.Parse(lines[9]);
                maxslots = int.Parse(lines[9]);
                confReader.Close();
            }
        }
        public void loadDefault(object sender, EventArgs e)
        {
            shareLimit.Text = "50";
            maxShareLimit = 50;
            loginTimeOut.Text = "30";
            reloginTimeOut = 30;
            refreshTimeOut.Text = "3";
            refreshTime = 3;
            automaticSharing = true;
            doStartSharing.Checked = true;
            timeIntervalChk.Checked = false;
            timeLabel.Enabled = false;
            numericUpDown1.Enabled = false;
            listBox1.Enabled = false;
            numericUpDown2.Enabled = false;
            listBox2.Enabled = false;
            slots.Value = 3;
            maxslots = 3;
            saveSettings(sender,e);
        }
        public void saveSettings(object sender, EventArgs e)
        {
            string temppath = Path.GetTempPath();
            if (!File.Exists(temppath + ".lanshar.conf"))
            {
                File.Create(temppath + ".lanshar.conf").Close();
            }
            string y;
            y = shareLimit.Text + "\n" + loginTimeOut.Text + "\n" + refreshTimeOut.Text + "\n";
            if (doStartSharing.Checked == true)
            {
                y = y + "1\n";
            }
            else { y = y + "0\n"; }
            if (timeIntervalChk.Checked == true)
            {
                y = y + "1\n";
            }
            else { y = y + "0\n"; }
            y = y + numericUpDown1.Value + "\n";
            if(listBox1.SelectedIndex==0)
            {
                y=y+"AM\n"+numericUpDown2.Value+"\n";
            }else{y=y+"PM\n"+numericUpDown2.Value+"\n";}
            if (listBox2.SelectedIndex == 0)
            {
                y = y + "AM\n" + slots.Value;
            }
            else { y = y + "PM\n" + slots.Value; }
            StreamWriter writer = new StreamWriter(temppath + ".lanshar.conf");
            writer.Write(y);
            writer.Close();
            
        }
        public void loadUser()
        {
            string temppath = Path.GetTempPath();
            string[] lines = new string[3]; 
            if (!File.Exists(temppath + ".LANuser.conf"))
            {
                File.Create(temppath + ".LANuser.conf").Close();
                StreamWriter writer = new StreamWriter(temppath + ".LANuser.conf");
                string y = "0\n" + userName.Text + "\n" + password.Text;
                writer.Write(y);
                writer.Close();
            }
            else
            {
                StreamReader reader = new StreamReader(temppath + ".LANuser.conf");
                for (int i = 0; i < 3; i++)
                {
                    lines[i] = reader.ReadLine();
                }
                if (int.Parse(lines[0]) == 1)
                {
                    rememberChk.Checked = true;
                    userName.Text = lines[1];
                    password.Text = lines[2];
                }
                reader.Close();
            }
        }
        public void loadAccounts()
        {
            string temppath = Path.GetTempPath();
            if (!File.Exists(temppath + ".LANaccs.conf"))
            {
                File.Create(temppath + ".LANaccs.conf").Close();
            }
            else
            {
                StreamReader reader = new StreamReader(temppath + ".LANaccs.conf");
                int i = 0;
                reader.ReadLine();
                numberOfAccounts = 0;
                while (!reader.EndOfStream && i<20)
                {
                    accountList[i].username = reader.ReadLine();
                    accountList[i].password = reader.ReadLine();
                    accounts.Items.Add(accountList[i].username);
                    numberOfAccounts++;
                    i++;
                }
                reader.Close();
            }
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }
        private void optBtn_Click(object sender, EventArgs e)
        {
            helpPanel.Visible = false;
            loginPanel.Visible = false;
            accountsPanel.Visible = false;
            optionsPanel.Visible = true;
        }
        private void loginBtn_Click(object sender, EventArgs e)
        {
            helpPanel.Visible = false;
            optionsPanel.Visible = false;
            accountsPanel.Visible = false;
            loginPanel.Visible = true;
        }
        private void closeBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void miniBtn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipTitle = "Message from LANShare App..";
            notifyIcon.BalloonTipText = "LANShare Minimized....";
            notifyIcon.ShowBalloonTip(500);
            this.Hide();
        }
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            notifyIcon.Visible = false;
            this.Show();
            WindowState = FormWindowState.Normal;
        }
        private void accountsBtn_Click(object sender, EventArgs e)
        {
            helpPanel.Visible = false;
            loginPanel.Visible = false;
            optionsPanel.Visible = false;
            accountsPanel.Visible = true;
        }
        private void helpBtn_Click(object sender, EventArgs e)
        {
            loginPanel.Visible = false;
            optionsPanel.Visible = false;
            accountsPanel.Visible = false;
            helpPanel.Visible = true;
        }
        private void timeIntervalChk_CheckedChanged(object sender, EventArgs e)
        {
            if (timeIntervalChk.Checked == true)
            {
                timeLabel.Enabled = true;
                numericUpDown1.Enabled = true;
                listBox1.Enabled = true;
                numericUpDown2.Enabled = true;
                listBox2.Enabled = true;
            }
            else
            {
                timeLabel.Enabled = false;
                numericUpDown1.Enabled = false;
                listBox1.Enabled = false;
                numericUpDown2.Enabled = false;
                listBox2.Enabled = false;
            }
        }
        private void saveUser(object sender, EventArgs e)
        {
            try
            {
                string temppath = Path.GetTempPath();
                string y;
                if (rememberChk.Checked == true)
                {
                    y = "1\n";
                }
                else { y = "0\n"; }
                StreamWriter writer = new StreamWriter(temppath + ".LANuser.conf");
                y = y + userName.Text + "\n" + password.Text;
                writer.Write(y);
                writer.Close();
            }
            catch { }
        }
        private void addAccount(object sender, EventArgs e)
        {
            string temppath = Path.GetTempPath();
            if(numberOfAccounts<20)
            {
                accountList[numberOfAccounts].username = aUsername.Text;
                accountList[numberOfAccounts].password = aPassword.Text;
                FileStream f = new FileStream(temppath + ".LANaccs.conf",FileMode.Append);
                f.Write(Encoding.ASCII.GetBytes("\n"+accountList[numberOfAccounts].username), 0, accountList[numberOfAccounts].username.Length+1);
                f.Write(Encoding.ASCII.GetBytes("\n"+accountList[numberOfAccounts].password), 0, accountList[numberOfAccounts].password.Length+1);
                accounts.Items.Add(accountList[numberOfAccounts].username);
                numberOfAccounts++;
                f.Close();
                aUsername.Text = "";
                aPassword.Text = "";
            }
        }
        private void removeAccount(object sender, EventArgs e)
        {
            int temp = accounts.SelectedIndex;
            accountList[accounts.SelectedIndex].username = "-1";
            accountList[accounts.SelectedIndex].password = "-1";
            accounts.Items.Clear();
            accounts.Text = "";
            numberOfAccounts--;
            string temppath = Path.GetTempPath();
            FileStream f = new FileStream(temppath + ".LANaccs.conf",FileMode.Truncate);
            int i = 0;
            while (i <= numberOfAccounts)
            {
                if (accountList[i].username == "-1" || i==temp)
                {
                    i++;
                    continue;
                }
                else
                {
                    f.Write(Encoding.ASCII.GetBytes("\n" + accountList[i].username), 0, accountList[i].username.Length + 1);
                    f.Write(Encoding.ASCII.GetBytes("\n" + accountList[i].password), 0, accountList[i].password.Length + 1);
                    i++;
                }

            }
            f.Close();
            loadAccounts();
        }
        private void fGetBtn_Click(object sender, EventArgs e)
        {
            forceGet = !forceGet;
        }
        private void startLocalService()
        {
            IPAddress ip = IPAddress.Loopback;
            IPEndPoint edpnt = new IPEndPoint(ip, 1065);
            slocal = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            slocal.Bind(edpnt);
            slocal.Listen(50);
            slocal.BeginAccept(new AsyncCallback(OnAcceptLocal), slocal);
        }
        private void OnAcceptLocal(IAsyncResult ar)
        {
            try
            {
                Socket cl = slocal.EndAccept(ar);
                slocal.BeginAccept(new AsyncCallback(OnAcceptLocal), slocal);
                if (count > slotRatio.Value && useFlag)
                {
                    count = 0;
                    int i=0;
                    var timeToWait = TimeSpan.FromSeconds(10);
                    while (i < dataTable.Rows.Count)
                    {
                        string r = dataTable.Rows[i][0].ToString();
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(r), 4001);
                        var udpClient = new UdpClient();
                        udpClient.Send(Encoding.ASCII.GetBytes("OK?"), 3, endPoint);
                        var asyncResult = udpClient.BeginReceive(null, null);
                        asyncResult.AsyncWaitHandle.WaitOne(timeToWait);
                        try
                        {
                            IPEndPoint remoteEP = null;
                            byte[] receivedData = udpClient.EndReceive(asyncResult, ref remoteEP);
                            if (Encoding.ASCII.GetString(receivedData).Contains("YES"))
                            {
                                Socket frw = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                frw.Connect(IPAddress.Parse(r), 1065);
                                int freeIndex = lnds.Pop();
                                if (freeIndex != -1)
                                {
                                    lNodes[freeIndex] = new Server(cl, frw, false, freeIndex);
                                }
                            }
                        }
                        catch { }

                    }
                    
                }
                else
                {
                    int freeIndex = lnds.Pop();
                    if (freeIndex != -1)
                    {
                        lNodes[freeIndex] = new Server(cl, null, false, freeIndex);
                    }
                    count++;
                }
            }
            catch { }
        }
        private void stoplocalservice()
        {
            slocal.Close();
        }
        private void sharingBtn_Click(object sender, EventArgs e)
        {
            if (sharingBtn.Text == "Start Sharing")
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ip;
                int j = 0;
                for (j = 0; j < host.AddressList.Length; j++)
                {
                    if (host.AddressList[j].AddressFamily.ToString() == "InterNetwork")
                    {
                        break;
                    }
                }
                ip = host.AddressList[j];
                IPEndPoint endpoint = new IPEndPoint(ip, 1065);
                sshare = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sshare.Bind(endpoint);
                sshare.Listen(50);
                sshare.BeginAccept(new AsyncCallback(OnAccept), sshare);
                sharingBtn.Text = "Stop Sharing";
            }
            else
            {
                sshare.Close();
                sharingBtn.Text = "Start Sharing";
            }
        }
        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket cl = sshare.EndAccept(ar);
                otherclients++;
                Console.Out.WriteLine("Otherclients = " + otherclients);
                if (otherclients <= maxslots)
                {
                    sshare.BeginAccept(new AsyncCallback(OnAccept), sshare);
                }
                else
                {
                    acceptFlag = true;
                }
                int freeIndex = snds.Pop();
                sNodes[freeIndex] = new Server(cl, null, true,freeIndex);
            }
            catch
            {
                sshare.Close();
            }
        }
        public static void updateotherClients()
        {
            otherclients--;
        }
        public static void updateshareDownload(int bytes)
        {
            shareDownload += bytes;
        }
        public static void updatelocalDownload(int bytes)
        {
            localDownload += bytes;
        }
        private void usrLoginBtn_Click(object sender, EventArgs e)
        {
            if (userName.Text!= null)
            {
                Byte[] cybreply = new Byte[4096];
                string id;
                id = userName.Text + "%40da-iict.org";
                string y;
                string x = "POST http://10.100.56.55:8090/corporate/servlet/CyberoamHTTPClient HTTP/1.1\r\nHost: 10.100.56.55:8090\r\nUser-Agent: Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.2.3) Gecko/20100401 Firefox/3.6.3\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\nAccept-Language: en-us,en;q=0.5\r\nAccept-Encoding: gzip,deflate\r\nAccept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7\r\nReferer: http://10.100.56.55:8090/corporate/webpages/httpclientlogin.jsp \r\nContent-Type: application/x-www-form-urlencoded\r\nContent-Length: ";
                if (loginStat == "Login")
                {
                    y = "mode=191&isAccessDenied=null&url=null&message=&username=" + id + "&password=" + password.Text + "&saveinfo=saveinfo&login=" + loginStat;
                }
                else
                {
                    y = "mode=193&isAccessDenied=null&url=null&message=&username=" + id + "&password=" + password.Text + "&saveinfo=saveinfo&login=" + loginStat;
                }
                x = x + y.Length.ToString() + "\r\n\r\n" + y;
                Socket cyber = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse("10.100.56.55");
                try
                {
                    cyber.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    cyber.Connect(ip, 8090);
                    cyber.Send(Encoding.ASCII.GetBytes(x), 0, x.Length, SocketFlags.None);
                }
                catch { cyber.Close(); errLabel.Text = "Network Error"; }
                if (cyber.Connected == true)
                {
                    bool error = false;
                    try
                    {
                        cyber.Receive(cybreply);
                    }
                    catch { cyber.Close(); errLabel.Text = "Network Error"; error = true; }
                    if (error == false)
                    {
                        string rply = Encoding.ASCII.GetString(cybreply).ToString();
                        if (rply.Contains("You+have+successfully+logged+in"))
                        {
                            loginStat = "Logout";
                            errLabel.Text = null;
                            userName.Enabled = false;
                            password.Enabled = false;
                            rememberChk.Enabled = false;
                            usrLoginBtn.Text = "Logout";
                            sharingBtn.Enabled = true;
                            if (automaticSharing == true)
                            { sharingBtn_Click(sender, e); }
                        }
                        else if (rply.Contains("You+have+successfully+logged+off"))
                        {
                            loginStat = "Login";
                            errLabel.Text = null;
                            userName.Enabled = true;
                            password.Enabled = true;
                            rememberChk.Enabled = true;
                            sharingBtn.Text = "Start Sharing";
                            sshare.Close();
                            sharingBtn.Enabled = false;
                            usrLoginBtn.Text = "Login";
                            if (rememberChk.Checked == false)
                            {
                                password.Text = null;
                            }
                        }
                        else
                        {
                            errLabel.Text = "Invalid Username or Password";
                        }
                    }
                }
                if (cyber.Connected == true)
                {
                    cyber.Shutdown(SocketShutdown.Both);
                    cyber.Close();
                    cyber.Dispose();
                }
            }
        }
        private void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
       //     shareUsed += shareDownload;
         //   this.downloadSpeed.Text = ((double)(localDownload + shareDownload) / 1024).ToString();
           localDownload = 0;
            shareDownload = 0;
        //    shrUsed.Value = (int)((double)(shareUsed / maxShareLimit) * 0.1);
            if (acceptFlag == true && otherclients <= maxslots)
            {
                sshare.BeginAccept(new AsyncCallback(OnAccept), sshare);
                acceptFlag = false;
            }
        }
        private void UDPHandler()
        {
            try
            {
                byte[] buff;
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ip;
                int j = 0;
                for (j = 0; j < host.AddressList.Length; j++)
                {
                    if (host.AddressList[j].AddressFamily.ToString() == "InterNetwork")
                    {
                        break;
                    }
                }
                ip = host.AddressList[j];
                string ipaddr = ip.ToString();
                dataTable.Columns.Add("IPAddress", typeof(string));
                DataColumn[] clmn = new DataColumn[1];
                clmn[0] = dataTable.Columns[0];
                dataTable.PrimaryKey = clmn;
                dataTable.Columns.Add("ShareLeft", typeof(string));
                dataTable.Columns.Add("SlotsAvailable", typeof(string));
                while (true)
                {
                    IPEndPoint remote = null;
                    buff = uclient.Receive(ref remote);
                    string msg = Encoding.ASCII.GetString(buff, 0, buff.Length);
                    if (msg.Contains("GET"))
                    {
                        //send details
                        Byte[] rmsg = Encoding.ASCII.GetBytes("ADD" + " " + ipaddr + " " + maxShareLimit + " " + (maxslots - otherclients));
                        remote.Port = 4001;
                        uclient.Send(rmsg, rmsg.Length, remote);
                    }
                    else
                        if (msg.Contains("OK?"))
                        {
                            //send proceed signal
                            Byte[] rmsg = Encoding.ASCII.GetBytes("YES");
                            uclient.Send(rmsg, rmsg.Length, remote);
                        }
                        else
                            if (msg.Contains("ADD"))
                            {
                                //Add entry
                                string[] tokens = msg.Split(' ');
                                DataRow found = dataTable.Rows.Find(tokens[1]);

                                if (found == null && tokens[1]!=ipaddr)
                                {
                                    dataTable.Rows.Add(tokens[1], tokens[2], tokens[3]);
                                    if (totalShare.Text == "")
                                    {
                                        totalShare.Text = (int.Parse(tokens[2])).ToString() + " MB";
                                    }
                                    else
                                    {
                                        totalShare.Text = (int.Parse(totalShare.Text) + int.Parse(tokens[2])).ToString() + " MB";
                                    }
                                }
                            }
                }

            }
            catch { if (uclient != null) { uclient.Close(); } }
        }     
        private void useIP_SelectedIndexChanged(object sender, EventArgs e)
        {
                totalShare.Text = "";
                dataTable.Clear();
                UdpClient udp = new UdpClient();
               
                string ipinitial = useIP.Text.Substring(0, 10);
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 4001);
                for (int i = 0; i < 255; i++)
                {
                    ipep.Address = IPAddress.Parse(ipinitial + i);
                    udp.Send(Encoding.ASCII.GetBytes("GET"), 3, ipep);
                }
                udp.Close();
              
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                uclient.Close();
                slocal.Close();
                slocal.Dispose();
                uclient = null;
                slocal = null;
                if (sshare != null)
                    sshare.Close();
            }
            catch { }
        }
        private void use_Click(object sender, EventArgs e)
        {
            useFlag = !useFlag;
            if (useFlag)
            {
                use.Text = "Stop";
            }
            else
            {
                use.Text="Use";
            }
        }
    }
}
