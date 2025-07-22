using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.DTO
{
    public class DeleteMessageResponseDto
    {
        public int RoomId { get; set; }
        public int MessageId { get; set; }
        public bool IsDeleted { get; set; } = true;
        public bool IsForAll { get; set; }
    }
}
