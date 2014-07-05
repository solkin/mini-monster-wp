using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using mini_monster.Resources;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace mini_monster
{
    public partial class MainPage : PhoneApplicationPage
    {

        // Конструктор
        public MainPage()
        {
            InitializeComponent();

            

            MonstersPivot.ItemsSource = App.MonstersList;

            string json = "{\"type\":\"MM16N\",\"fwv\":\"MM16N v2.1 std_lite b280614\",\"id\":\"Mini-Monster\",\"out\":[5,0,0,0],\"in\":[0,0],\"t\": 22.7,\"wdr\":0}";
            json = json.Replace("\"out\"", "\"outs\"");
            json = json.Replace("\"in\"", "\"ins\"");
            JsonAnswer answer = MonsterExecutor.ReadJson(json);

            MessageBox.Show(Convert.ToString(answer.outs[0]));

            // ContentPanel.ItemsSource = PortsList;

            /*for (int i = 0; i < 5; i++)
            {
                PortsList.Add(new Port("Порт " + i, 1, false));
            }*/

            // Пример кода для локализации ApplicationBar
            // BuildLocalizedApplicationBar();
        }

        public void Settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        public void MonsterPromo_Click(object sender, EventArgs e)
        {
            App.OpenMonsterPromo();
        }

        private async void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggle = (ToggleSwitch) sender;
            int port_index = int.Parse(toggle.Tag.ToString());
            bool port_state;
            if (toggle.IsChecked == true)
            {
                port_state = true;
            }
            else
            {
                port_state = false;
            }

            bool is_ok = await SwitchPort(port_index, port_state);
            if (!is_ok)
            {
                toggle.IsChecked = !port_state;
            }
        }

        public void Refresh_Click(object sender, EventArgs e)
        {
            RefreshPorts();
        }

        public async void Temperature_Click(object sender, EventArgs e)
        {
            try
            {
                ShowProgress("Получение температуры …");
                string temperature = await MonsterExecutor.GetTemperature(GetMonsterUrl());
                ShowMessageBox("Температура", temperature);
            }
            catch (Exception)
            {
                ShowMessageBox("Ошибка", "К сожалению, получить температуру не удалось");
            }
        }

        public void Pwm_Click(object sender, EventArgs e)
        {
            Monster monster = GetSelectedMonster();
            NavigationService.Navigate(new Uri("/PwmPage.xaml" +
                "?" + PwmPage.MONSTER_URL + "=" + monster.MonsterUrl,
                UriKind.Relative));
        }

        private void MonstersPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.MonstersList.Count > 0 && MonstersPivot.SelectedIndex < App.MonstersList.Count)
            {
                Monster monster = GetSelectedMonster();
                if (monster.PortsList.Count == 0)
                {
                    RefreshPorts();
                }
            }
        }

        private void ShowMessageBox(string title, string message)
        {
            HideProgress();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK);
            });
        }

        private void ShowProgress(string text)
        {
            ProgressIndicator progressIndicator = new ProgressIndicator()
            {
                IsVisible = true,
                IsIndeterminate = true,
                Text = text
            };

            SystemTray.SetProgressIndicator(this, progressIndicator);
        }

        private void HideProgress()
        {
            SystemTray.SetProgressIndicator(this, null);
        }

        public async void RefreshPorts()
        {
            try
            {
                ShowProgress("Обновление портов …");
                ObservableCollection<Port> ports = await MonsterExecutor.GetPorts(GetMonsterUrl());
                ApplyPorts(ports);
                HideProgress();
            }
            catch (Exception)
            {
                ShowMessageBox("Ошибка", "К сожалению, получить состояния портов сейчас удалось");
            }
        }

        public async Task<bool> SwitchPort(int port_index, bool port_state)
        {
            try
            {
                ShowProgress("Переключение порта …");
                ObservableCollection<Port> ports = await MonsterExecutor.SwitchPort(GetMonsterUrl(), port_index, port_state);
                ApplyPorts(ports);
                HideProgress();
                return true;
            }
            catch (Exception)
            {
                ShowMessageBox("Ошибка", "К сожалению, переключить порт сейчас удалось");
            }
            return false;
        }

        private void ApplyPorts(ObservableCollection<Port> ports)
        {
            Monster monster = GetSelectedMonster();
            monster.PortsList.Clear();
            foreach (Port port in ports)
            {
                monster.PortsList.Add(port);
                UpdateTileIfExist(monster, port);
            }
            App.SaveMonsters();
        }

        private Monster GetSelectedMonster()
        {
            return App.MonstersList[MonstersPivot.SelectedIndex];
        }

        private string GetMonsterUrl()
        {
            Monster monster = GetSelectedMonster();
            return monster.MonsterUrl + monster.MonsterPassword + "/";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.MonstersList.Count == 0)
            {
                NavigationService.Navigate(new Uri("/AddMonsterPage.xaml", UriKind.Relative));
                return;
            }

            // This is a fucking stub. Pivot collection freezes after items source becames empty.
            Collection<Monster> monsters_fake = new Collection<Monster>();
            for (int i = 0; i < 2; i++)
            {
                Monster monster = new Monster("", "", "");
                monsters_fake.Add(monster);
            }
            MonstersPivot.ItemsSource = monsters_fake;
            // Applying real monsters.
            MonstersPivot.ItemsSource = App.MonstersList;
            // Checking for index is working.
            if(MonstersPivot.SelectedIndex == -1 || MonstersPivot.SelectedIndex >= App.MonstersList.Count)
            {
                MonstersPivot.SelectedIndex = 0;
            }

            if (e.NavigationMode == NavigationMode.New)
            {
                string monster_url = string.Empty;
                string port_index_string = string.Empty;
                int port_index = -1;
                if (NavigationContext.QueryString.TryGetValue("monster", out monster_url) &&
                    NavigationContext.QueryString.TryGetValue("port", out port_index_string) &&
                    int.TryParse(port_index_string, out port_index))
                {
                    NavigationContext.QueryString.Remove("monster");
                    NavigationContext.QueryString.Remove("port");
                    Monster actual_monster = App.MonstersList.First(monster => monster.MonsterUrl.Equals(monster_url));
                    MonstersPivot.SelectedIndex = App.MonstersList.IndexOf(actual_monster);
                    Port actual_port = actual_monster.PortsList.First(port => (port.PortIndex == port_index));

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        AskToSwitchPort(actual_port);
                    });
                }
            }
        }

        private async void AskToSwitchPort(Port actual_port)
        {
            MessageBoxResult result = MessageBox.Show("Переключить порт \"" + actual_port.PortName + "\" в состояние \"" +
                            (actual_port.PortState ? "ВЫКЛ" : "ВКЛ") + "\"?", "Подтверждение", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                await SwitchPort(actual_port.PortIndex, !actual_port.PortState);
            }
        }

        private void CreateTile_Click(object sender, RoutedEventArgs e)
        {
            Monster monster = GetSelectedMonster();
            int port_index = int.Parse((sender as MenuItem).Tag.ToString());
            Port selected_port = monster.PortsList.First(port => (port.PortIndex == port_index));
            Uri navigateUri = GetPortUri(monster, selected_port);
            ShellTile tile = FindTile(navigateUri);
            if (tile == null)
            {
                IconicTileData tileData = GetTileData(monster, selected_port);
                ShellTile.Create(navigateUri, tileData, true);
            }
            else
            {
                MessageBox.Show("Такая плитка уже вынесена на рабочий стол!", "Ошибка", MessageBoxButton.OK);
            }
        }

        private void RenamePort_Click(object sender, RoutedEventArgs e)
        {
            Monster monster = GetSelectedMonster();
            int port_index = int.Parse((sender as MenuItem).Tag.ToString());
            Port selected_port = monster.PortsList.First(port => (port.PortIndex == port_index));
            if (selected_port.PortImmutable)
            {
                MessageBox.Show("К сожалению, Ваш модуль не поддерживает переименовывание портов. Попробуйте вынести " + 
                    "плиточку на рабочий стол и расположить на удобном месте :)", "Предупреждение", MessageBoxButton.OK);
            }
            else
            {
                NavigationService.Navigate(new Uri("/RenamePortPage.xaml" +
                    "?" + RenamePortPage.MONSTER_URL + "=" + monster.MonsterUrl +
                    "&" + RenamePortPage.PORT_INDEX + "=" + selected_port.PortIndex,
                    UriKind.Relative));
            }
        }

        internal static void UpdateTileIfExist(Monster monster, Port port)
        {
            Uri uri = GetPortUri(monster, port);
            ShellTile tile = FindTile(uri);
            if (tile != null)
            {
                tile.Update(GetTileData(monster, port));
            }
        }

        internal static void UpdateTiles(Monster monster)
        {
            foreach (Port port in monster.PortsList)
            {
                UpdateTileIfExist(monster, port);
            }
        }

        internal static IconicTileData GetTileData(Monster monster, Port port)
        {
            Uri SwitcherLargeImage = new Uri("/Assets/Tiles/" + (port.PortState ? "IconicTileMediumLargeOn.png" : "IconicTileMediumLargeOff.png"), UriKind.Relative);
            Uri SwitcherSmallImage = new Uri("/Assets/Tiles/" + (port.PortState ? "IconicTileSmallOn.png" : "IconicTileSmallOff.png"), UriKind.Relative);
            return new IconicTileData
            {
                Title = port.PortName,
                WideContent1 = monster.MonsterName,
                IconImage = SwitcherLargeImage,
                SmallIconImage = SwitcherSmallImage,
            };
        }

        internal static Uri GetPortUri(Monster monster, Port port)
        {
            return GetPortUri(monster.MonsterUrl, port.PortIndex);
        }

        internal static Uri GetPortUri(String monster_url, int port_index)
        {
            return new Uri("/MainPage.xaml" +
                "?monster=" + Uri.EscapeUriString(monster_url) +
                "&port=" + port_index,
                UriKind.Relative);
        }

        internal static ShellTile FindTile(Uri uri)
        {
            ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(
                tile => tile.NavigationUri.Equals(uri));
            return shellTile;
        }

        // Пример кода для построения локализованной панели ApplicationBar
        /*private void BuildLocalizedApplicationBar()
        {
            // Установка в качестве ApplicationBar страницы нового экземпляра ApplicationBar.
            ApplicationBar = new ApplicationBar();

            // Создание новой кнопки и установка текстового значения равным локализованной строке из AppResources.
            ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonText;
            ApplicationBar.Buttons.Add(appBarButton);

            // Создание нового пункта меню с локализованной строкой из AppResources.
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
            ApplicationBar.MenuItems.Add(appBarMenuItem);
        }*/
    }
}