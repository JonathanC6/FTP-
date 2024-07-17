using FTPLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPclient
{
    internal class ftpClient
    {
        static void Main(string[] args)
        {
            ftpClientLib ftpCli = new ftpClientLib();
            ftpCli.connectToServer();
            Console.ReadKey();
        }
    }
}
