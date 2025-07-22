using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ServerMessenger.Models
{
    public class RoomModel
    {
        public int RoomId { get; set; } // 방 고유 ID
        public string RoomName { get; set; } // 방 이름 (그룹일 경우 표시용)
        public List<string> Participants { get; set; } = new List<string>(); // 참여자 ID 목록
        public List<MessageModel> Messages { get; set; } = new List<MessageModel>();

    }
}
