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
using System.ComponentModel;
using System.IO;


namespace ProjectServer
{
    /// <summary>
    /// Page2.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class ControlPage : Page
    {
        //private string bindIP = "192.168.35.105";//집
        private string bindIP = "10.10.20.106";
        private const int bindPort = 9191;
        private TcpListener server = null;
        private IPEndPoint localAddress;

        private int cnt = 0;
        //접속자 스트림
        List<NetworkStream> streamList = new List<NetworkStream>();
        //라인+스트림
        Dictionary<string, NetworkStream> lineNameAndStream = new Dictionary<string, NetworkStream>();
        //클라이언트 강제 종료시 Thread 처리를 위한 Dictionary
        Dictionary<NetworkStream, Thread> streamAndThread = new Dictionary<NetworkStream, Thread>();

        //동기화
        readonly object firstLock = new object();
        bool secondLock = false, secondLock2 = false;

        //접속자 수
        private int clientCount = 0;

        //pass ListView
        List<PassData> passItems = new List<PassData>();

        public class PassData
        {
            public string Item { get; set; }
            public string Time { get; set; }
            public string Line { get; set; }
            public string Director { get; set; }
            public string Note { get; set; }
        }

        private void HandleDataFromClients(object individualClient)
        {
            int length;
            string dataMessage = null;
            byte[] messageBuffer = new byte[256];
            TcpClient client = (TcpClient)individualClient;
            NetworkStream stream = client.GetStream();

            while (true)//클라 접속 끊겼을 때 예외처리 하기
            {
                try
                {
                    length = stream.Read(messageBuffer, 0, messageBuffer.Length);

                    dataMessage = Encoding.Default.GetString(messageBuffer, 0, length);
                    MessageBox.Show("받은 메시지 : " + dataMessage);
                    string[] messageSplit = new string[1024];
                    System.Array.Clear(messageSplit, 0, messageSplit.Length);
                    messageSplit = dataMessage.Split(new string[] { "/" }, StringSplitOptions.None);

                    FileStream fileStr;
                    byte[] size = new byte[4];
                    int fileLength;
                    int totalLength = 0;


                    int caseNumber = int.Parse(messageSplit[0]);

                    switch (caseNumber)
                    {
                        case 1:
                            MessageBox.Show("Entry protocol does match");
                            MessageBox.Show("입장한 생산라인 클라이언트 : " + messageSplit[1]);
                            IPEndPoint ip_point = (IPEndPoint)client.Client.RemoteEndPoint;
                            string ip = ip_point.Address.ToString();
                            this.Dispatcher.Invoke(() =>
                            {
                                LineState.AppendText(messageSplit[1] + "\n");
                                IPState.AppendText(ip + "\n");
                            });

                            //lock (firstLock)
                            //{
                            //    while (secondLock2 == true)
                            //        Monitor.Wait(firstLock);

                            //    lineNameAndStream
                            //    secondLock2 = true;
                            //    secondLock2 = false;
                            //    Monitor.Pulse(firstLock);
                            //}

                            lineNameAndStream[messageSplit[1]] = stream;
                            break;

                        case 2:
                            MessageBox.Show("여기냥");
                            //passItems.Insert(passItems.Count, new PassData() { Item = messageSplit[1], Time = messageSplit[2], Line = messageSplit[3], Director = messageSplit[4], Note = messageSplit[5] });
                            passItems.Add(new PassData() { Item = messageSplit[1], Time = messageSplit[2], Line = messageSplit[3], Director = messageSplit[4], Note = messageSplit[5] });
                            this.Dispatcher.Invoke(() =>
                            {
                                //PassList.ItemsSource = passItems;
                            });
                            break;


                        case 4:
                            //-------------------------------------------------

                            stream.Read(size, 0, size.Length);
                            fileLength = BitConverter.ToInt32(size, 0);
                            MessageBox.Show("데이터 수신:" + fileLength.ToString());

                            fileStr = new FileStream("../../acx" + cnt.ToString() + ".png", FileMode.Create, FileAccess.Write);
                            int byteread;//리드한 파일 크기 담을거

                            BinaryWriter write = new BinaryWriter(fileStr);

                            byte[] buffer = new byte[fileLength];

                            byteread = stream.Read(buffer, 0, buffer.Length);
                            write.Write(buffer, 0, byteread);
                            MessageBox.Show("이미지 파일 수신 완료");
                            //fileStr.Close();
                            //----------------------------------------------------
                            cnt += 1;
                            break;
                    }
                    Array.Clear(messageBuffer, 0, messageBuffer.Length);
                }
                catch(Exception a)
                {
                    ThreadCloser(streamAndThread[stream]);
                }                 
            }
        }

        private void HandleEntranceOfClients()
        {
            string message = "1/";
            byte[] sendMessage = new byte[256];
            while (true)
            {
                MessageBox.Show("Waiting for clients");
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = null;
                lock (firstLock)
                {
                    while (secondLock == true)// init에는 client 0 왼쪽 조건 x false 실행 x secondLock = 0 false 실행 x
                        Monitor.Wait(firstLock);

                    secondLock = true;

                    MessageBox.Show("클라이언트 접속 : " + ((IPEndPoint)client.Client.RemoteEndPoint).ToString());
                    clientCount += 1;
                    stream = client.GetStream();
                    

                    sendMessage = System.Text.Encoding.Default.GetBytes(message);
                    stream.Write(sendMessage, 0, sendMessage.Length);
                    //stream list에 접속자 stream 넣기
                    streamList.Add(stream);

                    secondLock = false;

                    Monitor.Pulse(firstLock);
                }
                Thread t2 = new Thread(new ParameterizedThreadStart(HandleDataFromClients));
                t2.Start(client);
                streamAndThread[stream] = t2;
            }
        }
        
        private void ThreadCloser(object individualThread)
        {
            try
            {
                Thread closeThread = (Thread)individualThread;
                closeThread.Abort();
                NetworkStream closeStream = streamAndThread.FirstOrDefault(x => x.Value == closeThread).Key;
                MessageBox.Show("성공적으로 개별 스레드 종료됨 빠져나간 라인 번호 : " + lineNameAndStream.FirstOrDefault(x => x.Value == closeStream).Key);
            }
            catch(Exception a)
            {
                MessageBox.Show("오류 : " + a.ToString());
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

                MessageBox.Show("Woong server has just been connected");
            }
            catch (SocketException a)
            {
                MessageBox.Show("{0}", a.ToString());
                server.Stop();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_EMC(object sender, RoutedEventArgs e)
        {

        }

        private int i = 0;

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            passItems.Add(new PassData() { Item = "asasd", Time = "dsadsa", Line = "dsadwq", Director = "gdasfe", Note = "gdfsa" });
            //PassList.Items.Insert(i, passItems);
            i += 1;
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
            MessageBox.Show("Thread starts");
            Thread t1 = new Thread(new ThreadStart(HandleEntranceOfClients));
            t1.Start();
        }


        private void Btn_Sclose(object sender, RoutedEventArgs e)
        {
            server.Stop();
        }
    }



}


