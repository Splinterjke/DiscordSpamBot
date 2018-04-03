using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSpamBot
{
    class Program
    {
        private DiscordClient client;
        private string token;
        private List<DiscordGuild> guilds;

        static void Main(string[] args)
        {
            new Program().Run().GetAwaiter().GetResult();
        }

        private void ConsoleLog(string msg)
        {
            Console.WriteLine($"[Discord Spam BOT] [{DateTime.Now.ToShortTimeString()}]: {msg}");
        }

        private async Task Run()
        {
            var welcomeMsg = "-----[Discord Spam BOT by Splinter#8029]-----";
            Console.SetCursorPosition((Console.WindowWidth - welcomeMsg.Length) / 2, Console.CursorTop);
            Console.WriteLine(welcomeMsg);
            var verMsg = "Version: 1.0";
            Console.SetCursorPosition((Console.WindowWidth - verMsg.Length) / 2, Console.CursorTop);
            Console.WriteLine(verMsg);
            var dateMsg = "Release: 06.2017";
            Console.SetCursorPosition((Console.WindowWidth - dateMsg.Length) / 2, Console.CursorTop);
            Console.WriteLine(dateMsg);
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                var json = "";
                using (var fs = File.OpenRead("config.json"))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    json = await sr.ReadToEndAsync();
                var cfgJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (!cfgJson.ContainsKey("token") || string.IsNullOrEmpty(cfgJson["token"]?.ToString()))
                {
                    ConsoleLog("error: invalid token");
                    return;
                }

                client = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = cfgJson["token"].ToString(),
                    TokenType = TokenType.User,
                    LogLevel = LogLevel.Info,
                    UseInternalLogHandler = false
                });
                client.Ready += Client_Ready;
                client.ClientErrored += Client_ClientErrored;
                client.GuildAvailable += Client_GuildAvailable;
                await client.ConnectAsync();
                new Task(() => StartRaidAsync()).Start();
            }
            catch (Exception ex)
            {
                ConsoleLog($"exception occured: {ex.GetType()} {ex.Message}");
            }
            await Task.Delay(-1);
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            guilds.Add(e.Guild);
            return Task.CompletedTask;
        }

        private Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            ConsoleLog($"error: at {e.EventName} {e.Exception.GetType()} {e.Exception.Message}");
            return Task.CompletedTask;
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            guilds = new List<DiscordGuild>(e.Client.Guilds.Count);
            ConsoleLog("client is ready to process events");
            return Task.CompletedTask;
        }

        private async void StartRaidAsync()
        {
            while (guilds == null || guilds.Count != guilds.Capacity)
            {
                continue;
            }
            var random = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";            
            for (int i = 0; i < guilds.Count; i++)
            {
                var members = guilds[i].Members.Where(x=>!x.IsBot).ToList();
                for (int j = 0; j < members.Count; j++)
                {
                    try
                    {
                        var dmChannel = await client.CreateDmAsync(members[j]);
                        var msg = new string(Enumerable.Repeat(pool, 5).Select(s => s[random.Next(s.Length)]).ToArray());
                        await dmChannel.SendMessageAsync("https://discord.gg/nHUZYVd" + $"\n{msg}");
                        await dmChannel.DeleteAsync();
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }
                    catch (DSharpPlus.Exceptions.RateLimitException ex)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(10));
                        j--;
                    }
                    catch (DSharpPlus.Exceptions.UnauthorizedException ex)
                    {
                        if (ex.JsonMessage.Contains("fast"))
                            await Task.Delay(TimeSpan.FromMinutes(10));
                        j--;
                    }
                    catch (DSharpPlus.Exceptions.NotFoundException ex)
                    {
                        j++;
                    }
                    catch (Exception ex)
                    {
                        j++;
                    }
                }
            }
        }
    }
}
