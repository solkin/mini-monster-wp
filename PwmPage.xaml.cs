using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace mini_monster
{
    public partial class PwmPage : PhoneApplicationPage
    {
        internal static string MONSTER_URL = "monster_url";

        private Monster actual_monster;

        public PwmPage()
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
                RequestInitialValue();
            }
        }

        private async void PwmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsProgress())
            {
                ShowTrayProgress("Установка значения...");
                int value = (int) e.NewValue;
                bool is_ok = await MonsterExecutor.UpdatePWM(GetMonsterUrl(), value);
                if (is_ok)
                {
                    HideTrayProgress();
                }
                else
                {
                    ShowMessageBox("Ошибка", "Не удалось установить установить значение на модуле!");
                }
            }
        }

        private async void RequestInitialValue()
        {
            ShowProgress();
            try
            {
                int value = await MonsterExecutor.GetPWMValue(GetMonsterUrl());
                PwmSlider.Value = value;
                HideProgress();
            }
            catch (Exception)
            {
                MessageBox.Show("К сожалению, сейчас не удалось установить связь с модулем, либо он не поддерживает ШИМ.", "Ошибка", MessageBoxButton.OK);
                NavigationService.GoBack();
            }
        }

        private string GetMonsterUrl()
        {
            return actual_monster.MonsterUrl + actual_monster.MonsterPassword + "/";
        }

        private bool IsProgress()
        {
            return ControlsPanel.Visibility == System.Windows.Visibility.Collapsed;
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

        private void ShowMessageBox(string title, string message)
        {
            HideTrayProgress();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK);
            });
        }

        private void ShowTrayProgress(string text)
        {
            ProgressIndicator progressIndicator = new ProgressIndicator()
            {
                IsVisible = true,
                IsIndeterminate = true,
                Text = text
            };

            SystemTray.SetProgressIndicator(this, progressIndicator);
        }

        private void HideTrayProgress()
        {
            SystemTray.SetProgressIndicator(this, null);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}