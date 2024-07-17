using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FTPLibrary
{
     public class MessageDefs
    {
        public enum MSGTAG
        {
            MSG_FILENAME,
            MSG_FILESIZE,
            MSG_READY_READ,
            MSG_SEND,
            MSG_SUCCESS
            //MSG_OPENFILE_FAILD
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct FileInfoStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string fileName;
            public long fileSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MSGHeader
        {
            public MSGTAG msgID;
            public FileInfoStruct fileInfo;
        }
    }
}
