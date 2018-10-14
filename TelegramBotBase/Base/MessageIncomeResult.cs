﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBotBase.Sessions;

namespace TelegramBotBase.Base
{
    public class MessageIncomeResult : EventArgs
    {

        public long DeviceId { get; set; }

        public DeviceSession Device { get; set; }

        public MessageResult Message { get; set; }

        public MessageIncomeResult(long DeviceId, DeviceSession Device, MessageResult message)
        {
            this.DeviceId = DeviceId;
            this.Device = Device;
            this.Message = message;
        }




    }
}
