﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using TelegramBotBase.Args;
using TelegramBotBase.Base;
using TelegramBotBase.Interfaces;
using TelegramBotBase.Sessions;

namespace TelegramBotBase.Factories.MessageLoops
{
    public class FormBaseMessageLoop : IMessageLoopFactory
    {
        private static readonly object __evUnhandledCall = new object();

        private readonly EventHandlerList __Events = new EventHandlerList();

        public async Task MessageLoop(BotBase Bot, DeviceSession session, UpdateResult ur, MessageResult mr)
        {
            var update = ur.RawData;


            if (update.Type != UpdateType.Message
                && update.Type != UpdateType.EditedMessage
                && update.Type != UpdateType.CallbackQuery)
                return;

            //Is this a bot command ?
            if (mr.IsFirstHandler && mr.IsBotCommand &&
                Bot.BotCommands.Count(a => "/" + a.Command == mr.BotCommand) > 0)
            {
                var sce = new BotCommandEventArgs(mr.BotCommand, mr.BotCommandParameters, mr.Message, session.DeviceId,
                    session);
                await Bot.OnBotCommand(sce);

                if (sce.Handled)
                    return;
            }

            mr.Device = session;

            var activeForm = session.ActiveForm;

            //Pre Loading Event
            await activeForm.PreLoad(mr);

            //Send Load event to controls
            await activeForm.LoadControls(mr);

            //Loading Event
            await activeForm.Load(mr);


            //Is Attachment ? (Photo, Audio, Video, Contact, Location, Document) (Ignore Callback Queries)
            if (update.Type == UpdateType.Message)
                if ((mr.MessageType == MessageType.Contact)
                    | (mr.MessageType == MessageType.Document)
                    | (mr.MessageType == MessageType.Location)
                    | (mr.MessageType == MessageType.Photo)
                    | (mr.MessageType == MessageType.Video)
                    | (mr.MessageType == MessageType.Audio))
                    await activeForm.SentData(new DataResult(ur));

            //Action Event
            if (!session.FormSwitched && mr.IsAction)
            {
                //Send Action event to controls
                await activeForm.ActionControls(mr);

                //Send Action event to form itself
                await activeForm.Action(mr);

                if (!mr.Handled)
                {
                    var uhc = new UnhandledCallEventArgs(ur.Message.Text, mr.RawData, session.DeviceId, mr.MessageId,
                        ur.Message, session);
                    OnUnhandledCall(uhc);

                    if (uhc.Handled)
                    {
                        mr.Handled = true;
                        if (!session.FormSwitched) return;
                    }
                }
            }

            if (!session.FormSwitched)
            {
                //Render Event
                await activeForm.RenderControls(mr);

                await activeForm.Render(mr);
            }
        }

        /// <summary>
        ///     Will be called if no form handeled this call
        /// </summary>
        public event EventHandler<UnhandledCallEventArgs> UnhandledCall
        {
            add => __Events.AddHandler(__evUnhandledCall, value);
            remove => __Events.RemoveHandler(__evUnhandledCall, value);
        }

        public void OnUnhandledCall(UnhandledCallEventArgs e)
        {
            (__Events[__evUnhandledCall] as EventHandler<UnhandledCallEventArgs>)?.Invoke(this, e);
        }
    }
}