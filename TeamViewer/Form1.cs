using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TeamViewer
{
    public partial class Form1 : Form
    {
        static IPAddress dest;
        string localIP = "192.168.1.4";
        static IPEndPoint endPoint;
        static Socket soc;
        static Socket multicastSoc;
        bool server = false;
        static Socket udpGetFiles;
        static Socket udpSocket;
        static Socket _socket;
        List<Socket> clientsList;


        public Form1()
        {
            InitializeComponent();
        }


        private byte[] PrintScreen()
        {

            Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            Graphics graphics = Graphics.FromImage(printscreen as Image);

            graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            using (var ms = new MemoryStream()) {

                printscreen.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
            
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            server = true;

            RunClientServerChat();

            clientsList = new List<Socket>();
            _socket.Bind(new IPEndPoint(IPAddress.Any, 8888));
           
            _socket.Listen(10);


            new Task(action:() =>
            {
                while (true)
                {
                    Socket s = _socket.Accept();
                    clientsList.Add(s);
                }
            }).Start();
            new Task(action: () =>
            {
                while (true)
                {

                    for (int i = 0; i < clientsList.Count; i++)
                    {
                        try
                        {
                            if (clientsList[i].Connected)
                            {
                                byte[] buffer = PrintScreen();
                                clientsList[i].Send(buffer);
                            }
                            else
                                clientsList.Remove(clientsList[i]);

                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }).Start();
            
            
        }
        private Image byteArrayToImage(byte[] byteArrayIn)
        {
            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
            Image img = (Image)converter.ConvertFrom(byteArrayIn);

            return img;

        }

        private  void button2_Click(object sender, EventArgs e)
        {
            RunClientServerChat();

            var _ip = IPAddress.Parse(localIP);

            _socket.Connect(_ip, 8888);
            

            new Task(action:()=> {
                while (true)
                {
                    byte[] buffGet = new byte[200000];
                    int n = _socket.Receive(buffGet);
                    pictureBox1.BeginInvoke(new InvokeDelegate(delegateMethod), args: buffGet);
                }
            }).Start();
        }
     
        public delegate void InvokeDelegate(byte[]byteGet);

        private void delegateMethod(byte[] byteGet)
        {
            pictureBox1.Image = byteArrayToImage(byteGet);
        }



   

        private void RunClientServerChat()
        {
            IPAddress ipAddr = IPAddress.Parse("192.168.255.255");
            try
            {
                soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


                IPEndPoint local = new IPEndPoint(IPAddress.Parse(localIP), 3333);


                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 4567);

                soc.Bind(ipep);

                IPAddress ip = IPAddress.Parse("224.5.5.5");

                dest = IPAddress.Parse("224.5.5.5");

                soc.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    new MulticastOption(ip, IPAddress.Any));

                soc.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);


                string str = string.Empty;

                new Task( () =>
                {
                    while (true)
                    {
                        byte[] buff = new byte[1024];
                        
                        int n = soc.Receive(buff);
                                              
                        str = Encoding.Default.GetString(buff, 0, n);
                        richTextBoxOut.BeginInvoke(new ChatText(delegateMethodForChat), str);
                     
                    }
                }).Start();

               /// soc.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();

            }
        }


        public delegate void ChatText(string str);



        private void delegateMethodForChat(string str)
        {
            richTextBoxOut.AppendText(str + Environment.NewLine);
            maskedTextBox1.Text = "";
        }


        private void maskedTextBox1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Enter)
            {
                
                byte[] buff = ASCIIEncoding.ASCII.GetBytes(maskedTextBox1.Text);

                Socket s = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);

                IPAddress ip = IPAddress.Parse("224.5.5.5");

                s.SetSocketOption(SocketOptionLevel.IP,
                        SocketOptionName.AddMembership, new MulticastOption(ip));

                s.SetSocketOption(SocketOptionLevel.IP,
                        SocketOptionName.MulticastTimeToLive, 2);

                IPEndPoint ipep = new IPEndPoint(ip, 4567);
                s.Connect(ipep);

                s.Send(buff, buff.Length, SocketFlags.None);

                s.Close();

                maskedTextBox1.BeginInvoke(new CleanDelegate(CleanMaskedTB));

            }
        }
        private delegate void CleanDelegate();

        private void CleanMaskedTB()
        {
            maskedTextBox1.Text = "";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0112)
            {
               
                if (m.WParam == new IntPtr(0xF030)) 
                {
                    this.button1.Visible = false;
                    this.button2.Visible = false;
                    this.richTextBoxOut.Visible = false;
                    this.maskedTextBox1.Visible = false;
                   
                    pictureBox1.Width = Screen.PrimaryScreen.Bounds.Width;
                    pictureBox1.Height = Screen.PrimaryScreen.Bounds.Height;

                    //window is being maximized
                }
                else
                {
                    pictureBox1.Width = 615;
                    pictureBox1.Height =524;
                    this.button1.Visible = true;
                    this.button2.Visible = true;
                    this.richTextBoxOut.Visible = true;
                    this.maskedTextBox1.Visible = true;
                }
            }
            base.WndProc(ref m);
        }

    }
}
