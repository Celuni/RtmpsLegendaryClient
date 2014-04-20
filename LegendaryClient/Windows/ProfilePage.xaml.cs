﻿using LegendaryClient.Logic;
using LegendaryClient.Logic.Riot;
using LegendaryClient.Logic.Riot.Platform;
using LegendaryClient.Windows.Profile;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public ProfilePage(string Name = "")
        {
            InitializeComponent();
            InGameContainer.Content = new Ingame();
            OverviewContainer.Content = new Overview();
            LeaguesContainer.Content = new Leagues();
            MatchHistoryContainer.Content = new MatchHistory();
            ChampionsContainer.Content = new Champions();
            RunesContainer.Content = new Runes();
            MasteriesContainer.Content = new Masteries();
            SkinsContainer.Content = new Skins();

            if (String.IsNullOrEmpty(Name))
            {
                GetSummonerProfile(Client.LoginPacket.AllSummonerData.Summoner.Name);
            }
            else
            {
                GetSummonerProfile(Name);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            GetSummonerProfile(SearchTextBox.Text);
        }

        public async void GetSummonerProfile(string s)
        {
            PublicSummoner Summoner = await RiotCalls.GetSummonerByName(String.IsNullOrWhiteSpace(s) ? Client.LoginPacket.AllSummonerData.Summoner.Name : s);
            if (Summoner == null)
            {
                MessageOverlay overlay = new MessageOverlay();
                overlay.MessageTitle.Content = "No Summoner Found";
                overlay.MessageTextBox.Text = "The summoner \"" + s + "\" does not exist.";
                Client.OverlayContainer.Content = overlay.Content;
                Client.OverlayContainer.Visibility = Visibility.Visible;
                return;
            }
            SummonerNameLabel.Content = Summoner.Name;
            SummonerLevelLabel.Content = "Level " + Summoner.SummonerLevel;

            if (Summoner.SummonerLevel < 30)
            {
                LeagueHeader.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                SummonerLeaguesDTO dto = await RiotCalls.GetAllLeaguesForPlayer(Summoner.SummonerId);
                GotLeaguesForPlayer(dto);
            }

            int ProfileIconID = Summoner.ProfileIconId;
            var uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "profileicon", ProfileIconID + ".png");
            ProfileImage.Source = Client.GetImage(uriSource);

            try
            {
                PlatformGameLifecycleDTO n = await RiotCalls.RetrieveInProgressSpectatorGameInfo(s);
                InGameHeader.Visibility = Visibility.Visible;
                InGameHeader.IsSelected = true;
                Ingame ingame = InGameContainer.Content as Ingame;
                ingame.Update(n);
            }
            catch
            {
                InGameHeader.Visibility = Visibility.Collapsed;
                OverviewHeader.IsSelected = true;
            }

            if (Summoner.Name == Client.LoginPacket.AllSummonerData.Summoner.Name)
            {
                ChampionsTab.Visibility = System.Windows.Visibility.Visible;
                SkinsTab.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                ChampionsTab.Visibility = System.Windows.Visibility.Hidden;
                SkinsTab.Visibility = System.Windows.Visibility.Hidden;
            }

            MatchHistory history = MatchHistoryContainer.Content as MatchHistory;
            history.Update(Summoner.AcctId);

            Overview overview = OverviewContainer.Content as Overview;
            overview.Update(Summoner.SummonerId, Summoner.AcctId);
        }

        private void GotLeaguesForPlayer(SummonerLeaguesDTO result)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                if (result.SummonerLeagues != null && result.SummonerLeagues.Count > 0)
                {
                    LeagueHeader.Visibility = System.Windows.Visibility.Visible;
                    Leagues overview = LeaguesContainer.Content as Leagues;
                    overview.Update(result);
                }
                else
                {
                    LeagueHeader.Visibility = System.Windows.Visibility.Collapsed;
                }
            }));
        }

        public class KeyValueItem
        {
            public object Key { get; set; }

            public object Value { get; set; }
        }

        private void TabContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((string)((TabItem)TabContainer.SelectedItem).Header)
            {
                case "Champions":
                    Champions champions = ChampionsContainer.Content as Champions;
                    champions.Update();
                    break;

                case "Skins":
                    Skins skins = SkinsContainer.Content as Skins;
                    skins.Update();
                    break;

                default:
                    break;
            }
        }
    }
}