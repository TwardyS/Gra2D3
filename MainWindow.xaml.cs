using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe
        public const int LAS = 1, LAKA = 2, SKALA = 3;
        private const int RozmiarSegmentu = 32;
        private const int MaxZycia = 10;

        // Zmienne gry
        private int[,] mapa;
        private int szerokoscMapy = 20, wysokoscMapy = 15;
        private Image[,] tablicaTerenu;
        private BitmapImage[] obrazyTerenu = new BitmapImage[4];
        private Image obrazGracza;
        private int pozycjaGraczaX = 0, pozycjaGraczaY = 0;
        private int iloscDrewna = 0, zycia = 5, punkty = 0;
        private Random generator = new Random();
        private int calkowiteDrewno = 0;

        // Questy
        private List<Quest> aktywneQuesty = new List<Quest>();
        private int krokiGracza = 0;
        private int krokiBezStrat = 0;
        private int poziomGry = 1;
        private int zniszczoneSkaly = 0;
        private int uniknieteSkaly = 0;

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();
            InicjalizujGracza();
            GenerujMape();
            GenerujQuesty();
        }

        private void InicjalizujGracza()
        {
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("pack://application:,,,/gracz.png"))
            };
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("pack://application:,,,/las.png"));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("pack://application:,,,/laka.png"));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("pack://application:,,,/skala.png"));
        }

        private void GenerujMape()
        {
            mapa = new int[wysokoscMapy, szerokoscMapy];
            int liczbaDrewn = 0;

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    int los = generator.Next(100);
                    mapa[y, x] = los < 50 ? LAKA : (los < 90 ? LAS : SKALA);
                    if (mapa[y, x] == LAS) liczbaDrewn++;
                }
            }
            mapa[0, 0] = LAKA;
            calkowiteDrewno = liczbaDrewn;
            RysujMape();
        }

        private void RysujMape()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    var obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]]
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1);
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();
            AktualizujStatystyki();
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void AktualizujStatystyki()
        {
            EtykietaDrewna.Content = $"Drewno: {iloscDrewna} (pozostało: {calkowiteDrewno})";
            EtykietaZycia.Content = $"Życia: {new string('❤', zycia)}";
            EtykietaPunkty.Content = $"Punkty: {punkty}";
            EtykietaPoziom.Content = $"Poziom: {poziomGry}";
        }

        private class Quest
        {
            public string Opis { get; set; }
            public int Cel { get; set; }
            public int Postep { get; set; }
            public int Nagroda { get; set; }
            public bool Ukonczony { get; set; }
            public string Typ { get; set; }

            public Quest(string opis, int cel, int nagroda, string typ)
            {
                Opis = opis;
                Cel = cel;
                Nagroda = nagroda;
                Typ = typ;
            }
        }

        private void GenerujQuesty()
        {
            aktywneQuesty.Clear();

            int wymaganeDrewno = 5 + (poziomGry * 2);
            int wymaganeKroki = 15 + (poziomGry * 3);
            int wymaganeUnikniecia = 3 + poziomGry;

            switch (poziomGry % 3)
            {
                case 0:
                    aktywneQuesty.Add(new Quest($"Zbierz {wymaganeDrewno} drewna", wymaganeDrewno, 100, "drewno"));
                    aktywneQuesty.Add(new Quest($"Przeżyj {wymaganeKroki} kroków", wymaganeKroki, 50, "przetrwanie"));
                    aktywneQuesty.Add(new Quest($"Omijaj {wymaganeUnikniecia} skały", wymaganeUnikniecia, 80, "unikajSkal"));
                    break;
                case 1:
                    aktywneQuesty.Add(new Quest($"Zbierz {wymaganeDrewno + 3} drewna", wymaganeDrewno + 3, 120, "drewno"));
                    aktywneQuesty.Add(new Quest($"Zrób {wymaganeKroki} kroków", wymaganeKroki, 60, "kroki"));
                    aktywneQuesty.Add(new Quest($"Nie trać życia przez {15} kroków", 15, 90, "bezStrat"));
                    break;
                case 2:
                    aktywneQuesty.Add(new Quest($"Zbierz {wymaganeDrewno} drewna", wymaganeDrewno, 150, "drewno"));
                    if (poziomGry >= 2)
                    {
                        aktywneQuesty.Add(new Quest($"Zniszcz {poziomGry + 1} skały (X)", poziomGry + 1, 100, "zniszczSkaly"));
                    }
                    aktywneQuesty.Add(new Quest($"Omijaj {wymaganeUnikniecia} skały", wymaganeUnikniecia, 70, "unikajSkal"));
                    break;
            }

            uniknieteSkaly = 0;
            zniszczoneSkaly = 0;
            krokiBezStrat = 0;
            OdswiezListeQuestow();
        }

        private void OdswiezListeQuestow()
        {
            ListaQuestow.Items.Clear();
            foreach (var quest in aktywneQuesty)
            {
                var item = new ListBoxItem
                {
                    Content = $"{quest.Opis}: {quest.Postep}/{quest.Cel} {(quest.Ukonczony ? "✓" : "")}",
                    Foreground = quest.Ukonczony ? Brushes.Green : Brushes.White,
                    FontWeight = FontWeights.Bold
                };
                ListaQuestow.Items.Add(item);
            }
        }

        private void SprawdzQuesty()
        {
            bool wszystkieUkonczone = true;
            foreach (var quest in aktywneQuesty)
            {
                if (quest.Postep >= quest.Cel && !quest.Ukonczony)
                {
                    quest.Ukonczony = true;
                    punkty += quest.Nagroda;
                    MessageBox.Show($"Ukończono: {quest.Opis}!\n+{quest.Nagroda} punktów",
                                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                if (!quest.Ukonczony) wszystkieUkonczone = false;
            }

            if (wszystkieUkonczone)
            {
                poziomGry++;
                zycia = Math.Min(zycia + 2, MaxZycia);
                MessageBox.Show($"Awans na poziom {poziomGry}!\n+2 życia",
                               "Gratulacje!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                GenerujQuesty();
                AktualizujStatystyki();
            }
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX, nowyY = pozycjaGraczaY;

            switch (e.Key)
            {
                case Key.Up: nowyY--; break;
                case Key.Down: nowyY++; break;
                case Key.Left: nowyX--; break;
                case Key.Right: nowyX++; break;
                case Key.C: ZbierzDrewno(); return;
                case Key.X: ZniszczSkale(); return;
                case Key.R: ResetujGre(); return;
                default: return;
            }

            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                int staryX = pozycjaGraczaX;
                int staryY = pozycjaGraczaY;

                krokiGracza++;
                foreach (var q in aktywneQuesty.Where(q => q.Typ == "kroki" || q.Typ == "przetrwanie" || q.Typ == "bezStrat"))
                    q.Postep++;

                if (mapa[nowyY, nowyX] == SKALA)
                {
                    zycia--;
                    krokiBezStrat = 0;
                    foreach (var q in aktywneQuesty.Where(q => q.Typ == "bezStrat"))
                        q.Postep = 0;

                    if (zycia <= 0)
                    {
                        KoniecGry("Straciłeś wszystkie życia!");
                        return;
                    }
                    MessageBox.Show("Uderzyłeś w skałę! -1 życie", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    if (CzyMijalSkale(staryX, staryY, nowyX, nowyY))
                    {
                        uniknieteSkaly++;
                        foreach (var q in aktywneQuesty.Where(q => q.Typ == "unikajSkal"))
                            q.Postep = uniknieteSkaly;
                    }
                }

                pozycjaGraczaX = nowyX;
                pozycjaGraczaY = nowyY;
                AktualizujPozycjeGracza();
                OdswiezListeQuestow();
                SprawdzQuesty();
            }
        }

        private bool CzyMijalSkale(int staryX, int staryY, int nowyX, int nowyY)
        {
            int[][] kierunki = new int[4][] {
                new int[] { 0, 1 }, new int[] { 1, 0 },
                new int[] { 0, -1 }, new int[] { -1, 0 }
            };

            foreach (var kierunek in kierunki)
            {
                int x = staryX + kierunek[0];
                int y = staryY + kierunek[1];

                if (x >= 0 && x < szerokoscMapy && y >= 0 && y < wysokoscMapy)
                {
                    if (mapa[y, x] == SKALA && !(x == nowyX && y == nowyY))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void ZbierzDrewno()
        {
            if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDrewna++;
                calkowiteDrewno--;

                foreach (var q in aktywneQuesty.Where(q => q.Typ == "drewno"))
                    q.Postep++;

                AktualizujStatystyki();
                OdswiezListeQuestow();
                SprawdzQuesty();

                if (calkowiteDrewno <= 0)
                {
                    KoniecGry();
                }
            }
        }

        private void ZniszczSkale()
        {
            if (poziomGry >= 2 && mapa[pozycjaGraczaY, pozycjaGraczaX] == SKALA)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                zniszczoneSkaly++;
                punkty += 30;

                foreach (var q in aktywneQuesty.Where(q => q.Typ == "zniszczSkaly"))
                    q.Postep = zniszczoneSkaly;

                AktualizujStatystyki();
                OdswiezListeQuestow();
                SprawdzQuesty();
            }
            else if (poziomGry < 2)
            {
                MessageBox.Show("Niszczenie skał odblokujesz od poziomu 2!", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void KoniecGry(string powod = "Zebrałeś wszystkie drewno!")
        {
            string podsumowanie = $"""
            GRATULACJE! {powod}
            ----------------------------
            Zdobyte punkty: {punkty}
            Ukończone poziomy: {poziomGry}
            Zebrane drewno: {iloscDrewna}
            Pozostałe życia: {zycia}
            ----------------------------
            Naciśnij OK, aby zagrać ponownie
            """;

            MessageBox.Show(podsumowanie, "Koniec gry - Zwycięstwo!", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetujGre();
        }

        private void ResetujGre()
        {
            poziomGry = 1;
            GenerujMape();
            GenerujQuesty();
            iloscDrewna = 0;
            zycia = 5;
            punkty = 0;
            krokiGracza = 0;
            krokiBezStrat = 0;
            zniszczoneSkaly = 0;
            uniknieteSkaly = 0;
            AktualizujStatystyki();
        }

        private void NowaRunda_Click(object sender, RoutedEventArgs e) => ResetujGre();
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}