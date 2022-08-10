﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotBase.Args;
using TelegramBotBase.Base;
using TelegramBotBase.Exceptions;
using TelegramBotBase.Form;
using TelegramBotBase.Interfaces;
using TelegramBotBase.Markdown;

namespace TelegramBotBase.Sessions
{
    /// <summary>
    ///     Base class for a device/chat session
    /// </summary>
    public class DeviceSession : IDeviceSession
    {
        private static readonly object __evMessageSent = new object();
        private static readonly object __evMessageReceived = new object();
        private static readonly object __evMessageDeleted = new object();

        private readonly EventHandlerList __Events = new EventHandlerList();

        public DeviceSession()
        {
        }

        public DeviceSession(long DeviceId)
        {
            this.DeviceId = DeviceId;
        }

        public DeviceSession(long DeviceId, FormBase StartForm)
        {
            this.DeviceId = DeviceId;
            ActiveForm = StartForm;
            ActiveForm.Device = this;
        }

        /// <summary>
        ///     Returns the ID of the last received message.
        /// </summary>
        public int LastMessageId => LastMessage?.MessageId ?? -1;

        /// <summary>
        ///     Returns the last received message.
        /// </summary>
        public Message LastMessage { get; set; }

        private MessageClient Client => ActiveForm.Client;

        /// <summary>
        ///     Returns if the messages is posted within a group.
        /// </summary>
        public bool IsGroup => LastMessage != null &&
                               (LastMessage.Chat.Type == ChatType.Group) |
                               (LastMessage.Chat.Type == ChatType.Supergroup);

        /// <summary>
        ///     Returns if the messages is posted within a channel.
        /// </summary>
        public bool IsChannel => LastMessage != null && LastMessage.Chat.Type == ChatType.Channel;

        #region "Static"

        /// <summary>
        ///     Indicates the maximum number of times a request that received error
        ///     429 will be sent again after a timeout until it receives code 200 or an error code not equal to 429.
        /// </summary>
        public static uint MaxNumberOfRetries { get; set; }

        #endregion "Static"

        /// <summary>
        ///     Device or chat id
        /// </summary>
        public long DeviceId { get; set; }

        /// <summary>
        ///     Username of user or group
        /// </summary>
        public string ChatTitle { get; set; }

        /// <summary>
        ///     When did any last action happend (message received or button clicked)
        /// </summary>
        public DateTime LastAction { get; set; }

        /// <summary>
        ///     Returns the form where the user/group is at the moment.
        /// </summary>
        public FormBase ActiveForm { get; set; }

        /// <summary>
        ///     Returns the previous shown form
        /// </summary>
        public FormBase PreviousForm { get; set; }

        /// <summary>
        ///     contains if the form has been switched (navigated)
        /// </summary>
        public bool FormSwitched { get; set; } = false;

        /// <summary>
        ///     Returns the ChatTitle depending on groups/channels or users
        /// </summary>
        /// <returns></returns>
        public string GetChatTitle()
        {
            return LastMessage?.Chat.Title
                   ?? LastMessage?.Chat.Username
                   ?? LastMessage?.Chat.FirstName
                   ?? ChatTitle;
        }


        /// <summary>
        ///     Confirm incomming action (i.e. Button click)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ConfirmAction(string CallbackQueryId, string message = "", bool showAlert = false,
            string urlToOpen = null)
        {
            try
            {
                await Client.TelegramClient.AnswerCallbackQueryAsync(CallbackQueryId, message, showAlert, urlToOpen);
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Edits the text message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public async Task<Message> Edit(int messageId, string text, ButtonForm buttons = null,
            ParseMode parseMode = ParseMode.Markdown)
        {
            InlineKeyboardMarkup markup = buttons;

            if (text.Length > Constants.Telegram.MaxMessageLength) throw new MaxLengthException(text.Length);

            try
            {
                return await API(a =>
                    a.EditMessageTextAsync(DeviceId, messageId, text, parseMode, replyMarkup: markup));
            }
            catch
            {
            }


            return null;
        }

        /// <summary>
        ///     Edits the text message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public async Task<Message> Edit(int messageId, string text, InlineKeyboardMarkup markup,
            ParseMode parseMode = ParseMode.Markdown)
        {
            if (text.Length > Constants.Telegram.MaxMessageLength) throw new MaxLengthException(text.Length);

            try
            {
                return await API(a =>
                    a.EditMessageTextAsync(DeviceId, messageId, text, parseMode, replyMarkup: markup));
            }
            catch
            {
            }


            return null;
        }

        /// <summary>
        ///     Edits the text message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public async Task<Message> Edit(Message message, ButtonForm buttons = null,
            ParseMode parseMode = ParseMode.Markdown)
        {
            InlineKeyboardMarkup markup = buttons;

            if (message.Text.Length > Constants.Telegram.MaxMessageLength)
                throw new MaxLengthException(message.Text.Length);

            try
            {
                return await API(a =>
                    a.EditMessageTextAsync(DeviceId, message.MessageId, message.Text, parseMode, replyMarkup: markup));
            }
            catch
            {
            }


            return null;
        }

        /// <summary>
        ///     Edits the reply keyboard markup (buttons)
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="bf"></param>
        /// <returns></returns>
        public async Task<Message> EditReplyMarkup(int messageId, ButtonForm bf)
        {
            try
            {
                return await API(a => a.EditMessageReplyMarkupAsync(DeviceId, messageId, bf));
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        ///     Sends a simple text message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> Send(long deviceId, string text, ButtonForm buttons = null, int replyTo = 0,
            bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown,
            bool MarkdownV2AutoEscape = true)
        {
            if (ActiveForm == null)
                return null;

            InlineKeyboardMarkup markup = buttons;

            if (text.Length > Constants.Telegram.MaxMessageLength) throw new MaxLengthException(text.Length);

            if (parseMode == ParseMode.MarkdownV2 && MarkdownV2AutoEscape) text = text.MarkdownV2Escape();

            try
            {
                var t = API(a => a.SendTextMessageAsync(deviceId, text, parseMode, replyToMessageId: replyTo,
                    replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends a simple text message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> Send(string text, ButtonForm buttons = null, int replyTo = 0,
            bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown,
            bool MarkdownV2AutoEscape = true)
        {
            return await Send(DeviceId, text, buttons, replyTo, disableNotification, parseMode, MarkdownV2AutoEscape);
        }

        /// <summary>
        ///     Sends a simple text message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="markup"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> Send(string text, InlineKeyboardMarkup markup, int replyTo = 0,
            bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown,
            bool MarkdownV2AutoEscape = true)
        {
            if (ActiveForm == null)
                return null;

            if (text.Length > Constants.Telegram.MaxMessageLength) throw new MaxLengthException(text.Length);

            if (parseMode == ParseMode.MarkdownV2 && MarkdownV2AutoEscape) text = text.MarkdownV2Escape();

            try
            {
                var t = API(a => a.SendTextMessageAsync(DeviceId, text, parseMode, replyToMessageId: replyTo,
                    replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends a simple text message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="markup"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> Send(string text, ReplyMarkupBase markup, int replyTo = 0,
            bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown,
            bool MarkdownV2AutoEscape = true)
        {
            if (ActiveForm == null)
                return null;

            if (text.Length > Constants.Telegram.MaxMessageLength) throw new MaxLengthException(text.Length);

            if (parseMode == ParseMode.MarkdownV2 && MarkdownV2AutoEscape) text = text.MarkdownV2Escape();

            try
            {
                var t = API(a => a.SendTextMessageAsync(DeviceId, text, parseMode, replyToMessageId: replyTo,
                    replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends an image
        /// </summary>
        /// <param name="file"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendPhoto(InputOnlineFile file, string caption = null, ButtonForm buttons = null,
            int replyTo = 0, bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown)
        {
            if (ActiveForm == null)
                return null;

            InlineKeyboardMarkup markup = buttons;

            try
            {
                var t = API(a => a.SendPhotoAsync(DeviceId, file, caption, parseMode, replyToMessageId: replyTo,
                    replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends an video
        /// </summary>
        /// <param name="file"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendVideo(InputOnlineFile file, string caption = null, ButtonForm buttons = null,
            int replyTo = 0, bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown)
        {
            if (ActiveForm == null)
                return null;

            InlineKeyboardMarkup markup = buttons;

            try
            {
                var t = API(a => a.SendVideoAsync(DeviceId, file, caption: caption, parseMode: parseMode,
                    replyToMessageId: replyTo, replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends an video
        /// </summary>
        /// <param name="file"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendVideo(string url, ButtonForm buttons = null, int replyTo = 0,
            bool disableNotification = false, ParseMode parseMode = ParseMode.Markdown)
        {
            if (ActiveForm == null)
                return null;

            InlineKeyboardMarkup markup = buttons;

            try
            {
                var t = API(a => a.SendVideoAsync(DeviceId, new InputOnlineFile(url), parseMode: parseMode,
                    replyToMessageId: replyTo, replyMarkup: markup, disableNotification: disableNotification));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Sends an document
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="document"></param>
        /// <param name="caption"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendDocument(string filename, byte[] document, string caption = "",
            ButtonForm buttons = null, int replyTo = 0, bool disableNotification = false)
        {
            var ms = new MemoryStream(document);

            var fts = new InputOnlineFile(ms, filename);

            return await SendDocument(fts, caption, buttons, replyTo, disableNotification);
        }

        /// <summary>
        ///     Generates a Textfile from scratch with the specified encoding. (Default is UTF8)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="textcontent"></param>
        /// <param name="encoding">Default is UTF8</param>
        /// <param name="caption"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendTextFile(string filename, string textcontent, Encoding encoding = null,
            string caption = "", ButtonForm buttons = null, int replyTo = 0, bool disableNotification = false)
        {
            encoding = encoding ?? Encoding.UTF8;

            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, encoding);

            sw.Write(textcontent);
            sw.Flush();

            var content = ms.ToArray();

            return await SendDocument(filename, content, caption, buttons, replyTo, disableNotification);
        }

        /// <summary>
        ///     Sends an document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="caption"></param>
        /// <param name="buttons"></param>
        /// <param name="replyTo"></param>
        /// <param name="disableNotification"></param>
        /// <returns></returns>
        public async Task<Message> SendDocument(InputOnlineFile document, string caption = "",
            ButtonForm buttons = null, int replyTo = 0, bool disableNotification = false)
        {
            InlineKeyboardMarkup markup = null;
            if (buttons != null) markup = buttons;

            try
            {
                var t = API(a => a.SendDocumentAsync(DeviceId, document, caption, replyMarkup: markup,
                    disableNotification: disableNotification, replyToMessageId: replyTo));

                var o = GetOrigin(new StackTrace());
                OnMessageSent(new MessageSentEventArgs(await t, o));

                return await t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Set a chat action (showed to the user)
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task SetAction(ChatAction action)
        {
            await API(a => a.SendChatActionAsync(DeviceId, action));
        }

        /// <summary>
        ///     Requests the contact from the user.
        /// </summary>
        /// <param name="buttonText"></param>
        /// <param name="requestMessage"></param>
        /// <param name="OneTimeOnly"></param>
        /// <returns></returns>
        public async Task<Message> RequestContact(string buttonText = "Send your contact",
            string requestMessage = "Give me your phone number!", bool OneTimeOnly = true)
        {
            var rck = new ReplyKeyboardMarkup(KeyboardButton.WithRequestContact(buttonText));
            rck.OneTimeKeyboard = OneTimeOnly;
            return await API(a => a.SendTextMessageAsync(DeviceId, requestMessage, replyMarkup: rck));
        }

        /// <summary>
        ///     Requests the location from the user.
        /// </summary>
        /// <param name="buttonText"></param>
        /// <param name="requestMessage"></param>
        /// <param name="OneTimeOnly"></param>
        /// <returns></returns>
        public async Task<Message> RequestLocation(string buttonText = "Send your location",
            string requestMessage = "Give me your location!", bool OneTimeOnly = true)
        {
            var rcl = new ReplyKeyboardMarkup(KeyboardButton.WithRequestLocation(buttonText));
            rcl.OneTimeKeyboard = OneTimeOnly;
            return await API(a => a.SendTextMessageAsync(DeviceId, requestMessage, replyMarkup: rcl));
        }

        public async Task<Message> HideReplyKeyboard(string closedMsg = "Closed", bool autoDeleteResponse = true)
        {
            try
            {
                var m = await Send(closedMsg, new ReplyKeyboardRemove());

                if (autoDeleteResponse && m != null) await DeleteMessage(m);

                return m;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        ///     Deletes a message
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteMessage(int messageId = -1)
        {
            await RAW(a => a.DeleteMessageAsync(DeviceId, messageId));

            OnMessageDeleted(new MessageDeletedEventArgs(messageId));

            return true;
        }

        /// <summary>
        ///     Deletes the given message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteMessage(Message message)
        {
            return await DeleteMessage(message.MessageId);
        }


        public virtual async Task ChangeChatPermissions(ChatPermissions permissions)
        {
            try
            {
                await API(a => a.SetChatPermissionsAsync(DeviceId, permissions));
            }
            catch
            {
            }
        }

        private Type GetOrigin(StackTrace stackTrace)
        {
            for (var i = 0; i < stackTrace.FrameCount; i++)
            {
                var methodBase = stackTrace.GetFrame(i).GetMethod();

                //Debug.WriteLine(methodBase.Name);

                if (methodBase.DeclaringType.IsSubclassOf(typeof(FormBase)) |
                    methodBase.DeclaringType.IsSubclassOf(typeof(ControlBase))) return methodBase.DeclaringType;
            }

            return null;
        }

        /// <summary>
        ///     Gives access to the original TelegramClient without any Exception catchings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="call"></param>
        /// <returns></returns>
        public T RAW<T>(Func<ITelegramBotClient, T> call)
        {
            return call(Client.TelegramClient);
        }

        /// <summary>
        ///     This will call a function on the TelegramClient and automatically Retry if an limit has been exceeded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="call"></param>
        /// <returns></returns>
        public async Task<T> API<T>(Func<ITelegramBotClient, Task<T>> call)
        {
            var numberOfTries = 0;
            while (numberOfTries < MaxNumberOfRetries)
                try
                {
                    return await call(Client.TelegramClient);
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode != 429)
                        throw;

                    if (ex.Parameters != null && ex.Parameters.RetryAfter != null)
                        await Task.Delay(ex.Parameters.RetryAfter.Value * 1000);

                    numberOfTries++;
                }

            return default;
        }

        /// <summary>
        ///     This will call a function on the TelegramClient and automatically Retry if an limit has been exceeded.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public async Task API(Func<ITelegramBotClient, Task> call)
        {
            var numberOfTries = 0;
            while (numberOfTries < MaxNumberOfRetries)
                try
                {
                    await call(Client.TelegramClient);
                    return;
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode != 429)
                        throw;

                    if (ex.Parameters != null && ex.Parameters.RetryAfter != null)
                        await Task.Delay(ex.Parameters.RetryAfter.Value * 1000);

                    numberOfTries++;
                }
        }

        #region "Users"

        public virtual async Task RestrictUser(long userId, ChatPermissions permissions, DateTime until = default)
        {
            try
            {
                await API(a => a.RestrictChatMemberAsync(DeviceId, userId, permissions, until));
            }
            catch
            {
            }
        }

        public virtual async Task<ChatMember> GetChatUser(long userId)
        {
            try
            {
                return await API(a => a.GetChatMemberAsync(DeviceId, userId));
            }
            catch
            {
            }

            return null;
        }

        [Obsolete("User BanUser instead.")]
        public virtual async Task KickUser(long userId, DateTime until = default)
        {
            try
            {
                await API(a => a.BanChatMemberAsync(DeviceId, userId, until));
            }
            catch
            {
            }
        }

        public virtual async Task BanUser(long userId, DateTime until = default)
        {
            try
            {
                await API(a => a.BanChatMemberAsync(DeviceId, userId, until));
            }
            catch
            {
            }
        }

        public virtual async Task UnbanUser(long userId)
        {
            try
            {
                await API(a => a.UnbanChatMemberAsync(DeviceId, userId));
            }
            catch
            {
            }
        }

        #endregion


        #region "Events"

        /// <summary>
        ///     Eventhandler for sent messages
        /// </summary>
        public event EventHandler<MessageSentEventArgs> MessageSent
        {
            add => __Events.AddHandler(__evMessageSent, value);
            remove => __Events.RemoveHandler(__evMessageSent, value);
        }


        public void OnMessageSent(MessageSentEventArgs e)
        {
            (__Events[__evMessageSent] as EventHandler<MessageSentEventArgs>)?.Invoke(this, e);
        }

        /// <summary>
        ///     Eventhandler for received messages
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived
        {
            add => __Events.AddHandler(__evMessageReceived, value);
            remove => __Events.RemoveHandler(__evMessageReceived, value);
        }


        public void OnMessageReceived(MessageReceivedEventArgs e)
        {
            (__Events[__evMessageReceived] as EventHandler<MessageReceivedEventArgs>)?.Invoke(this, e);
        }

        /// <summary>
        ///     Eventhandler for deleting messages
        /// </summary>
        public event EventHandler<MessageDeletedEventArgs> MessageDeleted
        {
            add => __Events.AddHandler(__evMessageDeleted, value);
            remove => __Events.RemoveHandler(__evMessageDeleted, value);
        }


        public void OnMessageDeleted(MessageDeletedEventArgs e)
        {
            (__Events[__evMessageDeleted] as EventHandler<MessageDeletedEventArgs>)?.Invoke(this, e);
        }

        #endregion
    }
}