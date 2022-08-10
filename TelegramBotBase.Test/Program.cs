﻿using System;
using System.Linq;
using TelegramBotBase.Builder;
using TelegramBotBase.Commands;
using TelegramBotBase.Enums;
using TelegramBotBaseTest.Tests;

namespace TelegramBotBaseTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var apiKey = Environment.GetEnvironmentVariable("API_TOKEN") ?? throw new Exception("API_TOKEN is not set");

            var bb = BotBaseBuilder
                .Create()
                .WithAPIKey(apiKey)
                .DefaultMessageLoop()
                .WithStartForm<Start>()
                .NoProxy()
                .CustomCommands(a =>
                {
                    a.Start("Starts the bot");
                    a.Help("Should show you some help");
                    a.Settings("Should show you some settings");
                    a.Add("form1", "Opens test form 1");
                    a.Add("form2", "Opens test form 2");
                    a.Add("params", "Returns all send parameters as a message.");
                })
                .NoSerialization()
                .UseEnglish()
                .Build();


            bb.BotCommand += async (s, en) =>
            {
                switch (en.Command)
                {
                    case "/start":

                        var start = new Menu();

                        await en.Device.ActiveForm.NavigateTo(start);

                        break;
                    case "/form1":

                        var form1 = new TestForm();

                        await en.Device.ActiveForm.NavigateTo(form1);

                        break;

                    case "/form2":

                        var form2 = new TestForm2();

                        await en.Device.ActiveForm.NavigateTo(form2);

                        break;

                    case "/params":

                        var m = en.Parameters.DefaultIfEmpty("").Aggregate((a, b) => a + " and " + b);

                        await en.Device.Send("Your parameters are: " + m, replyTo: en.Device.LastMessageId);

                        en.Handled = true;

                        break;
                }
            };

            //Update Bot commands to botfather
            bb.UploadBotCommands().Wait();

            bb.SetSetting(eSettings.LogAllMessages, true);

            bb.Message += (s, en) =>
            {
                Console.WriteLine(en.DeviceId + " " + en.Message.MessageText + " " + (en.Message.RawData ?? ""));
            };

            bb.Start();


            Console.WriteLine("Telegram Bot started...");

            Console.WriteLine("Press q to quit application.");


            Console.ReadLine();

            bb.Stop();
        }
    }
}
