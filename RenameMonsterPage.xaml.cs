using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Input;

namespace mini_monster
{
    public partial class RenameMonsterPage : PhoneApplicationPage
    {
        internal static string MONSTER_URL = "monster_url";
        internal static string PORT_INDEX = "port_index";
        internal static string ALLOWED_SYMBOLS = "!;:'\"?/$()-_.*),+[]><}{^~|abcdefghijklmnopqrstuvwxyz1234567890";
        internal static int ALLOWED_LENGTH = 20;

        private Monster actual_monster;

        public RenameMonsterPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string monster_url = string.Empty;
            if (NavigationContext.QueryString.TryGetValue(MONSTER_URL, out monster_url))
            {
                actual_monster = App.MonstersList.First(monster => monster.MonsterUrl.Equals(monster_url));
                MonsterName.Text = actual_monster.MonsterName;
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            ShowProgress();
            string monster_name = MonsterName.Text;
            try
            {
                bool is_success = await MonsterExecutor.RenameMonster(GetMonsterUrl(), monster_name);
                if (is_success)
                {
                    for (int c = 0; c < App.MonstersList.Count; c++)
                    {
                        if (App.MonstersList[c].MonsterUrl.Equals(actual_monster.MonsterUrl))
                        {
                            Monster monster = new Monster(monster_name, actual_monster.MonsterUrl, actual_monster.MonsterPassword);
                            monster.PortsList = actual_monster.PortsList;
                            App.MonstersList[c] = monster;
                            App.SaveMonsters();
                            MainPage.UpdateTiles(monster);
                            NavigationService.GoBack();
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            HideProgress();
            MessageBox.Show("К сожалению, не удалось переименовать порт. Попробуйте ещё разок.", "Ошибка", MessageBoxButton.OK);
        }

        private string GetMonsterUrl()
        {
            return actual_monster.MonsterUrl + actual_monster.MonsterPassword + "/";
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ShowProgress()
        {
            ControlsPanel.Visibility = System.Windows.Visibility.Collapsed;
            WaitPanel.Visibility = System.Windows.Visibility.Visible;
            ApplicationBar.IsVisible = false;
        }

        private void HideProgress()
        {
            ControlsPanel.Visibility = System.Windows.Visibility.Visible;
            WaitPanel.Visibility = System.Windows.Visibility.Collapsed;
            ApplicationBar.IsVisible = true;
        }

        private void MonsterName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = MonsterName.Text;
            string allowed_text = string.Empty;

            foreach (char symbol in text)
            {
                if (ALLOWED_SYMBOLS.Contains(Convert.ToString(symbol).ToLower()))
                {
                    allowed_text += symbol;
                }
            }
            if (allowed_text.Length > ALLOWED_LENGTH)
            {
                allowed_text = allowed_text.Substring(0, ALLOWED_LENGTH);
            }

            if (text.Length != allowed_text.Length)
            {
                int selection_start = MonsterName.SelectionStart - (text.Length - allowed_text.Length);
                MonsterName.Text = allowed_text;
                MonsterName.SelectionStart = selection_start;
            }
        }
    }
}