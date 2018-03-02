﻿using CodenameIndigo.Modules.Models;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CodenameIndigo.Modules.Preconditions;

namespace CodenameIndigo.Modules.Commands
{
    public class ViewBracketsCommand : InteractiveBase
    {
        [Command("bracket")]
        public async Task ViewBrackets(int tid = 0)
        {
            if (tid == 0)
                tid = (await DatabaseHelper.GetLatestTourneyAsync()).ID;
            List<Bracket> brackets = new List<Bracket>();
            IUserMessage message = await Context.Channel.SendMessageAsync("Loading brackets...");

            MySqlConnection conn = DatabaseHelper.GetClosedConnection();
            try
            {
                await conn.OpenAsync();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `battles` WHERE `tid` = {tid} ORDER BY `bid` DESC", conn);

                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        brackets.Add(new Bracket(tid, reader.GetInt32("player1"), reader.GetInt32("player2"), reader.GetInt32("round"), reader.GetInt32("bid"), reader.GetInt32("winner")));
                    }
                }
            }
            catch (Exception e)
            {
                await Program.Log(e.ToString(), "DatabaseConn", LogSeverity.Error);
            }
            finally
            {
                await conn.CloseAsync();
            }

            if (brackets.Count == 0)
            {
                await message.ModifyAsync((s) => s.Content = $"Tournament {tid} doesn't exist or hasn't started yet.");
                return;
            }
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            try
            {
                foreach (Bracket item in brackets)
                {
                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    string p1 = (await item.FetchPlayerAsync(1)).DiscordName, p2 = (await item.FetchPlayerAsync(2)).DiscordName;

                    field.Name = $"Battle {item.BID}:";
                    if (item.Winner == 1)
                        field.Value =
                            $"**{p1}**\n" +
                            $"VS\n" +
                            $"~~{p2}~~";
                    else if (item.Winner == 2)
                        field.Value =
                            $"~~{p1}~~\n" +
                            $"VS\n" +
                            $"**{p2}**";
                    else
                        field.Value =
                            $"{p1}\n" +
                            $"VS\n" +
                            $"{p2}";
                    field.IsInline = true;
                    fields.Add(field);
                }
            }
            catch (Exception e)
            {
                await Program.Log(e.ToString(), "BracketBuilder", LogSeverity.Error);
                await message.ModifyAsync((s) => s.Content = $"An error occured. Please contact an administrator.");
            }
            EmbedBuilder builder = new EmbedBuilder() { Title = $"{(await DatabaseHelper.GetTourneyByIDAsync(tid)).Name}'s Brackets", Color = Color.DarkMagenta, Fields = fields };
            try
            {
                await message.ModifyAsync((s) =>
                {
                    s.Content = "";
                    s.Embed = builder.Build();
                });
            }
            catch (Exception e)
            {
                await Program.Log(e.ToString(), "BracketBuilder", LogSeverity.Error);
                await message.ModifyAsync((s) => s.Content = $"An error occured. Please contact an administrator.");
            }
        }
    }
}
