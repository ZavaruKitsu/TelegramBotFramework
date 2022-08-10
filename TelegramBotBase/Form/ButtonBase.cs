﻿using System.Diagnostics;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotBase.Form
{
    [DebuggerDisplay("{Text}, {Value}")]
    /// <summary>
    /// Base class for button handling
    /// </summary>
    public class ButtonBase
    {
        public ButtonBase()
        {
        }

        public ButtonBase(string Text, string Value, string Url = null)
        {
            this.Text = Text;
            this.Value = Value;
            this.Url = Url;
        }

        public virtual string Text { get; set; }

        public string Value { get; set; }

        public string Url { get; set; }


        /// <summary>
        ///     Returns an inline Button
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public virtual InlineKeyboardButton ToInlineButton(ButtonForm form)
        {
            var id = form.DependencyControl != null ? form.DependencyControl.ControlID + "_" : "";
            if (Url == null) return InlineKeyboardButton.WithCallbackData(Text, id + Value);

            var ikb = new InlineKeyboardButton(Text);

            //ikb.Text = this.Text;
            ikb.Url = Url;

            return ikb;
        }


        /// <summary>
        ///     Returns a KeyBoardButton
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public virtual KeyboardButton ToKeyboardButton(ButtonForm form)
        {
            return new KeyboardButton(Text);
        }
    }
}