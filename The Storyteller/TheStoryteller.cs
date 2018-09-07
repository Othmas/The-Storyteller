﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using The_Storyteller.Commands;
using The_Storyteller.Entities;
using The_Storyteller.Entities.Tools;

namespace The_Storyteller
{
    public class TheStoryteller : IDisposable
    {
        private readonly DiscordClient _client;
        private readonly CancellationTokenSource _cts;
        private readonly EmbedGenerator _embed;

        private readonly EntityManager _entityManager;
        private readonly Resources _res;
        private CommandsNextModule _cnext;
        private InteractivityModule _interactivity;

        public TheStoryteller()
        {
            if (!File.Exists("config.json"))
            {
                Config.Instance.SaveToFile("config.json");
                Console.WriteLine("config file is empty");
                Environment.Exit(0);
            }

            Config.Instance.LoadFromFile("config.json");
            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                EnableCompression = true,
                LogLevel = LogLevel.Debug,
                Token = Config.Instance.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });

            _interactivity = _client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = TimeoutBehaviour.Default,
                PaginationTimeout = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(30)
            });

            _cts = new CancellationTokenSource();
            _res = new Resources("textResources.json");
            _embed = new EmbedGenerator();

            _entityManager = new EntityManager();

            DependencyCollection dep = null;
            using (var d = new DependencyCollectionBuilder())
            {
                d.AddInstance(new Dependencies
                {
                    Interactivity = _interactivity,
                    Cts = _cts,
                    Resources = _res,
                    Embed = _embed,
                    Entities = _entityManager
                });
                dep = d.Build();
            }

            _cnext = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = false,
                EnableDms = true,
                EnableMentionPrefix = true,
                StringPrefix = Config.Instance.Prefix,
                IgnoreExtraArguments = true,
                Dependencies = dep
            });
            

            _cnext.RegisterCommands<Test>();

            /////////CHARACTER COMMANDS
            _cnext.RegisterCommands<Commands.CCharacter.CharacterInfo>();
            _cnext.RegisterCommands<Commands.CCharacter.Move>();


            /////////GENERAL COMMANDS
            _cnext.RegisterCommands<Commands.CGeneral.RegisterGuild>();
            _cnext.RegisterCommands<Commands.CGeneral.Start>();

            ////////MAP COMMANDS
            _cnext.RegisterCommands<Commands.CMap.RegionInfo>();
            

            _client.Ready += OnReadyAsync;
            _client.GuildCreated += OnGuildCreated;
        }

        public void Dispose()
        {
            _client.Dispose();
            _interactivity = null;
            _cnext = null;
        }

        public async Task RunAsync()
        {
            await _client.ConnectAsync();
            await WaitForCancellationAsync();
        }

        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }

        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            await Task.Yield();
        }

        private async Task OnGuildCreated(GuildCreateEventArgs e)
        {
            await Task.Yield();
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "The Storyteller", $"New Guild: {e.Guild.Name}",
                DateTime.Now);

            var embed = _embed.createEmbed(_res.GetString("introduction"), _res.GetString("introductionTypeGen"), true);
            await e.Guild.GetDefaultChannel().SendMessageAsync(embed: embed);
        }

        internal void WriteCenter(string value, int skipline = 0)
        {
            for (var i = 0; i < skipline; i++)
                Console.WriteLine();

            Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
        }
    }
}