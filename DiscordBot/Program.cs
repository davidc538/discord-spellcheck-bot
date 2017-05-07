using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Discord;
using Discord.Commands;

using NHunspell;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args) => new Program().Start();

        private DiscordClient _client;
        private Hunspell _hunspell;

        private string StripPunctuation(string text)
        {
            StringBuilder retVal =new StringBuilder();

            foreach (char c in text)
            {
                if (char.IsPunctuation(c) && c != '\'')
                    retVal.Append(" ");
                else retVal.Append(c);
            }

            return retVal.ToString();
        }

        private void Start()
        {
            _hunspell = new Hunspell("en_us.aff", "en_us.dic");

            _client = new DiscordClient(x =>
            {
                x.AppName = "Spellcheck bot";
                x.AppUrl = "www.google.com";
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = (object sender, LogMessageEventArgs e) => { Console.WriteLine($"[{e.Severity}] [{e.Source}] {e.Message}"); };
            });

            _client.UsingCommands(x =>
            {
                x.PrefixChar = '/';
                x.AllowMentionPrefix = true;
                x.HelpMode = HelpMode.Public;
            });

            _client.MessageReceived += (object sender, MessageEventArgs e) =>
            {
                if (e.Message.User.IsBot) return;

                string s_message = Regex.Replace(e.Message.Text, @"\p{Cs}", "");

                s_message = StripPunctuation(s_message);

                int errorCount = 0;

                string[] words = s_message.Split(' ');
                Dictionary<string, List<string> > suggestions = new Dictionary<string, List<string> >();

                foreach (string word in words)
                {
                    bool correct = _hunspell.Spell(word);

                    if (!correct)
                    {
                        errorCount++;

                        List<string> _suggestions = _hunspell.Suggest(word);

                        suggestions[word] = _suggestions;
                    }
                }

                StringBuilder message = new StringBuilder();

                foreach (KeyValuePair<string, List<string> > typo in suggestions)
                {
                    message.Append("\r\n" + typo.Key + " -> ");

                    int temp = 0;

                    foreach (string suggestion in typo.Value)
                    {
                        message.Append(suggestion);

                        if (++temp < typo.Value.Count) message.Append(", ");
                    }
                }

                if (errorCount > 0)
                {
                    e.Channel.SendMessage(message.ToString());
                }
            };

            var token = File.ReadAllText("discord_token.token");

            var eService = _client.GetService<CommandService>();

            eService.CreateCommand("ping").Description("returns pong").Do(async (e) => { await e.Channel.SendMessage("pong"); });
            
            _client.ExecuteAndWait(async () => { await _client.Connect(token, TokenType.Bot); });
        }
    }
}
