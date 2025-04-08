using System.Linq;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System.Windows;
using System.Windows.Input;
using StatisticsAnalysisTool.Guild;
using System.Windows.Threading;
using System;
using System.Windows.Media;
using System.Windows.Controls;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Interaction logic for LoggingControl.xaml
/// </summary>
public partial class GuildControl
{
    private bool _isSelectAllActive;
    private DispatcherTimer aTimer;
    private DispatcherTimer wTimer;
    private bool _timerRunning = false;
    private int _defaultTime = 3600;
    private int _currentTime = 3600;
    private int _whisperLimit = 50;
    private int _whisperCount = 0;
    private MainWindowViewModel _mainWindowViewModel;
    private GuildBindings _guildBinding;

    public GuildControl()
    {
        InitializeComponent();

        aTimer = new DispatcherTimer();
        aTimer.Tick += new EventHandler(OnTimedEvent);
        aTimer.Interval = TimeSpan.FromSeconds(5);

        wTimer = new DispatcherTimer();
        wTimer.Tick += new EventHandler(OnWTimedEvent);
        wTimer.Interval = TimeSpan.FromSeconds(1);
    }

    private void OnTimedEvent(object source, EventArgs e) {
        RefreshList();
    }

    private void OnWTimedEvent(object source, EventArgs e) {
        _currentTime -= 1;
        if (_currentTime <= 0) {
            _guildBinding.IsSearchingForGuildlessPlayers = true;
            _currentTime = _defaultTime;
            txtWhisperTimer.Text = "00:00";
            txtWhisperTimer.Foreground = new SolidColorBrush(Colors.Green);
            _whisperCount = 0;
            txtWhisperSent.Text = $"{_whisperCount}/{_whisperLimit}";
            txtWhisperSent.Foreground = new SolidColorBrush(Colors.Green);
            aTimer.Start();
            wTimer.Stop();
            return;
        }

        TimeSpan t = TimeSpan.FromSeconds(_currentTime);
        string answer = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        txtWhisperTimer.Text = answer;
    }

    private async void BtnDeleteSelectedSiphonedEnergyEntries_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DialogWindow(LocalizationController.Translation("DELETE_SELECTED_ENTRIES"), LocalizationController.Translation("SURE_YOU_WANT_TO_DELETE_SELECTED_ENTRIES"));
        var dialogResult = dialog.ShowDialog();

        var mainWindowViewModel = ServiceLocator.Resolve<MainWindowViewModel>();

        if (mainWindowViewModel == null)
        {
            return;
        }

        if (dialogResult is true)
        {
            var trackingController = ServiceLocator.Resolve<TrackingController>();

            var selectedEntries = mainWindowViewModel.GuildBindings.SiphonedEnergyCollectionView?.Cast<SiphonedEnergyItem>()
                .ToList()
                .Where(x => x?.IsSelectedForDeletion ?? false)
                .Select(x => x.GetHashCode());

            mainWindowViewModel.GuildBindings.IsDeleteEntriesButtonEnabled = false;
            await trackingController?.GuildController?.RemoveTradesByIdsAsync(selectedEntries)!;
            mainWindowViewModel.GuildBindings.IsDeleteEntriesButtonEnabled = true;
        }
    }

    private void OpenSiphonedEnergyInfoPopup_MouseEnter(object sender, MouseEventArgs e) {
        _guildBinding.GuildPopupVisibility = Visibility.Visible;
    }

    private void CloseSiphonedEnergyInfoPopup_MouseLeave(object sender, MouseEventArgs e) {
        _guildBinding.GuildPopupVisibility = Visibility.Collapsed;
    }

    private void BtnSelectSwitchAllSiphonedEnergyEntries_Click(object sender, RoutedEventArgs e)
    {
        if ((MainWindowViewModel) DataContext is not { GuildBindings.SiphonedEnergyList: not null } mainWindowViewModel)
        {
            return;
        }

        foreach (var item in mainWindowViewModel.GuildBindings.SiphonedEnergyList)
        {
            item.IsSelectedForDeletion = !_isSelectAllActive;
        }

        _isSelectAllActive = !_isSelectAllActive;
    }

    private void btnRefreshList_Click(object sender, RoutedEventArgs e) {
        _mainWindowViewModel = (MainWindowViewModel) DataContext;
        _guildBinding = _mainWindowViewModel.GuildBindings;
        aTimer.Start();
        _timerRunning = true;
        _guildBinding.IsSearchingForGuildlessPlayers = true;
    }

    private void RefreshList() {
        stackUnguildedPlayers.Children.Clear();
        int count = 0;
        foreach (var name in _guildBinding.UnguildedPlayers) {
            Button button = new Button();
            button.Content = name;
            button.ToolTip = $"Total: {Utilities.FameConvertion(_guildBinding.UnguildedPlayersFame[count].Item1)}\n" +
                $"Kill: {Utilities.FameConvertion(_guildBinding.UnguildedPlayersFame[count].Item2)}\n" +
                $"PvE: {Utilities.FameConvertion(_guildBinding.UnguildedPlayersFame[count].Item3)}\n" +
                $"Gathering: {Utilities.FameConvertion(_guildBinding.UnguildedPlayersFame[count].Item4)}\n" +
                $"Crafting: {Utilities.FameConvertion(_guildBinding.UnguildedPlayersFame[count].Item5)}";
            button.Foreground = new SolidColorBrush(Colors.White);
            button.FontSize = 14;
            button.Click += new RoutedEventHandler(button_Click);
            button.MouseRightButtonDown += new MouseButtonEventHandler(button_RClick);
            stackUnguildedPlayers.Children.Add(button);
            count++;
        }
    }

    private void RemoveEntry(string playerName) {
        if (_whisperCount >= 50) {
            _guildBinding.IsSearchingForGuildlessPlayers = false;
            txtWhisperSent.Foreground = new SolidColorBrush(Colors.Red);
            txtWhisperTimer.Foreground = new SolidColorBrush(Colors.Red);
            aTimer.Stop();
            _guildBinding.UnguildedPlayers.Clear();
            RefreshList();
            wTimer.Start();
            return;
        }

        if (_guildBinding.UnguildedPlayers.Contains(playerName)) {
            _whisperCount++;
            txtWhisperSent.Text = $"{_whisperCount}/{_whisperLimit}";

            var index = _guildBinding.UnguildedPlayers.IndexOf(playerName);
            _guildBinding.PlayersAlreadyInvited.Add(_guildBinding.UnguildedPlayers[index]);
            _guildBinding.UnguildedPlayersFame.RemoveAt(index);
            _guildBinding.UnguildedPlayers.RemoveAt(index);
        }

        RefreshList();
    }

    private void button_Click(object sender, RoutedEventArgs e) {
        Clipboard.SetText($"/w {(sender as Button).Content} {txtWhisperMessage.Text}");
        RemoveEntry((sender as Button).Content.ToString());
    }

    private void button_RClick(object sender, MouseButtonEventArgs e) {
        Clipboard.SetText($"{(sender as Button).Content}");
    }

    private void btnStopRefreshList_Click(object sender, RoutedEventArgs e) {
        aTimer.Stop();
        _timerRunning = false;
        _guildBinding.IsSearchingForGuildlessPlayers = false;
    }

    private void btnRemoveEntry_Click(object sender, RoutedEventArgs e) {
        if (_timerRunning)
            aTimer.Stop();

        RemoveEntry("");

        if (_timerRunning)
            aTimer.Start();
    }

    private void btnClearList_Click(object sender, RoutedEventArgs e) {
        _guildBinding.UnguildedPlayers.Clear();
        _guildBinding.UnguildedPlayersFame.Clear();
        RefreshList();
    }
}