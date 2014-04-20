﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using LegendaryClient.Controls;
using LegendaryClient.Logic;
using LegendaryClient.Logic.SQLite;
using System;
using System.Windows.Threading;
using System.Threading;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Logic.Riot.Leagues;
using LegendaryClient.Logic.Riot;

namespace LegendaryClient.Windows.Profile
{
    /// <summary>
    /// Interaction logic for Leagues.xaml
    /// </summary>
    public partial class Leagues : Page
    {
        SummonerLeaguesDTO MyLeagues;
        string SelectedRank;
        string Queue;
        AggregatedStats SelectedAggregatedStats;

        public Leagues()
        {
            InitializeComponent();
        }

        public void Update(SummonerLeaguesDTO result)
        {
            MyLeagues = result;
            foreach (LeagueListDTO leagues in MyLeagues.SummonerLeagues)
            {
                if (leagues.Queue == "RANKED_SOLO_5x5")
                    SelectedRank = leagues.RequestorsRank;
            }
            Queue = "RANKED_SOLO_5x5";
            RenderLeague();
        }

        public void RenderLeague()
        {
            LeaguesListView.Items.Clear();
            foreach (LeagueListDTO leagues in MyLeagues.SummonerLeagues)
            {
                if (leagues.Queue == Queue)
                {
                    if (SelectedRank == "V")
                    {
                        UpTierButton.IsEnabled = true;
                        DownTierButton.IsEnabled = false;
                    }
                    else if (SelectedRank == "I")
                    {
                        UpTierButton.IsEnabled = false;
                        DownTierButton.IsEnabled = true;
                    }
                    else
                    {
                        UpTierButton.IsEnabled = true;
                        DownTierButton.IsEnabled = true;
                    }

                    CurrentLeagueLabel.Content = leagues.Tier + " " + SelectedRank;
                    CurrentLeagueNameLabel.Content = leagues.Name;
                    List<LeagueItemDTO> players = leagues.Entries.OrderBy(o => o.LeaguePoints).Where(item => item.Rank == SelectedRank).ToList();
                    players.Reverse();
                    int i = 0;
                    foreach (LeagueItemDTO player in players)
                    {
                        i++;
                        LeagueItem item = new LeagueItem();
                        item.DataContext = player;
                        item.PlayerLabel.Content = player.PlayerOrTeamName;

                        item.PlayerRankLabel.Content = i;
                        if (i - player.PreviousDayLeaguePosition != 0)
                            item.RankChangeLabel.Content = i - player.PreviousDayLeaguePosition;

                        //TypedObject miniSeries = player.MiniSeries as TypedObject;
                        //if (miniSeries != null)
                        //    item.LPLabel.Content = ((string)miniSeries["progress"]).Replace('N', '-');

                        LeaguesListView.Items.Add(item);
                    }
                }
            }
            LeaguesListView.SelectedIndex = 0;
        }

        private void DownTierButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedRank == "I")
                SelectedRank = "II";
            else if (SelectedRank == "II")
                SelectedRank = "III";
            else if (SelectedRank == "III")
                SelectedRank = "IV";
            else if (SelectedRank == "IV")
                SelectedRank = "V";
            RenderLeague();
        }

        private void UpTierButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedRank == "V")
                SelectedRank = "IV";
            else if (SelectedRank == "IV")
                SelectedRank = "III";
            else if (SelectedRank == "III")
                SelectedRank = "II";
            else if (SelectedRank == "II")
                SelectedRank = "I";
            RenderLeague();
        }

        private async void LeaguesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeaguesListView.SelectedItem != null)
            {
                LeagueItem item = (LeagueItem)LeaguesListView.SelectedItem;
                TopChampionsListView.Items.Clear();
                PublicSummoner x = await RiotCalls.GetSummonerByName((string)item.PlayerLabel.Content);
                AggregatedStats stats = await RiotCalls.GetAggregatedStats(x.AcctId, "CLASSIC", Client.LoginPacket.ClientSystemStates.currentSeason.ToString());
                GotStats(stats);
                PlayerLabel.Content = item.PlayerLabel.Content;
            }
        }

        private void GotStats(AggregatedStats stats)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                SelectedAggregatedStats = stats;
            
                ViewAggregatedStatsButton.IsEnabled = false;
                List<AggregatedChampion> ChampionStats = new List<AggregatedChampion>();
                int i = 0;
                TopChampionsListView.Items.Clear();
                if (SelectedAggregatedStats.LifetimeStatistics.Count() > 0)
                {
                    foreach (AggregatedStat stat in SelectedAggregatedStats.LifetimeStatistics)
                    {
                        AggregatedChampion Champion = null;
                        Champion = ChampionStats.Find(x => x.ChampionId == stat.ChampionId);
                        if (Champion == null)
                        {
                            Champion = new AggregatedChampion();
                            Champion.ChampionId = stat.ChampionId;
                            ChampionStats.Add(Champion);
                        }
            
                        var type = typeof(AggregatedChampion);
                        string fieldName = Client.TitleCaseString(stat.StatType.Replace('_', ' ')).Replace(" ", "");
                        var f = type.GetField(fieldName);
                        f.SetValue(Champion, stat.Value);
                    }
            
                    ChampionStats.Sort((x, y) => y.TotalSessionsPlayed.CompareTo(x.TotalSessionsPlayed));
            
                    foreach (AggregatedChampion info in ChampionStats)
                    {
                        //Show the top 6 champions
                        if (i++ > 6)
                            break;
                        ViewAggregatedStatsButton.IsEnabled = true;
                        if (info.ChampionId != 0.0)
                        {
                            ChatPlayer player = new ChatPlayer();
                            champions Champion = champions.GetChampion(Convert.ToInt32(info.ChampionId));
                            player.LevelLabel.Visibility = System.Windows.Visibility.Hidden;
                            player.PlayerName.Content = Champion.displayName;
                            player.PlayerStatus.Content = info.TotalSessionsPlayed + " games played";
                            player.ProfileImage.Source = champions.GetChampion(Champion.id).icon;
                            TopChampionsListView.Items.Add(player);
                        }
                    }
                }
            }));
        }

        private void ViewAggregatedStatsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Client.OverlayContainer.Content = new AggregatedStatsOverlay(SelectedAggregatedStats, false).Content;
            Client.OverlayContainer.Visibility = System.Windows.Visibility.Visible;
        }
    }
}