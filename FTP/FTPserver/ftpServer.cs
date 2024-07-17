using FTPLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPserver
{
    internal class ftpServer
    {
        static void Main(string[] args)
        {
            ftpServerLib ftpSer = new ftpServerLib("127.0.0.1", 8888);
            ftpSer.listenToClient();
            Console.ReadKey();
        }
    }
}
