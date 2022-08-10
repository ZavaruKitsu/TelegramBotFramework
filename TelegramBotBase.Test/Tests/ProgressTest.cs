﻿using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramBotBase.Base;
using TelegramBotBase.Controls.Inline;
using TelegramBotBase.Enums;
using TelegramBotBase.Form;

namespace TelegramBotBaseTest.Tests
{
    public class ProgressTest : AutoCleanForm
    {
        public ProgressTest()
        {
            DeleteMode = eDeleteMode.OnLeavingForm;
            Opened += ProgressTest_Opened;
            Closed += ProgressTest_Closed;
        }


        private async Task ProgressTest_Opened(object sender, EventArgs e)
        {
            await Device.Send("Welcome to ProgressTest");
        }

        public override async Task Action(MessageResult message)
        {
            var call = message.GetData<CallbackData>();

            await message.ConfirmAction();


            if (call == null)
                return;

            ProgressBar Bar = null;

            switch (call.Value)
            {
                case "standard":

                    Bar = new ProgressBar(0, 100, ProgressBar.eProgressStyle.standard);
                    Bar.Device = Device;

                    break;

                case "squares":

                    Bar = new ProgressBar(0, 100, ProgressBar.eProgressStyle.squares);
                    Bar.Device = Device;

                    break;

                case "circles":

                    Bar = new ProgressBar(0, 100, ProgressBar.eProgressStyle.circles);
                    Bar.Device = Device;

                    break;

                case "lines":

                    Bar = new ProgressBar(0, 100, ProgressBar.eProgressStyle.lines);
                    Bar.Device = Device;

                    break;

                case "squaredlines":

                    Bar = new ProgressBar(0, 100, ProgressBar.eProgressStyle.squaredLines);
                    Bar.Device = Device;

                    break;

                case "start":

                    var sf = new Menu();

                    await NavigateTo(sf);

                    return;

                default:

                    return;
            }


            //Render Progress bar and show some "example" progress
            await Bar.Render(message);

            Controls.Add(Bar);

            for (var i = 0; i <= 100; i++)
            {
                Bar.Value++;
                await Bar.Render(message);

                Thread.Sleep(250);
            }
        }


        public override async Task Render(MessageResult message)
        {
            var btn = new ButtonForm();
            btn.AddButtonRow(new ButtonBase("Standard", new CallbackData("a", "standard").Serialize()),
                new ButtonBase("Squares", new CallbackData("a", "squares").Serialize()));

            btn.AddButtonRow(new ButtonBase("Circles", new CallbackData("a", "circles").Serialize()),
                new ButtonBase("Lines", new CallbackData("a", "lines").Serialize()));

            btn.AddButtonRow(new ButtonBase("Squared Line", new CallbackData("a", "squaredlines").Serialize()));

            btn.AddButtonRow(new ButtonBase("Back to start", new CallbackData("a", "start").Serialize()));

            await Device.Send("Choose your progress bar:", btn);
        }

        private async Task ProgressTest_Closed(object sender, EventArgs e)
        {
            await Device.Send("Ciao from ProgressTest");
        }
    }
}