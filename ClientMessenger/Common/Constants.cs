using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.Common
{
    internal static class Constants
    {
        public const string Signature = "MWSG";
        public const int ServerPort = 9000;

        // 최대 파일 크기 제한 (이미지, 일반 파일 공통 적용)
        public const int DefaultBufferSize = 1024 * 1024;
    }
}
