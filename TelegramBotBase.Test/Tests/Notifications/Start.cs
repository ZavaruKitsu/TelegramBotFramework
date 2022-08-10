﻿using System.Threading.Tasks;
using TelegramBotBase.Base;
using TelegramBotBase.Enums;
using TelegramBotBase.Form;

namespace TelegramBotBaseTest.Tests.Notifications
{
    public class Start : AutoCleanForm
    {
        private bool sent;

        public Start()
        {
            DeleteMode = eDeleteMode.OnLeavingForm;
        }

        public override async Task Action(MessageResult message)
        {
            if (message.Handled)
                return;

            switch (message.RawData)
            {
                case "alert":

                    await message.ConfirmAction("This is an alert.", true);

                    break;
                case "back":

                    var mn = new Menu();
                    await NavigateTo(mn);

                    break;
                default:

                    await message.ConfirmAction("This is feedback");

                    break;
            }
        }

        public override async Task Render(MessageResult message)
        {
            if (sent)
                return;

            var bf = new ButtonForm();
            bf.AddButtonRow("Normal feeback", "normal");
            bf.AddButtonRow("Alert Box", "alert");
            bf.AddButtonRow("Back", "back");

            await Device.Send("Choose your test", bf);

            sent = true;
        }
    }
}