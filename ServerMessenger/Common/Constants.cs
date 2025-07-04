using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.Common
{
    internal static class Constants
    {
        public const string Signature = "MWSG";
        public const int ServerPort = 9000;

        // 파일 경로 (방 정보, 유저 정보, 채팅 내역 저장 위치)
        public const string RoomDataPath = "Data/Rooms/";
        public const string UserDataPath = "Data/Users/";
        public const string MessageDataPath = "Data/Messages/";

        // 최대 파일 크기 제한 (이미지, 일반 파일 공통 적용)
        public const int MaxFileSize = 10 * 1024 * 1024;  // 10MB
    }
}
