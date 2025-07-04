using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.Common
{
    public enum PacketType
    {
        TextMessage,
        ImageMessage,
        FileMessage,
        LoginRequest,
        LoginResponse,
        SignUpRequest,
        SignUpResponse,
        FileSaveRequest, // 클라이언트가 서버에 파일 저장 요청
        FileSaveBroadcast // 서버가 파일 저장이 완료되었음 클라에게 알림 
    }
}
