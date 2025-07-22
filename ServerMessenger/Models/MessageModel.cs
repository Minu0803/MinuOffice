using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.Models
{
    public class MessageModel
    {
        public int MessageId { get; set; }
        public string UserId { get; set; }
        public int RoomId { get; set; }
        public string SenderNickname { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } 
        public string Content { get; set; }  // 메시지 텍스트
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string FileUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsDeleted { get; set; }
         
    }
}

