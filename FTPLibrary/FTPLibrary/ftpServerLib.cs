using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

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
                //最大并发连接数为10，开始监听
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
                        MessageDefs.MSGHeader msgHeader = ByteArrayToStructure<MessageDefs.MSGHeader>(buffer);
                        //string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        //Console.WriteLine("Message received from client: " + receivedMessage);

                        switch (msgHeader.msgID)
                        {
                            case MessageDefs.MSGTAG.MSG_FILENAME:
                                Console.WriteLine("File name received: " + msgHeader.fileInfo.fileName);
                                readFile(msgHeader.fileInfo.fileName, clientSocket);
                                break;
                            case MessageDefs.MSGTAG.MSG_READY_READ:
                                Console.WriteLine("Client is ready to receive data");
                                sendFileData(msgHeader.fileInfo.fileName, clientSocket);
                                break;
                            default:
                                Console.WriteLine("Unknown message type received.");
                                break;
                        }

                        //发送确认消息给客户端
                        SendConfirmation(clientSocket, $"Message received!");

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

        //实现函数readFile，当服务器收到文件名时，读取文件，获得文件大小并发回给客户端
        private void readFile(string fileName, Socket clientSocket)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    long fileSize = fileInfo.Length;

                    Console.WriteLine($"File size: {fileSize} bytes");

                    MessageDefs.MSGHeader responseHeader = new MessageDefs.MSGHeader
                    {
                        msgID = MessageDefs.MSGTAG.MSG_FILESIZE,
                        fileInfo = new MessageDefs.FileInfoStruct
                        {
                            fileName = fileName,
                            fileSize = fileSize
                        }
                    };

                    byte[] responseBytes = StructureToByteArray(responseHeader);
                    clientSocket.Send(responseBytes);
                }
                else
                {
                    Console.WriteLine($"File not found: {fileName}");
                    SendConfirmation(clientSocket, "File not found");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading file: {ex.Message}");
                SendConfirmation(clientSocket, "Error reading file");
            }
        }

        private void sendFileData(string fileName, Socket clientSocket)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    byte[] fileData = File.ReadAllBytes(fileName);
                    clientSocket.Send(fileData);
                    Console.WriteLine($"File data sent: {fileName}");

                    SendConfirmation(clientSocket, "File transfer complete");
                }
                else
                {
                    Console.WriteLine($"File not found: {fileName}");
                    SendConfirmation(clientSocket, "File not found");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending file data: {ex.Message}");
                SendConfirmation(clientSocket, "Error sending file data");
            }
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

        private void SendConfirmation(Socket clientSocket, string message)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(message);
            clientSocket.Send(responseBytes);
            Console.WriteLine("Confirmation sent to client: " + message);
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
    }
}
