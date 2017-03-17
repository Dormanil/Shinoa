// <copyright file="Logging.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Niceties for logging of commands and errors.
    /// </summary>
    public static class Logging
    {
        private static IMessageChannel loggingChannel;
        private static string loggingFilePath;

        /// <summary>
        /// Logs a specific string, as given in message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task Log(string message)
        {
            PrintWithTime(message);
            if (loggingFilePath != null) await WriteLogWithTime(message, false);
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
            if (loggingFilePath != null) await WriteLogWithTime(message, true);
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
        /// Initiates logging to the file system. This method can be called inherently earlier than the <see cref="InitLoggingToChannel"/> Task.
        /// </summary>
        public static void InitLoggingToFile()
        {
            if (loggingFilePath != null) return;
            loggingFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".logs", DateTime.UtcNow.ToString("yyyyMMddhhmmssfff") + ".log");
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

        private static async Task WriteLogWithTime(string line, bool error)
        {
            try
            {
                using (var fileStream = new FileStream(loggingFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.Unicode))
                    {
                        await streamWriter.WriteLineAsync($"<{DateTime.UtcNow:u}> {(error ? "[ERROR]" : "[INFO]")} {line}");
                    }
                }
            }
            catch (Exception e)
            {
                loggingFilePath = null;
                await Logging.LogError(e.ToString());
                throw;
            }
        }
    }
}
