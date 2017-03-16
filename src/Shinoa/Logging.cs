// <copyright file="Logging.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Niceties for logging of commands and errors.
    /// </summary>
    public static class Logging
    {
        private static IMessageChannel loggingChannel;

        /// <summary>
        /// Logs a specific string, as given in message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task Log(string message)
        {
            PrintWithTime(message);
            var sendMessageAsync = loggingChannel?.SendMessageAsync(message);
            if (sendMessageAsync != null) await sendMessageAsync;
        }

        /// <summary>
        /// Logs a specific string, as given in message, as an error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task LogError(string message)
        {
            PrintErrorWithTime(message);
            var embed = new EmbedBuilder
            {
                Title = "Error",
                Color = new Color(200, 0, 0),
                Description = $"```{message}```",
                Author =
                    new EmbedAuthorBuilder
                    {
                        IconUrl = Shinoa.Client.CurrentUser.GetAvatarUrl(),
                        Name = nameof(Shinoa),
                    },
                Timestamp = DateTimeOffset.Now,
                Footer = new EmbedFooterBuilder
                {
                    Text = Shinoa.VersionString,
                },
            };
            var sendEmbedAsync = loggingChannel?.SendEmbedAsync(embed);
            if (sendEmbedAsync != null) await sendEmbedAsync;
        }

        /// <summary>
        /// Logs a specific Discord message as specified by the CommandContext.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task LogMessage(SocketCommandContext context)
        {
            await Log(!(context.Channel is IPrivateChannel)
                ? $"[{context.Guild.Name} -> #{context.Channel.Name}] {context.User.Username}: {context.Message.Content}"
                : $"[PM] {context.User.Username}: {context.Message.Content}");
        }

        /// <summary>
        /// Initialises logging to a specific Discord Channel.
        /// </summary>
        /// <param name="channel">Reference to the channel in which we want to log.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task InitLoggingToChannel(IMessageChannel channel)
        {
            if (loggingChannel != null || channel == null) return;
            loggingChannel = channel;
            await Log($"Now logging to channel \"{channel.Name}\".");
        }

        /// <summary>
        /// Stops replicating the log to a specific channel.
        /// </summary>
        public static void StopLoggingToChannel()
        {
            loggingChannel = null;
        }

        private static void PrintWithTime(string line)
        {
            Console.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }

        private static void PrintErrorWithTime(string line)
        {
            Console.Error.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }
    }
}
