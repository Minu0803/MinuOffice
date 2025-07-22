using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.DTO
{
    public class FileDownloadResponseDto
    {
        public int RoomId { get; set; }
        public string SenderId { get; set; }
        public string FileName { get; set; }
        public int TotalChunks { get; set; }
        public int ChunkIndex { get; set; }
        public bool IsFinal { get; set; }
    }
}
