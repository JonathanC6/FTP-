using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using FTPLibrary;
using System.Runtime.InteropServices;

namespace FTPLibrary
{
    public class ftpServerLib
    {
        private string ipAddress;
        private ushort port;
        private Socket serverSocket;
        //private ftpClientLib clientLibInstance;
        private byte[] buffer = new byte[1024]; //用来接收客户端发送的消息

        public ftpServerLib(string ipAddress, ushort port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            //this.clientLibInstance = new ftpClientLib(); //实例化 ftpClientLib
        }
        //监听客户端连接
        public void listenToClient()
        {
            //创建serverSocket套接字，初始化socket库 (ipv4,流式,TCP)
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                //给Socket绑定本地IP地址和端口号
                //IPAddress ipAddr = IPAddress.Any;
                IPAddress ipAddr = IPAddress.Parse(ipAddress);
                IPEndPoint endPoint = new IPEndPoint(ipAddr, port);
                serverSocket.Bind(endPoint);
                //开始监听
                serverSocket.Listen(10);

                Console.WriteLine($"Server started and listening on {ipAddress}:{port}");

                //开始处理消息
                while (true)
                {
                    //接受客户端连接
                    Socket clientSocket = serverSocket.Accept();
                    Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint}");

                    //在新的任务中处理客户端连接
                    Task.Run(() => processClient(clientSocket));
                }
            }
            catch (SocketException ex) //Socket相关操作中可能抛出的异常
            {
                Console.Error.WriteLine($"Socket error: {ex.Message}");
                serverSocket?.Close();
            }
            catch (FormatException ex) //解析格式不正确的字符串时可能抛出的异常
            {
                Console.Error.WriteLine($"Invalid IP address format: {ex.Message}");
                serverSocket?.Close();
            }
        }

        private void processClient(Socket clientSocket)
        {
            try
            {
                while (true)
                {
                    int bytesReceived = clientSocket.Receive(buffer);
                    if (bytesReceived > 0)
                    {
                        MSGHeader msgHeader = ByteArrayToStructure<MSGHeader>(buffer);
                        //string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        //Console.WriteLine("Message received from client: " + receivedMessage);

                        switch (msgHeader.msgID)
                        {
                            case MSGTAG.MSG_FILENAME:
                                Console.WriteLine("File name received: " + msgHeader.fileInfo.fileName);
                                break;
                            case MSGTAG.MSG_FILESIZE:
                                Console.WriteLine("File size received: " + msgHeader.fileInfo.fileSize);
                                break;
                            case MSGTAG.MSG_READY_READ:
                                Console.WriteLine("Client is ready to read");
                                break;
                            case MSGTAG.MSG_SEND:
                                Console.WriteLine("Message received: " + msgHeader.fileInfo.fileName);
                                break;
                            case MSGTAG.MSG_SUCCESS:
                                Console.WriteLine("Operation successful");
                                break;
                            default:
                                Console.WriteLine("Unknown message type received.");
                                break;
                        }

                        //发送确认消息给客户端
                        SendConfirmation(clientSocket, "Message received!I'm HUST");

                        //清空缓冲区
                        Array.Clear(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine($"Socket error: {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
            }
        }

        private T ByteArrayToStructure<T>(byte[] bytes) where T : class
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)) as T;
            }
            finally
            {
                handle.Free();
            }
        }

        private void SendConfirmation(Socket clientSocket, string message)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(message);
            clientSocket.Send(responseBytes);
            Console.WriteLine("Confirmation sent to client: " + message);
        }
    }
}
