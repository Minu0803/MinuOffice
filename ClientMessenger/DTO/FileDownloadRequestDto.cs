﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.DTO
{
    public class FileDownloadRequestDto
    {
        public int RoomId { get; set; }
        public string FileName { get; set; }
    }

}
