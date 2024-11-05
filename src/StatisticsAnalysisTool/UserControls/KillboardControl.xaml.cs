using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Guild;
using StatisticsAnalysisTool.Killboard;
using StatisticsAnalysisTool.Models.ApiModel;
using StatisticsAnalysisTool.Properties;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;

namespace StatisticsAnalysisTool.UserControls;
/// <summary>
/// Interaction logic for KillboardControl.xaml
/// </summary>
public partial class KillboardControl : UserControl
{
    private List<GameInfoPlayerKillsDeaths> killboard = new List<GameInfoPlayerKillsDeaths>();
    private DispatcherTimer aTimer;

    public KillboardControl() {
        InitializeComponent();
        killboard.Clear();

        LoadFromFileAsync();

        aTimer = new DispatcherTimer();
        aTimer.Tick += new EventHandler(OnTimedEvent);
        aTimer.Interval = TimeSpan.FromSeconds(60);
        aTimer.Start();
    }

    private void OnTimedEvent(object source, EventArgs e) {
        CheckForNewEntries();
    }

    private async void CheckForNewEntries() {
        if (string.IsNullOrEmpty(txtGuildName.Text) && string.IsNullOrEmpty(txtAllianceName.Text) && string.IsNullOrEmpty(txtPlayerName.Text)) return;

        string guildName = txtGuildName.Text;
        string playerName = txtPlayerName.Text;
        string allianceName = txtAllianceName.Text;
        var value = await ApiController.GetGameInfoEventsFromJsonAsync(guildName, playerName, allianceName, 51, 1);

        foreach (var e in value) {
            var eventFound = false;
            foreach (var k in killboard) {
                if (k.EventId != e.EventId) continue;

                eventFound = true;
                break;
            }

            if (eventFound) continue;

            Button button = new Button();
            button.Content = $"{e.Killer.Name} -> {e.Victim.Name} ({e.EventId}) [{e.TimeStamp.ToLocalTime()}]";
            button.ToolTip = e.EventId.ToString();
            button.Foreground = new SolidColorBrush(Colors.White);
            button.FontSize = 14;
            button.Click += new RoutedEventHandler(button_Click);
            stackKillboardEntries.Children.Add(button);
            killboard.Add(e);
        }

        await SaveInFileAsync();
    }

    private void button_Click(object sender, RoutedEventArgs e) {
        var eventId = int.Parse((sender as Button).ToolTip.ToString());

        if (killboard.Count > 50) {
            killboard.RemoveRange(50, killboard.Count - 50);
        }

        foreach (var k in killboard) {
            if (k.EventId != eventId) continue;

            lblLink.Inlines.Clear();
            lblLink.Inlines.Add($"https://albiononline.com/killboard/kill/{k.EventId}?server=live_us");
            lblLink.NavigateUri = new Uri($"https://albiononline.com/killboard/kill/{k.EventId}?server=live_us");
            lblTime.Content = k.TimeStamp.ToLocalTime().ToString();
            lblParty.Content = k.GroupMemberCount.ToString();
            lblAssist.Content = (k.NumberOfParticipants - 1).ToString();
            if (k.Location != null) lblLocation.Content = k.Location.ToString();
            lblFame.Content = k.TotalVictimKillFame.ToString("N0");
            lblValue.Content = 0.ToString("N0");

            lblKillerName.Content = k.Killer.Name;
            lblKillerGuild.Content = k.Killer.GuildName;
            lblKillerIP.Content = k.Killer.AverageItemPower.ToString("N0");

            lblVictimName.Content = k.Victim.Name;
            lblVictimGuild.Content = k.Victim.GuildName;
            lblVictimIP.Content = k.Victim.AverageItemPower.ToString("N0");

            break;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
    }

    private void Button_Click_1(object sender, RoutedEventArgs e) {
        killboard.Clear();
        stackKillboardEntries.Children.Clear();
    }

    #region Save / Load data

    public async Task LoadFromFileAsync() {
        FileController.TransferFileIfExistFromOldPathToUserDataDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.KillboardFileName));

        var dto = await FileController.LoadAsync<KillboardDto>(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.KillboardFileName));

        txtPlayerName.Text = dto.PlayerName;
        txtGuildName.Text = dto.GuildName;
        txtAllianceName.Text = dto.AllianceName;
    }

    public async Task SaveInFileAsync() {
        DirectoryController.CreateDirectoryWhenNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName));
        await FileController.SaveAsync(new KillboardDto() {
            PlayerName = txtPlayerName.Text,
            GuildName = txtGuildName.Text,
            AllianceName = txtAllianceName.Text
        },
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.KillboardFileName));
        Log.Information("Killboard data saved");
    }

    #endregion
}
