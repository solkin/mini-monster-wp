using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Tasks;

namespace mini_monster
{
    public partial class AddMonster : PhoneApplicationPage
    {
        public AddMonster()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (App.MonstersList.Count == 0)
            {
                while (NavigationService.BackStack.Any())
                {
                    NavigationService.RemoveBackEntry();
                }
            }
            base.OnBackKeyPress(e);
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            ShowProgress();
            string monster_url = MonsterUrl.Text;
            string monster_password = MonsterPassword.Text;
            if (!monster_url.EndsWith("/"))
            {
                monster_url += "/";
            }
            try
            {
                if (App.MonstersList.Any(monster => monster.MonsterUrl.Equals(monster_url)))
                {
                    HideProgress();
                    MessageBox.Show("Такой модуль уже добавлен!", "Ошибка", MessageBoxButton.OK);
                }
                else
                {
                    string monster_name = await MonsterExecutor.GetMonsterName(monster_url + monster_password);
                    App.AddMonster(monster_url, monster_password, monster_name);
                    NavigationService.GoBack();
                }
            }
            catch (Exception)
            {
                HideProgress();
                MessageBox.Show("К сожалению, не удалось связаться с модулем. Проверьте введённые данные.", "Ошибка", MessageBoxButton.OK);
            }
        }

        public void MonsterPromo_Click(object sender, EventArgs e)
        {
            App.OpenMonsterPromo();
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
    }
}