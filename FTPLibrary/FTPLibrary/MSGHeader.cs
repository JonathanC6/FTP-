using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FTPLibrary
{
    public enum MSGTAG
    {
        MSG_FILENAME = 1,
        MSG_FILESIZE = 2,
        MSG_READY_READ = 3,
        MSG_SEND = 4,
        MSG_SUCCESS = 5
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class MSGHeader
    {
        public MSGTAG msgID;
        public FileInfo fileInfo;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class FileInfo
        {
            public int fileSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string fileName;
        }
    }
}
