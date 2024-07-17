using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static FTPLibrary.MessageDefs;
using System.Runtime.InteropServices;
using System.IO;

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
                //processMsg(clientSocket); 
                Task.Run(() => processMsg(clientSocket));
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
                    if (bytesReceived > 0)
                    {
                        MSGHeader receivedHeader = ByteArrayToStructure<MSGHeader>(buffer);

                        if (receivedHeader.msgID == MSGTAG.MSG_FILESIZE)
                        {
                            Console.WriteLine($"File size received: {receivedHeader.fileInfo.fileSize} bytes");

                            if (receivedHeader.fileInfo.fileSize > 0)
                            {
                                PrepareForReceiving(fileName);
                            }
                        }
                        else if (receivedHeader.msgID == MSGTAG.MSG_SUCCESS)
                        {
                            Console.WriteLine("File transfer complete");
                            break;
                        }
                        else
                        {
                            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                            Console.WriteLine("Message received from server: " + receivedMessage);
                        }
                    }
  
                    //string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    //Console.WriteLine("Message received from server: " + receivedMessage);

                    //等待5秒后发送下一条消息
                    //Thread.Sleep(5000);
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
                fileInfo = new FileInfoStruct
                {
                    fileName = fileName,
                    fileSize = 0
                }
            };

            byte[] messageBytes = StructureToByteArray(msgHeader);
            clientSocket.Send(messageBytes);
            Console.WriteLine("File name sent: " + fileName);
        }

        private void PrepareForReceiving(string fileName)
        {
            MSGHeader msgHeader = new MSGHeader
            {
                msgID = MSGTAG.MSG_READY_READ,
                fileInfo = new FileInfoStruct
                {
                    fileName = fileName,
                    fileSize = 0
                }
            };

            byte[] messageBytes = StructureToByteArray(msgHeader);
            clientSocket.Send(messageBytes);
            Console.WriteLine("Ready to receive file data for: " + fileName);

            ReceiveFileData(fileName);
        }

        private void ReceiveFileData(string fileName)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[1024];
                    int bytesReceived;

                    while ((bytesReceived = clientSocket.Receive(buffer)) > 0)
                    {
                        fs.Write(buffer, 0, bytesReceived);
                    }
                }

                Console.WriteLine("File received successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error receiving file data: {ex.Message}");
            }
        }

        private byte[] StructureToByteArray<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        private T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
