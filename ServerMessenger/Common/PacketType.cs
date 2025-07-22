using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.Common
{
    public enum PacketType
    {
        //채팅 메시지 타입
        TextMessage,
        ImageMessage,
        FileMessage,

        // 인증
        LoginRequest,
        LoginResponse, 

        SignUpRequest,
        SignUpResponse, 

        CheckIdDuplicateRequest,
        CheckIdDuplicateResponse,

        // 파일 관련
        FileSaveRequest, // 클라이언트가 서버에 파일 저장 요청
        FileSaveResponse,

        // 방 관련
        CreateRoomRequest, // 그룹방 생성 요청
        CreateRoomResponse, // 그룹방 생성 결과 (성공/실패)

        //메시지 관련
        LoadMessagesRequest, // 채팅 내역 불러오기 요청    
        LoadMessagesResponse, // 채팅 내역 불러오기 결과 전송   

        DeleteMessageRequest, // 채팅 메시지 삭제 요청 
        DeleteMessageResponse, // 채팅 메시지 삭제 결과 전송

        // 기타
        DisconnectRequest // 클라이언트가 서버와 연결 종료 요청
    }
}
