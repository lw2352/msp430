using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
using System.IO;

namespace PipeServer2016
{
    //The commands for interaction between the server and the client
    enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }

    public partial class SGSserverForm : Form
    {
        private delegate void UpdateStatusDelegate(string status);
        private string m_allreceivedstring = "";
        private string m_ipstring = "192.168.1.168";//这个是服务端的IP地址
        //private string m_ipstring = "120.25.229.254";//这个是服务端的IP地址
        private int m_intPort = 8080; //8080是服务器端口地址
        //private byte[] byteData = new byte[g_datafulllength]; //临时接收最大的缓冲区，单次接收放在这里。无论它来自哪个IP
        //private byte[] byteData = new byte[2048]; //临时接收最大的缓冲区，单次接收放在这里。无论它来自哪个IP,最大目前限制为2048
        private byte[] byteData = new byte[1006]; //临时接收最大的缓冲区，单次接收放在这里。无论它来自哪个IP,最大目前限制为2048
        private byte[] byteSendDataCommond = new byte[12]; //发送传输数据命令的指令
        //private byte[] byteAllData = new byte[600002]; //所有数据，算一个完整的数据。
        ArrayList receivedataList;
        Hashtable htClient = new Hashtable(); //创建一个Hashtable实例，存储是IP地址，value是byte数组
        Hashtable htClientOnline = new Hashtable(); //创建一个Hashtable实例，存储是IP地址，value是“1”，在这个列表里面，说明它是在线的
        public static int g_datafulllength = 600000; //完整数据包的一个长度
        //public static int g_datafulllength = 6144;

        public static int g_totalPackageCount = 600; //总数据包大小，目前是60万除以2000，是300个包
        //public static int g_totalPackageCount = 10; //测试10个包
        public static int g_perPackageSize = 1000; //每个数据包的大小，目前默认是2000
        //public static bool g_isADSwitchOn = false;  //默认采集AD是关闭的。
        //public static bool g_isADUploadDataSwitchOn = false;  //默认上传的采集AD是关闭的。
        //The ClientInfo structure holds the required information about every
        //client connected to the server
        public struct ClientInfo
        {
            public Socket socket;   //Socket of the client
            public string strName;  //Name by which the user logged into the chat room
        }

        //The collection of all clients logged into the room (an array of type ClientInfo)
        ArrayList clientList;

        //The main socket on which the server listens to the clients
        Socket serverSocket;

        

        private void UpdateStatus(string status)
        {
            this.txtLog.Text = status;
        }

        public SGSserverForm()
        {
            clientList = new ArrayList();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {            
            try
            {
                //g_isADSwitchOn = false;
                //g_isADUploadDataSwitchOn = false;
                //A5 A5 24 01 04 FF FF FF FF FF 5A 5A
                //初始化发送命令，随时等待告诉客户端去发送
                byteSendDataCommond[0]=0xA5;
                byteSendDataCommond[1] = 0xA5;
                byteSendDataCommond[2] = 0x24;
                byteSendDataCommond[3] = 0x01;
                byteSendDataCommond[4] = 0x04;
                byteSendDataCommond[5] = 0xFF;
                byteSendDataCommond[6] = 0xFF;
                byteSendDataCommond[7] = 0xFF;
                byteSendDataCommond[8] = 0xFF;
                byteSendDataCommond[9] = 0xFF;
                byteSendDataCommond[10] = 0x5A;
                byteSendDataCommond[11] = 0x5A;

                //We are using TCP sockets
                serverSocket = new Socket(AddressFamily.InterNetwork, 
                                          SocketType.Stream, 
                                          ProtocolType.Tcp);

                //Assign the any IP of the machine and listen on port number 1000
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(m_ipstring), m_intPort);

                //Bind and listen on the given address
                serverSocket.Bind(ipEndPoint);
                serverSocket.Listen(8080);

                //Accept the incoming clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSserverTCP",                     
                    MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }            
        }

        /// <summary> 
        /// 字节数组转16进制字符串 
        /// </summary> 
        /// <param name="bytes"></param> 
        /// <returns></returns> 
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (long i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        } 

/**  
    * 将int数值转换为占四个字节的byte数组，本方法适用于(低位在前，高位在后)的顺序。 和bytesToInt（）配套使用 
    * @param value  
    *            要转换的int值 
    * @return byte数组 
    */ 
        public static byte[] intToBytes(int value)
        {
            byte[] src = new byte[3];
            src[2] = (byte)((value >> 16) & 0xFF);
            src[1] = (byte)((value >> 8) & 0xFF);
            src[0] = (byte)(value & 0xFF);
            return src;  
        } 
   

        //接收来自客户端的请求
        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = serverSocket.EndAccept(ar);

                //Start listening for more clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from her
                //开始从连接的socket异步接收数据
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, 
                    new AsyncCallback(OnReceive), clientSocket);                
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSserverTCP", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

       //检查是否是心跳包，主要检查命令码即可
        private bool checkIsHeartPackage()
        {    
                if (byteData[0] == 0xA5 && byteData[1] == 0xA5 && byteData[2] == 0xFF && byteData[3] == 0x00 && byteData[4] == 0x04
                    && byteData[9] == 0xFF && byteData[10] == 0x5A && byteData[11] == 0x5A)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //是否采集完成
        private bool checkIsADFinished()
        {
            if (byteData[0] == 0xA5 && byteData[1] == 0xA5 && byteData[2] == 0x22 
                && byteData[9] == 0x55 && byteData[10] == 0xFF && byteData[11] == 0x5A && byteData[12] == 0x5A)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //是否设定采集时间完成
        private bool checkIsSetADSuccess()
        {
            if (byteData[0] == 0xA5 && byteData[1] == 0xA5 && byteData[2] == 0x25
                && byteData[9] == 0x55 && byteData[10] == 0xFF && byteData[11] == 0x5A && byteData[12] == 0x5A)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //是否完全是采集数据，而不是命令码或者应答或者心跳包之类的。
        private bool checkIsPureData(int bytesRead)
        {
            //if (byteData[0] == 0xA5 && byteData[1] == 0xA5  && byteData[bytesRead-2] == 0x5A && byteData[bytesRead - 1] == 0x5A)
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            if (byteData[0] == 0xAA  && byteData[bytesRead - 1] == 0x55 && bytesRead==g_perPackageSize+6)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //接收数据
        /*
         * 算法：1、如果收到一段报文，则首先判断这个IP在内存中有没有，没有则添加到hashtable中进行处理。
         */
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = (Socket)ar.AsyncState;
                //此处获取数据大小

                //获取客户端信息，包括了IP地址、端口
                IPAddress clientIPAddress = (clientSocket.RemoteEndPoint as IPEndPoint).Address;
                string clientIPAddressPort = (clientSocket.RemoteEndPoint as IPEndPoint).Port.ToString();
                string strIP = clientIPAddress.ToString();

                //获取接收的数据长度,注意此处的停止接收，后面必须继续接收，否则不会接收数据的。
                int bytesRead = clientSocket.EndReceive(ar);

                //ClientInfo clientInfospec = new ClientInfo();
                //clientInfospec.socket = clientSocket;

                string strrec = byteToHexStr(byteData);
                Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + DateTime.Now.ToLongTimeString() + "从硬件接收到的数据长度是" + bytesRead.ToString() + "\n"); 
                Console.WriteLine(strIP +":"+ clientIPAddressPort+"---"+DateTime.Now.ToLongTimeString() + "从硬件接收到的数据是" + strrec + "\n"); //把接收的数据打印出来
                //Console.WriteLine("从硬件接收到的数据时间是：" +  DateTime.Now.ToLongTimeString() + "\n"); //把接收的数据打印出来

                if (!htClient.Contains(strIP)) //判断哈希表是否包含特定键,其返回值为true或false
                {
                    if (checkIsHeartPackage())  //检查一下是否是心跳包
                    {    
                        DataItem dataitem = new DataItem();
                        dataitem.currentsendbulk=0;
                        dataitem.isADSwitchOn=false;
                        dataitem.isSetADTimeOn = false;
                        dataitem.isReadADTimeOn = false;
                        dataitem.isSendDataToServer=false;
                        dataitem.totalsendbulk=g_totalPackageCount;
                        dataitem.datalength = 0;
                        dataitem.socket = clientSocket;
                        dataitem.byteDeviceID = new byte[4]; //发送传输数据命令的指令
                        dataitem.byteAllData = new byte[g_datafulllength]; //所有数据，算一个完整的数据。
                        dataitem.byteDeviceID[0] = byteData[8];
                        dataitem.byteDeviceID[1] = byteData[7];
                        dataitem.byteDeviceID[2] = byteData[6];
                        dataitem.byteDeviceID[3] = byteData[5];

                        //设备的ID字符串
                        byte[] macid = new byte[4];
                        macid[0] = byteData[8];
                        macid[1] = byteData[7];
                        macid[2] = byteData[6];
                        macid[3] = 0x00;
                        dataitem.deviceMac = System.BitConverter.ToInt32(macid, 0).ToString();

                        htClient.Add(strIP, dataitem);
                        this.BeginInvoke(new MethodInvoker(delegate()
                        {
                            DeviceCheckedListBox1.Items.Add(strIP);
                        }));
                    }
                }
                else
                {
                    DataItem dataitem = (DataItem)htClient[strIP];
                    if (checkIsHeartPackage())  //检查一下是否是心跳包
                    {
                        if (dataitem.isADSwitchOn)
                        {
                            byte[] byteDeviceData = new byte[1]; //发送传输数据命令的指令
                            //0x22是AD数据采样的命令码
                            clientSocket.BeginSend(setupCommandData(dataitem.byteDeviceID, 0x22, 0, byteDeviceData, true), 0, 12, SocketFlags.None,
                                                   new AsyncCallback(OnSend), clientSocket);
                        }
                        if (dataitem.isSetADTimeOn)
                        {
                            byte[] byteDeviceData = new byte[4]; //发送设定采样时间的命令
                            byte byteHour = (byte)dataitem.readytosethour;
                            byteDeviceData[0] = byteHour;
                            //byteDeviceData[0] = 0x11;

                            byte byteMinute = (byte)dataitem.readytosetminute;
                            byteDeviceData[1] = byteMinute;
                            //byteDeviceData[1] = 0x02;
                            byteDeviceData[2] = 0x00;
                            byteDeviceData[3] = 0x00;
                            //0x22是AD数据采样的命令码
                            clientSocket.BeginSend(setupCommandData(dataitem.byteDeviceID, 0x25, 4, byteDeviceData, true), 0, 16, SocketFlags.None,
                                                   new AsyncCallback(OnSend), clientSocket);
                        }
                        if (dataitem.isReadADTimeOn)
                        {
                            byte[] byteDeviceData = new byte[4]; //发送设定采样时间的命令
                            byteDeviceData[0] = 0xFF;
                            byteDeviceData[1] = 0xFF;
                            byteDeviceData[2] = 0xFF;
                            byteDeviceData[3] = 0xFF;
                            //0x22是AD数据采样的命令码
                            clientSocket.BeginSend(setupCommandData(dataitem.byteDeviceID, 0x25, 4, byteDeviceData, false), 0, 16, SocketFlags.None,
                                                   new AsyncCallback(OnSend), clientSocket);
                        }
                        if (dataitem.isSendDataToServer)
                        {
                            Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + "开始进行把采集数据进行传输到服务端");//此处输出a
                            htClient[strIP] = dataitem;

                            if (dataitem.currentsendbulk == g_totalPackageCount)
                            {
                                //g_isADUploadDataSwitchOn = false; //数据以及全部上传完毕了！！
                                dataitem.isSendDataToServer = false;
                                dataitem.currentsendbulk = 0;
                                //addData(dataitem.deviceMac, "2017/9/3 11:11:11", dataitem.byteAllData);
                                Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + "300个包已经全部上传完毕！！！");//此处输出a
                            }
                            else
                            {
                                //dataitem.isSendDataToServer = true;
                                dataitem.byteCurrentPackageData = new byte[g_perPackageSize + 6];
                                dataitem.currentReceivePackageLength = 0;
                                Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + "开始发送第" + dataitem.currentsendbulk.ToString() + "包的命令");
                                clientSocket.BeginSend(setupReadADData(dataitem.currentsendbulk, dataitem.byteDeviceID), 0, 18, SocketFlags.None,
                                                        new AsyncCallback(OnSend), clientSocket);
                                dataitem.currentsendbulk = dataitem.currentsendbulk + 1;
                            }



                        }
                    }
                    else if (byteData[2]==0x25)
                    {
                        if (dataitem.isSetADTimeOn)
                        {
                            dataitem.isSetADTimeOn = false; //设定成功了，就关闭。
                            try
                            {
                                this.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    lblSetMessage.Text = "设定成功了！";
                                    lblSetMessage.Visible = true;
                                }));
                            }
                            catch (Exception ee)
                            {
                                this.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    lblSetMessage.Text = "设定失败了！";
                                    lblSetMessage.Visible = false;
                                }));
                            }

                        }
                        if (dataitem.isReadADTimeOn)
                        {
                            dataitem.isReadADTimeOn = false; //读取成功了，就关闭。
                            byte bytehour = byteData[9];
                            byte byteminute = byteData[10];
                            string stringhour = Convert.ToInt32(byteData[9]).ToString();
                            string stringminute = Convert.ToInt32(byteData[10]).ToString();
                            try
                            {
                                this.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    lblTime.Text = "当前设备的采样时间是" + stringhour + ":" + stringminute;
                                }));
                            }
                            catch (Exception ee)
                            {
                                this.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    lblTime.Text = "读取失败了！";
                                }));
                            }
                        }
                    }
                    else if (checkIsADFinished())
                    {
                        dataitem.isADSwitchOn = false; //采集完毕了，就把开关关掉，否则没完没了。
                    }
                    else if (dataitem.isSendDataToServer) //说明全部是数据来的，需要存储
                    {
                        if (bytesRead<g_perPackageSize+6)
                        {
                            
                                     for (int i = 0; i < bytesRead; i++)
                                    {

                                        dataitem.byteCurrentPackageData[i + dataitem.currentReceivePackageLength] = byteData[i];
                                    }
                                     dataitem.currentReceivePackageLength += bytesRead;
                                     if (dataitem.currentReceivePackageLength==g_perPackageSize+6)
                                     {
                             string fileName = "testreceivedata.txt";
                            string currentbulk = dataitem.currentsendbulk.ToString();
                                         /*
                            //***************************************************************************
                                         
                            using (FileStream fs = new FileStream(fileName, FileMode.Append))
                            {

                                using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                                {
                                    writer.Write("开始写第:" + currentbulk + "个数据包\n");
                                    //writer.Write("开始写数据于:" + DateTime.Now.ToLongTimeString() + "\n");
                                    
                                    Console.WriteLine("开始进行把采集数据进行存储");//此处输出a
                                    for (int i = 1; i < g_perPackageSize+1; i++)
                                    {
                                        byte[] test = new byte[1];
                                        test[0] = dataitem.byteCurrentPackageData[i];
                                        string strtest = byteToHexStr(test);
                                        writer.Write(strtest);
                                        dataitem.byteAllData[dataitem.datalength + i - 1] = dataitem.byteCurrentPackageData[i];
                                        //writer.Write(bytesMax[i]);
                                        writer.Write(" ");
                                    }
                                    writer.Write("\n");
                                    

                                } //using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                            }  //using (FileStream fs = new FileStream(fileName, FileMode.Append))
                                   
                                         */
                            dataitem.datalength = dataitem.datalength + bytesRead - 2; //去掉两个封装的字节
                            if (dataitem.currentsendbulk >= g_totalPackageCount)
                            {
                                //g_isADUploadDataSwitchOn = false; //数据以及全部上传完毕了！！
                                dataitem.currentsendbulk = 0;
                                dataitem.isSendDataToServer = false;
                                //addData(dataitem.deviceMac, "2017/8/24 11:33:30", dataitem.byteAllData);
                            }

                            //****************************************************************************************************
                                     }
                        }
                        else if (bytesRead==g_perPackageSize+6)
                        {
                            string fileName = "testreceivedata.txt";
                            string currentbulk = dataitem.currentsendbulk.ToString();
                            /*
                            //***************************************************************************
                            using (FileStream fs = new FileStream(fileName, FileMode.Append))
                            {

                                using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                                {
                                    writer.Write("开始写第:" + currentbulk + "个数据包\n");
                                    //writer.Write("开始写数据于:" + DateTime.Now.ToLongTimeString() + "\n");
                                    
                                    Console.WriteLine("开始进行把采集数据进行存储");//此处输出a
                                    for (int i = 1; i < bytesRead - 1; i++)
                                    {
                                        byte[] test = new byte[1];
                                        test[0] = byteData[i];
                                        string strtest = byteToHexStr(test);
                                        writer.Write(strtest);
                                        dataitem.byteAllData[dataitem.datalength + i - 1] = byteData[i];
                                        //writer.Write(bytesMax[i]);
                                        writer.Write(" ");
                                    }
                                    writer.Write("\n");
                                    

                                } //using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                            }  //using (FileStream fs = new FileStream(fileName, FileMode.Append))
                             
                             * */
                            dataitem.currentReceivePackageLength = dataitem.datalength + bytesRead - 6; //去掉两个封装的字节,4个字节的ID
                            dataitem.byteCurrentPackageData = new byte[g_perPackageSize + 6];

                            for (int i = 5; i < bytesRead-5; i++)
                            {

                                //dataitem.byteCurrentPackageData[i + dataitem.currentReceivePackageLength] = byteData[i];
                                dataitem.byteAllData[dataitem.datalength + i - 5] = byteData[i];
                            }
                            //dataitem.currentReceivePackageLength += bytesRead;

                            if (dataitem.currentsendbulk == g_totalPackageCount-1)//说明数据传输完毕了，2016.9.25标记
                            {
                                //说明：为了测试是否采集数据正确，采用保持为本地文件输出，开始和结尾使用了0x24做标记，保持和以前的文件一样的效果。
                                string outputfileName = dataitem.deviceMac+".dat";
                                using (FileStream fs = new FileStream(outputfileName, FileMode.CreateNew))
                            {

                                using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                                {
                                    byte[] testF = new byte[1];
                                    testF[0] = 0x24;
                                    string strtestF = byteToHexStr(testF);
                                    writer.Write(strtestF);
                                    for (int i = 0; i < dataitem.byteAllData.Length; i++)
                                    {
                                        byte[] test = new byte[1];
                                        test[0] = dataitem.byteAllData[i];
                                        string strtest = byteToHexStr(test);
                                        writer.Write(strtest);

                                    }
                                    writer.Write(strtestF);
                                    

                                } //using (StreamWriter writer = new StreamWriter(fs, Encoding.Unicode))
                            }  //using (FileStream fs = new FileStream(fileName, FileMode.Append))
                             

                                //g_isADUploadDataSwitchOn = false; //数据以及全部上传完毕了！！
                                dataitem.currentsendbulk = 0;
                                dataitem.isSendDataToServer = false;
                                //addData(dataitem.deviceMac, "2017/9/3 11:22:30", dataitem.byteAllData);
                            }

                            //****************************************************************************************************
                        } //else if (bytesRead==g_perPackageSize+2)

                        if (dataitem.isSendDataToServer)
                        {
                            dataitem.currentsendbulk = dataitem.currentsendbulk + 1;
                            Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + "开始发送第" + dataitem.currentsendbulk.ToString() + "包的命令");
                            clientSocket.BeginSend(setupReadADData(dataitem.currentsendbulk, dataitem.byteDeviceID), 0, 18, SocketFlags.None,
                                                    new AsyncCallback(OnSend), clientSocket);
                        }



                    } //else if (dataitem.isSendDataToServer) 

                    //if (checkIsHeartPackage())  //检查一下是否是心跳包
                    //{
                    //}
                }

                //需要持续接收
                Console.WriteLine(strIP + ":" + clientIPAddressPort + "---" + DateTime.Now.ToLongTimeString() + "开始持续BeginReceive" + "\n");
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception ex)
            { 
                //MessageBox.Show(ex.Message, "SGSserverTCP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                Console.WriteLine(ex.Message + "---" + DateTime.Now.ToLongTimeString() + "出错信息：" + "\n");
            }
    }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);                
            }
            catch (Exception ex)
            { 
               // MessageBox.Show(ex.Message, "SGSserverTCP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            //AFileName = this.openFileDialog1.FileName;
            byte[] photoA = GetPhoto("C:\\Users\\Administrator\\Desktop\\威派2016\\测试数据\\11.dat");
            addData("2", "2016/4/24 11:22:30", photoA);


        }


 
        //将外部flash采集数据传输到服务器上,目前协议还有问题，ID应该是4位，目前写成了3位。
        public  byte[] setupReadADData(int bulkCount, byte[] byteDeviceID)
        {
            byte[] bytesbulkCount = new byte[2];
            bytesbulkCount = intToBytes(bulkCount);

            byte[] bytesTotalCount = new byte[2];
            bytesTotalCount = intToBytes(g_totalPackageCount-1);

            byte[] byteSendDataCommond = new byte[18]; //发送传输数据命令的指令
            byteSendDataCommond[0] = 0xA5;
            byteSendDataCommond[1] = 0xA5;
            byteSendDataCommond[2] = 0x23;
            byteSendDataCommond[3] = byteDeviceID[3];
            byteSendDataCommond[4] = byteDeviceID[2];
            byteSendDataCommond[5] = byteDeviceID[1];
            byteSendDataCommond[6] = byteDeviceID[0];
            byteSendDataCommond[7] = 0x00; //此处是00是读操作，01是写操作
            byteSendDataCommond[8] = 0x06;
            //byteSendDataCommond[9] = 0x02;//9和10为固定的600包数
            //byteSendDataCommond[10] = 0x57;//

            byteSendDataCommond[9] = bytesTotalCount[1];//9和10为固定的600包数
            byteSendDataCommond[10] = bytesTotalCount[0];//

            if (bytesbulkCount[1] == 0x00 && bytesbulkCount[0] == 0x3A)
            {
                byteSendDataCommond[11] = 0x03;
                byteSendDataCommond[12] = 0x0A;
            }
            else if (bytesbulkCount[1] == 0x01 && bytesbulkCount[0] == 0x3A)
            {
                byteSendDataCommond[11] = 0x13;
                byteSendDataCommond[12] = 0x0A;
            }
            else if (bytesbulkCount[1] == 0x02 && bytesbulkCount[0] == 0x3A)
            {
                byteSendDataCommond[11] = 0x23;
                byteSendDataCommond[12] = 0x0A;
            }

            else
            {

                byteSendDataCommond[11] = bytesbulkCount[1];//11和12为目前要采第几段
                byteSendDataCommond[12] = bytesbulkCount[0];
            }
            //byteSendDataCommond[13] = 0x07; 
            //byteSendDataCommond[14] = 0xD0; //2000字节
            byteSendDataCommond[13] = 0x03;
            byteSendDataCommond[14] = 0xE8; //1000字节

            byteSendDataCommond[15] = 0xFF; //检验码
            byteSendDataCommond[16] = 0x5A;
            byteSendDataCommond[17] = 0x5A;

            string strtest = byteToHexStr(byteSendDataCommond);
            Console.WriteLine("发送把采集的数据回传到服务器端的命令的数据是" + strtest);
            return byteSendDataCommond;
        }


        //构建发送数据格式，目前支持命令码22，25
        public static byte[] setupCommandData(byte[] DeviceID,byte CommandCode,int DataCount,byte[] bytesData,bool isWrite)
        {
            byte[] byteSendDataCommond = new byte[12+DataCount]; //发送传输数据命令的指令
            byteSendDataCommond[0] = 0xA5;
            byteSendDataCommond[1] = 0xA5;
            byteSendDataCommond[2] = CommandCode;
            byteSendDataCommond[3] = DeviceID[3];
            byteSendDataCommond[4] = DeviceID[2];
            byteSendDataCommond[5] = DeviceID[1];
            byteSendDataCommond[6] = DeviceID[0];
            if (isWrite)
            {
                byteSendDataCommond[7] = 0x01; //此处是00是读操作，01是写操作
            }
            else
            {
                byteSendDataCommond[7] = 0x00; //此处是00是读操作，01是写操作
            }
            

            byte[] src = new byte[1];
            src[0] = (byte)(DataCount);

            //byteSendDataCommond[8] = src[0];
            byteSendDataCommond[8] = 0x04;
            for (int i = 0; i < DataCount; i++)
			{
			    byteSendDataCommond[9+i] = bytesData[i];
			}

            byteSendDataCommond[9+DataCount] = 0xFF; //检验码
            byteSendDataCommond[10+DataCount] = 0x5A;
            byteSendDataCommond[11+DataCount] = 0x5A;
            string strtest = byteToHexStr(byteSendDataCommond);
            Console.WriteLine("发送命令的数据是" + strtest);
            return byteSendDataCommond;
        }

        public static byte[] GetPhoto(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);

            byte[] photo = br.ReadBytes((int)fs.Length);

            br.Close();
            fs.Close();

            return photo;
        }
        public static void addData(string MACID, string captime, byte[] sensordata)
        {
            MySQLDB.InitDb();
            string sensorID = "0";
            //************************************************************
            try
            {
                DataSet ds1 = new DataSet("tpipe");

                string strSQL1 = "  SELECT sensorID FROM dbvpipe.tsensor where Mac=" + MACID;
                ds1 = MySQLDB.SelectDataSet(strSQL1, null);
                if (ds1 != null)
                {
                    if (ds1.Tables[0].Rows.Count > 0)
                    // 有数据集
                    {
                        sensorID = ds1.Tables[0].Rows[0][0].ToString();

                    }
                }

            }
            catch (Exception ex)
            {
                //return PublicMethod.getResultJson(ErrorCodeDefinition.DB_ERROR, ErrorCodeDefinition.getErrorMessageByErrorCode(ErrorCodeDefinition.DB_ERROR));//数据库异常
            }

            //************************************************************
            DataSet ds = new DataSet("dsarea");
            string strResult = "";
            MySqlParameter[] parmss = null;
            string strSQL = "";
            bool IsDelSuccess = false;
            strSQL = " insert into tsensorhistory (SensorID,SensorData,CAPTime,InsertTime) values (?SensorID,?SensorData,?CAPTime,?InsertTime);";

            parmss = new MySqlParameter[]
                                     {
                                         new MySqlParameter("?SensorID", MySqlDbType.Int32),
                                         new MySqlParameter("?SensorData", MySqlDbType.MediumBlob),
                                         new MySqlParameter("?CAPTime", MySqlDbType.Datetime),
                                         new MySqlParameter("?InsertTime", MySqlDbType.Datetime)
                                     };
            parmss[0].Value = Convert.ToInt32(sensorID);
            parmss[1].Value = sensordata;
            parmss[2].Value = Convert.ToDateTime(captime);
            parmss[3].Value = DateTime.Now.ToString(); ;


            try
            {
                IsDelSuccess = MySQLDB.ExecuteNonQry(strSQL, parmss);

            }

            catch (Exception ex)
            {
                //return "";
            }
            //**************************************************
        }

        private void btnCls_Click(object sender, EventArgs e)
        {
            m_allreceivedstring = "";
            txtLog.Text = "";
        }

        //下发采集命令
        private void btnAD_Click(object sender, EventArgs e)
        {
            //此处进行遍历操作
            foreach (DictionaryEntry de in htClient)
            {
                DataItem dataitem = (DataItem)de.Value;
                dataitem.isADSwitchOn = true;
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            //此处进行遍历操作
            foreach (DictionaryEntry de in htClient)
            {
                //System.Windows.Forms.MessageBox.Show(de.Key.ToString());
                //System.Windows.Forms.MessageBox.Show(de.Value.ToString());
                DataItem dataitem = (DataItem)de.Value;
                dataitem.isSendDataToServer = true;
                byte[] cmdupload = new byte[18];
                cmdupload=setupReadADData(dataitem.currentsendbulk,dataitem.byteDeviceID);
                dataitem.socket.BeginSend(cmdupload, 0, 18, SocketFlags.None,
                                                        new AsyncCallback(OnSend), dataitem.socket);
            }


        }

        private void btnSetADTime_Click(object sender, EventArgs e)
        {
            //此处进行遍历操作
            foreach (DictionaryEntry de in htClient)
            {
                DataItem dataitem = (DataItem)de.Value;
                dataitem.isSetADTimeOn = true;
                dataitem.readytosethour = Convert.ToInt32(txtHour.Text.Trim());
                dataitem.readytosetminute = Convert.ToInt32(txtMinute.Text.Trim());
            }
        }

        private void btnReadADTime_Click(object sender, EventArgs e)
        {
            //此处进行遍历操作
            foreach (DictionaryEntry de in htClient)
            {
                DataItem dataitem = (DataItem)de.Value;
                dataitem.isReadADTimeOn = true;
            }
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
            this.clientName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            int clientLen = BitConverter.ToInt32(data, 12);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 16, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 16 + nameLen, msgLen);
            else
                this.strMessage = null;

            if (clientLen > 0)
                this.clientName = Encoding.UTF8.GetString(data, 16 + nameLen + msgLen, clientLen);
            else
                this.clientName = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the clientName
            if (clientName != null)
                result.AddRange(BitConverter.GetBytes(clientName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            if (clientName != null)
                result.AddRange(Encoding.UTF8.GetBytes(clientName));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public string clientName;
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    } 
}