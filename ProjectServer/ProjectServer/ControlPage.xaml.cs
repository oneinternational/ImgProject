using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ProjectServer
{
    /// <summary>
    /// Page2.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class ControlPage : Page
    {
        private string bindIP = "10.10.20.106";
        private const int bindPort = 33224;
        private TcpListener server = null;
        private IPEndPoint localAddress;

      

        //접속자 스트림
        List<NetworkStream> streamList = new List<NetworkStream>();
        //라인+스트림
        Dictionary<string, NetworkStream> lineNameAndStream = new Dictionary<string, NetworkStream>();

        //동기화
        readonly object firstLock = new object();
        bool secondLock = false;

        //접속자 수
        private int clientCount = 0;

        //pass ListView
        List<PassData> passItems = new List<PassData>();

        public class PassData
        {
            public string Item { get; set; }
            public string Time { get; set; }
            public string NumberOfLine { get; set; }
            public string NameOfDirector { get; set; }
            public string Note { get; set; }
        }

        private void HandleDataFromClients(object stm9)
        {
            NetworkStream stm1 = (NetworkStream)stm9;
            int length;
            string dataMessage = null;
            byte[] messageBuffer = new byte[256];
            
            while((length = stm1.Read(messageBuffer, 0, messageBuffer.Length))!=0)
            {
                dataMessage = Encoding.Default.GetString(messageBuffer, 0, length);
                Console.WriteLine("받은 메시지 : {0}", dataMessage);
                string[] messageSplit = dataMessage.Split(new string[] { "/" }, StringSplitOptions.None);

                int caseNumber = int.Parse(messageSplit[0]);

                switch(caseNumber)
                {
                case 2:
                        passItems.Add(new PassData() { Item = messageSplit[1], Time = messageSplit[2], NumberOfLine = messageSplit[3], NameOfDirector = messageSplit[4], Note = messageSplit[5] });
                        PassList.ItemsSource = passItems;
                    break;

                    
                }
            }
 
                
        }

        private void HandleEntranceOfClients()
        {
            while (true)
            {
                MessageBox.Show("진짜");
                TcpClient client = server.AcceptTcpClient();
                MessageBox.Show("들어왔다.");
                lock (firstLock)
                {
                    while (clientCount > 0 || secondLock == true)// init에는 client 0 왼쪽 조건 x false 실행 x secondLock = 0 false 실행 x
                        Monitor.Wait(firstLock);

                    secondLock = true;

                    Console.WriteLine("클라이언트 접속 : {0}", ((IPEndPoint)client.Client.RemoteEndPoint).ToString());
                    clientCount += 1;
                    NetworkStream stream = client.GetStream();
                    streamList.Add(stream);

                    int length;
                    string entryMessage = null;
                    byte[] messageBuffer = new byte[256];
                    //입장 메시지 못받았을 때 return 0
                    if ((length = stream.Read(messageBuffer, 0, messageBuffer.Length)) == 0)
                    {
                        Console.WriteLine("Can't receive entry message from a client");
                    }
                    //받았을 때 
                    entryMessage = Encoding.Default.GetString(messageBuffer, 0, length);
                    Console.WriteLine("수신 {0}", entryMessage);

                    string[] splitMessage = entryMessage.Split(new string[] { "/" }, StringSplitOptions.None);
                    //1/1번라인 이런식으로 메시지가 온다
                    int.TryParse(splitMessage[0], out int protocol);

                    if (protocol == 1)
                    {
                        Console.WriteLine("Protocol do match");
                        lineNameAndStream[splitMessage[1]] = stream;
                    }
                    else
                        Console.WriteLine("Protocol does not match");

                    secondLock = false;

                    Monitor.Pulse(firstLock);
                }
                break;
            }
            


        }
        

        private void ServerInit()
        {
            try
            {
                localAddress =
                    new IPEndPoint(IPAddress.Parse(bindIP), bindPort);

                server = new TcpListener(localAddress);

                server.Start();

                Console.WriteLine("Information : Woong Server has been just opened");
                MessageBox.Show("들어왔다.");
            }
            catch (SocketException a)
            {
                Console.WriteLine(a);
                server.Stop();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }

    public partial class ControlPage : Page
    {
        
        public ControlPage()
        {
            InitializeComponent();
        }


        private void Btn_Sopen(object sender, RoutedEventArgs e)
        {
            ServerInit();
            MessageBox.Show("가보자");
            Thread t1 = new Thread(new ThreadStart(HandleEntranceOfClients));
            Thread t2 = new Thread(new ParameterizedThreadStart(HandleDataFromClients));
            t1.Start();
            t2.Start();

        }

        //private void aaaaa()
        //{
        //    HandleDataFromClients(streamList[0]);
        //}


        private void Btn_Sclose(object sender, RoutedEventArgs e)
        {
            server.Stop();
        }
    }
}
