using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static FTPLibrary.MSGHeader;
using System.Runtime.InteropServices;
using FTPLibrary;

namespace FTPLibrary
{
    public class ftpClientLib
    {
        private string ipAddress = "127.0.0.1";
        private ushort port = 8888;
        private Socket clientSocket;

        /*
        public ftpClientLib(string ipAddress, ushort port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }
        */

        //连接到服务器
        public void connectToServer()
        {
            //创建clientSocket套接字，初始化socket库 (ipv4,流式,TCP)
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                IPAddress ipAddr = IPAddress.Parse(ipAddress);
                IPEndPoint endPoint = new IPEndPoint(ipAddr, port);
                clientSocket.Connect(endPoint);

                Console.WriteLine($"Connected to server at {ipAddress}:{port}");

                //开始处理消息
                processMsg(clientSocket);
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine($"Socket error: {ex.Message}");
                clientSocket?.Close();
            }
            catch (FormatException ex)
            {
                Console.Error.WriteLine($"Invalid IP address format: {ex.Message}");
                clientSocket?.Close();
            }
        }

        //处理消息
        public void processMsg(Socket s)
        {
            //处理消息的逻辑 e.g.:
            try
            {
                while (true)
                {
                    Console.Write("Enter the name of the file to download: ");
                    string fileName = Console.ReadLine();

                    SendFileName(fileName);

                    //string message = "Hello, Server!This is WHU";
                    //byte[] messageBytes = Encoding.UTF8.GetBytes(fileName);
                    //s.Send(messageBytes);
                    //Console.WriteLine("File name sent: " + fileName);

                    //接收服务器响应
                    byte[] buffer = new byte[1024];
                    int bytesReceived = s.Receive(buffer);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine("Message received from server: " + receivedMessage);

                    //等待5秒后发送下一条消息
                    //Thread.Sleep(5000);

                    //如果接收到特定的退出消息，可以跳出循环
                    if (receivedMessage.Contains("exit"))
                    {
                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine($"Socket error: {ex.Message}");
            }
            finally
            {
                s.Close();
            }
        }

        private void SendFileName(string fileName)
        {
            MSGHeader msgHeader = new MSGHeader
            {
                msgID = MSGTAG.MSG_FILENAME,
                fileInfo = new MSGHeader.FileInfo
                {
                    fileSize = 0, // Not relevant for file name message
                    fileName = fileName
                }
            };

            byte[] messageBytes = StructureToByteArray(msgHeader);
            clientSocket.Send(messageBytes);
            Console.WriteLine("File name sent: " + fileName);
        }

        private byte[] StructureToByteArray<T>(T obj) where T : class
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }
    }
}
