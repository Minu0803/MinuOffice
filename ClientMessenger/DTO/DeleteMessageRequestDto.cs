using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.DTO
{
    public class DeleteMessageRequestDto
    {
        public int RoomId { get; set; }
        public int MessageId { get; set; }
        public string RequesterId { get; set; }
    }
}
