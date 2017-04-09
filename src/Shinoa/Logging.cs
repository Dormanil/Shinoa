// <copyright file="Logging.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Niceties for logging of commands and errors.
    /// </summary>
    public static class Logging
    {
        private static readonly SemaphoreSlim FileLoggingSemaphore = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim DiscordLoggingSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Queue<Task> DiscordLogQueue = new Queue<Task>(50);
        private static Timer loggingTimer;
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
            await DiscordLoggingSemaphore.WaitAsync();
            try
            {
                DiscordLogQueue.Enqueue(new Task(
                    async () =>
                    {
                        var sendMessageAsync = loggingChannel?.SendMessageAsync(message);
                        if (sendMessageAsync == null) return;
                        try
                        {
                            await sendMessageAsync;
                        }
                        catch (Exception e)
                        {
                            StopLoggingToChannel();
                            await LogError(e.ToString());
                            await Task.Delay(TimeSpan.FromMinutes(1));
                            await Shinoa.TryReenableLogging();
                        }
                    }));
            }
            finally
            {
                DiscordLoggingSemaphore.Release();
            }
        }

        /// <summary>
        /// Logs a specific string, as given in message, as an error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task LogError(string message)
        {
            PrintErrorWithTime(message);
            if (loggingFilePath != null) await WriteLogWithTime(message, true);
            await DiscordLoggingSemaphore.WaitAsync();
            try
            {
                DiscordLogQueue.Enqueue(new Task(
                    () =>
                    {
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
                        var sendMessageAsync = loggingChannel?.SendEmbedAsync(embed);
                        if (sendMessageAsync == null) return;
                        try
                        {
                            sendMessageAsync.GetAwaiter().GetResult();
                        }
                        catch (Exception e)
                        {
                            StopLoggingToChannel();
                            LogError(e.ToString()).GetAwaiter().GetResult();
                            Task.Delay(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
                            Shinoa.TryReenableLogging().GetAwaiter().GetResult();
                        }
                    }));
            }
            finally
            {
                DiscordLoggingSemaphore.Release();
            }
        }

        /// <summary>
        /// Logs a specific Discord message as specified by the CommandContext.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task LogMessage(SocketCommandContext context)
        {
            await Log(!(context.Channel is IPrivateChannel)
                ? $"[{context.Guild.Name} (ID: {context.Guild.Id}) -> #{context.Channel.Name} (ID: {context.Channel.Id})] {context.User.Username} (ID: {context.User.Id}): {context.Message.Content}"
                : $"[PM] {context.User.Username} (ID: {context.User.Id}): {context.Message.Content}");
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
            loggingTimer = new Timer(
                s =>
                {
                    ProcessLogQueue().GetAwaiter().GetResult();
                },
                null,
                0,
                1000);
            await Log($"Now logging to channel \"{channel.Name}\".");
        }

        /// <summary>
        /// Initiates logging to the file system. This method can be called inherently earlier than the <see cref="InitLoggingToChannel"/> Task.
        /// </summary>
        public static void InitLoggingToFile()
        {
            if (loggingFilePath != null) return;
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".logs.old")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".logs.old"));
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".logs")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".logs"));
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".logs.old", "logs.zip")))
            {
                using (var archive = new ZipArchive(new FileStream(Path.Combine(Directory.GetCurrentDirectory(), ".logs.old", "logs.zip"), FileMode.OpenOrCreate, FileAccess.ReadWrite), ZipArchiveMode.Update))
                {
                    foreach (var path in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), ".logs")))
                    {
                        archive.CreateEntryFromFile(path, Path.GetFileName(path));
                        File.Delete(path);
                    }
                }
            }

            loggingFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".logs", DateTime.UtcNow.ToString("yyyyMMddhhmmssfff") + ".log");
        }

        /// <summary>
        /// Stops replicating the log to a specific channel.
        /// </summary>
        public static void StopLoggingToChannel()
        {
            loggingChannel = null;
            loggingTimer.Dispose();
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
            await FileLoggingSemaphore.WaitAsync();
            try
            {
                using (var fileStream = new FileStream(loggingFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fileStream.Seek(0, SeekOrigin.End);
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.Unicode))
                    {
                        await streamWriter.WriteLineAsync($"<{DateTime.UtcNow:u}> {(error ? "[ERROR]" : "[INFO]")} {line}");
                    }
                }
            }
            catch (Exception e)
            {
                loggingFilePath = null;
                await LogError(e.ToString());
            }
            finally
            {
                FileLoggingSemaphore.Release();
            }
        }

        private static async Task ProcessLogQueue()
        {
            if (loggingChannel != null)
            {
                await DiscordLoggingSemaphore.WaitAsync();
                try
                {
                    if (DiscordLogQueue.Count > 0)
                    {
                        var task = DiscordLogQueue.Dequeue();
                        task.Start();
                        await Task.WhenAll(task, Task.Delay(1000));
                    }
                }
                finally
                {
                    DiscordLoggingSemaphore.Release();
                }
            }
        }
    }
}
