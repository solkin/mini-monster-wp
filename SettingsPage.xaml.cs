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
    public partial class MonsterProfile : PhoneApplicationPage
    {
        public MonsterProfile()
        {
            InitializeComponent();

            MonstersList.ItemsSource = App.MonstersList;
        }

        public void Add_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddMonsterPage.xaml", UriKind.Relative));
        }

        private void Watch_Click(object sender, RoutedEventArgs e)
        {
            // var selected = (sender as MenuItem).DataContext as MainPageViewModel;
            //Do something
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            string monster_url = (sender as MenuItem).Tag.ToString();
            NavigationService.Navigate(new Uri("/RenameMonsterPage.xaml" +
                "?" + RenameMonsterPage.MONSTER_URL + "=" + monster_url,
                UriKind.Relative));
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            string monster_url = (sender as MenuItem).Tag.ToString();
            MessageBoxResult result = MessageBox.Show("Действительно удалить выбранный модуль?", "Предупреждение", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Monster monster in App.MonstersList)
                {
                    if(monster.MonsterUrl.Equals(monster_url))
                    {
                        App.MonstersList.Remove(monster);
                        RemoveTiles(monster.MonsterUrl);
                        break;
                    }
                }
            }
            if (App.MonstersList.Count == 0)
            {
                NavigationService.Navigate(new Uri("/AddMonsterPage.xaml", UriKind.Relative));
            }
        }

        internal static void RemoveTiles(string monster_url)
        {
            IEnumerable<ShellTile> tilesToRemove = ShellTile.ActiveTiles.Where(
                tile => tile.NavigationUri.ToString().Contains(monster_url));
            foreach (ShellTile tile in tilesToRemove)
            {
                tile.Delete();
            }
        }
    }
}