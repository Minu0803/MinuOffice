using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.DTO
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string LastMessage { get; set; }
        public string Timestamp { get; set; }
    }
}
