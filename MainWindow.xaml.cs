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
        // Stałe - typy terenu i podstawowe ustawienia gry
        public const int LAS = 1, LAKA = 2, SKALA = 3;
        private const int RozmiarSegmentu = 32; // Rozmiar pojedynczego kafelka mapy w pikselach
        private const int MaxZycia = 10;       // Maksymalna liczba żyć gracza

        // Zmienne gry - przechowują stan gry
        private int[,] mapa;                   // Dwuwymiarowa tablica reprezentująca mapę
        private int szerokoscMapy = 20;       // Szerokość mapy w kafelkach
        private int wysokoscMapy = 15;         // Wysokość mapy w kafelkach
        private Image[,] tablicaTerenu;       // Tablica obrazków reprezentujących teren
        private BitmapImage[] obrazyTerenu = new BitmapImage[4]; // Obrazy dla różnych typów terenu
        private Image obrazGracza;            // Obrazek reprezentujący gracza
        private int pozycjaGraczaX = 0;        // Aktualna pozycja X gracza na mapie
        private int pozycjaGraczaY = 0;        // Aktualna pozycja Y gracza na mapie
        private int iloscDrewna = 0;           // Zebrane drewno przez gracza
        private int zycia = 5;                 // Aktualna liczba żyć gracza
        private int punkty = 0;                // Punkty zdobyte przez gracza
        private Random generator = new Random(); // Generator liczb losowych
        private int calkowiteDrewno = 0;       // Całkowita ilość drewna na mapie

        // Questy - system zadań w grze
        private List<Quest> aktywneQuesty = new List<Quest>(); // Lista aktywnych zadań
        private int krokiGracza = 0;           // Liczba wykonanych kroków przez gracza
        private int krokiBezStrat = 0;        // Kroki bez utraty życia
        private int poziomGry = 1;             // Aktualny poziom gry
        private int zniszczoneSkaly = 0;       // Liczba zniszczonych skał
        private int uniknieteSkaly = 0;        // Liczba unikniętych skał

        public MainWindow()
        {
            InitializeComponent(); // Inicjalizacja komponentów WPF
            WczytajObrazyTerenu(); // Ładowanie obrazów terenu
            InicjalizujGracza();  // Przygotowanie gracza
            GenerujMape();         // Generowanie losowej mapy
            GenerujQuesty();       // Tworzenie zadań dla gracza
        }

        // Inicjalizacja obrazka gracza
        private void InicjalizujGracza()
        {
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("pack://application:,,,/gracz.png"))
            };
        }

        // Ładowanie obrazów dla różnych typów terenu
        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("pack://application:,,,/las.png"));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("pack://application:,,,/laka.png"));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("pack://application:,,,/skala.png"));
        }

        // Generowanie losowej mapy
        private void GenerujMape()
        {
            mapa = new int[wysokoscMapy, szerokoscMapy];
            int liczbaDrewn = 0; // Licznik drewna na mapie

            // Wypełnianie mapy losowymi terenami
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    int los = generator.Next(100); // Losowa liczba 0-99
                    // 50% szans na łąkę, 40% na las, 10% na skałę
                    mapa[y, x] = los < 50 ? LAKA : (los < 90 ? LAS : SKALA);
                    if (mapa[y, x] == LAS) liczbaDrewn++;
                }
            }
            mapa[0, 0] = LAKA; // Zawsze zaczynamy na łące
            calkowiteDrewno = liczbaDrewn; // Zapisz całkowitą ilość drewna
            RysujMape(); // Narysuj mapę na ekranie
        }

        // Rysowanie mapy w interfejsie
        private void RysujMape()
        {
            SiatkaMapy.Children.Clear(); // Wyczyść obecną mapę
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            // Dodaj wiersze i kolumny do siatki
            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy]; // Inicjalizacja tablicy terenu

            // Wypełnianie siatki obrazkami terenu
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    var obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]] // Wybierz odpowiedni obraz
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz; // Zapisz obraz w tablicy
                }
            }

            // Dodaj gracza na mapę i ustaw go na początkową pozycję
            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1); // Upewnij się, że gracz jest na wierzchu
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();
            AktualizujStatystyki();
        }

        // Aktualizacja pozycji gracza na mapie
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        // Aktualizacja wyświetlanych statystyk
        private void AktualizujStatystyki()
        {
            EtykietaDrewna.Content = $"Drewno: {iloscDrewna} (pozostało: {calkowiteDrewno})";
            EtykietaZycia.Content = $"Życia: {new string('❤', zycia)}";
            EtykietaPunkty.Content = $"Punkty: {punkty}";
            EtykietaPoziom.Content = $"Poziom: {poziomGry}";
        }

        // Klasa reprezentująca zadanie (quest) w grze
        private class Quest
        {
            public string Opis { get; set; }   // Opis zadania
            public int Cel { get; set; }       // Wymagana ilość do ukończenia
            public int Postep { get; set; }    // Aktualny postęp
            public int Nagroda { get; set; }   // Punkty za ukończenie
            public bool Ukonczony { get; set; } // Czy zadanie ukończone
            public string Typ { get; set; }    // Typ zadania (np. "drewno", "kroki")

            public Quest(string opis, int cel, int nagroda, string typ)
            {
                Opis = opis;
                Cel = cel;
                Nagroda = nagroda;
                Typ = typ;
            }
        }

        // Generowanie nowych zadań na podstawie poziomu gry
        private void GenerujQuesty()
        {
            aktywneQuesty.Clear(); // Wyczyść obecne zadania

            // Oblicz wymagania na podstawie poziomu
            int wymaganeDrewno = 5 + (poziomGry * 2);
            int wymaganeKroki = 15 + (poziomGry * 3);
            int wymaganeUnikniecia = 3 + poziomGry;

            // Różne zestawy zadań w zależności od poziomu
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

            // Resetuj liczniki związane z zadaniami
            uniknieteSkaly = 0;
            zniszczoneSkaly = 0;
            krokiBezStrat = 0;
            OdswiezListeQuestow(); // Odśwież listę zadań w UI
        }

        // Aktualizacja listy zadań w interfejsie
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

        // Sprawdzenie, czy któreś zadania zostały ukończone
        private void SprawdzQuesty()
        {
            bool wszystkieUkonczone = true;
            foreach (var quest in aktywneQuesty)
            {
                // Jeśli zadanie ukończone, ale jeszcze nie oznaczono
                if (quest.Postep >= quest.Cel && !quest.Ukonczony)
                {
                    quest.Ukonczony = true;
                    punkty += quest.Nagroda; // Dodaj punkty
                    MessageBox.Show($"Ukończono: {quest.Opis}!\n+{quest.Nagroda} punktów",
                                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                if (!quest.Ukonczony) wszystkieUkonczone = false;
            }

            // Jeśli wszystkie zadania ukończone - przejdź na wyższy poziom
            if (wszystkieUkonczone)
            {
                poziomGry++;
                zycia = Math.Min(zycia + 2, MaxZycia); // Dodaj życia, ale nie więcej niż maksimum
                MessageBox.Show($"Awans na poziom {poziomGry}!\n+2 życia",
                               "Gratulacje!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                GenerujQuesty(); // Wygeneruj nowe zadania
                AktualizujStatystyki();
            }
        }

        // Obsługa klawiatury - poruszanie się gracza i akcje
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX, nowyY = pozycjaGraczaY;

            // Obsługa klawiszy kierunkowych i akcji
            switch (e.Key)
            {
                case Key.Up: nowyY--; break;
                case Key.Down: nowyY++; break;
                case Key.Left: nowyX--; break;
                case Key.Right: nowyX++; break;
                case Key.C: ZbierzDrewno(); return; // Zbierz drewno (C)
                case Key.X: ZniszczSkale(); return; // Zniszcz skałę (X)
                case Key.R: ResetujGre(); return;  // Reset gry (R)
                default: return;                  // Inne klawisze ignoruj
            }

            // Sprawdź, czy nowa pozycja jest w granicach mapy
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                int staryX = pozycjaGraczaX;
                int staryY = pozycjaGraczaY;

                krokiGracza++; // Zwiększ licznik kroków
                // Zaktualizuj postęp w odpowiednich zadaniach
                foreach (var q in aktywneQuesty.Where(q => q.Typ == "kroki" || q.Typ == "przetrwanie" || q.Typ == "bezStrat"))
                    q.Postep++;

                // Sprawdź kolizję ze skałą
                if (mapa[nowyY, nowyX] == SKALA)
                {
                    zycia--; // Odejmij życie
                    krokiBezStrat = 0;
                    // Zresetuj postęp w zadaniach "bez strat"
                    foreach (var q in aktywneQuesty.Where(q => q.Typ == "bezStrat"))
                        q.Postep = 0;

                    // Sprawdź, czy gracz stracił wszystkie życia
                    if (zycia <= 0)
                    {
                        KoniecGry("Straciłeś wszystkie życia!");
                        return;
                    }
                    MessageBox.Show("Uderzyłeś w skałę! -1 życie", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Sprawdź, czy gracz ominął skałę
                    if (CzyMijalSkale(staryX, staryY, nowyX, nowyY))
                    {
                        uniknieteSkaly++;
                        // Zaktualizuj postęp w zadaniach "unikaj skał"
                        foreach (var q in aktywneQuesty.Where(q => q.Typ == "unikajSkal"))
                            q.Postep = uniknieteSkaly;
                    }
                }

                // Zaktualizuj pozycję gracza
                pozycjaGraczaX = nowyX;
                pozycjaGraczaY = nowyY;
                AktualizujPozycjeGracza();
                OdswiezListeQuestow();
                SprawdzQuesty();
            }
        }

        // Sprawdza, czy gracz mijając skałę (używane w zadaniach)
        private bool CzyMijalSkale(int staryX, int staryY, int nowyX, int nowyY)
        {
            // Kierunki do sprawdzenia (prawo, dół, lewo, góra)
            int[][] kierunki = new int[4][] {
                new int[] { 0, 1 }, new int[] { 1, 0 },
                new int[] { 0, -1 }, new int[] { -1, 0 }
            };

            // Sprawdź wszystkie 4 kierunki wokół starej pozycji
            foreach (var kierunek in kierunki)
            {
                int x = staryX + kierunek[0];
                int y = staryY + kierunek[1];

                // Jeśli pozycja jest na mapie i jest tam skała, którą omijamy
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

        // Akcja zbierania drewna (klawisz C)
        private void ZbierzDrewno()
        {
            // Sprawdź, czy gracz stoi na lesie
            if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
            {
                // Zamień las na łąkę
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDrewna++; // Zwiększ licznik drewna
                calkowiteDrewno--; // Zmniejsz całkowitą ilość drewna

                // Zaktualizuj postęp w zadaniach związanych z drewnem
                foreach (var q in aktywneQuesty.Where(q => q.Typ == "drewno"))
                    q.Postep++;

                AktualizujStatystyki();
                OdswiezListeQuestow();
                SprawdzQuesty();

                // Jeśli nie ma już drewna na mapie - koniec gry
                if (calkowiteDrewno <= 0)
                {
                    KoniecGry();
                }
            }
        }

        // Akcja niszczenia skały (klawisz X)
        private void ZniszczSkale()
        {
            // Sprawdź, czy gracz ma odpowiedni poziom i stoi na skale
            if (poziomGry >= 2 && mapa[pozycjaGraczaY, pozycjaGraczaX] == SKALA)
            {
                // Zamień skałę na łąkę
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                zniszczoneSkaly++; // Zwiększ licznik zniszczonych skał
                punkty += 30; // Dodaj punkty

                // Zaktualizuj postęp w zadaniach związanych z niszczeniem skał
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

        // Zakończenie gry (wygrana lub przegrana)
        private void KoniecGry(string powod = "Zebrałeś wszystkie drewno!")
        {
            // Przygotuj podsumowanie gry
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
            ResetujGre(); // Zresetuj grę po zamknięciu okna
        }

        // Resetowanie gry do stanu początkowego
        private void ResetujGre()
        {
            poziomGry = 1;
            GenerujMape();      // Wygeneruj nową mapę
            GenerujQuesty();    // Stwórz nowe zadania
            iloscDrewna = 0;    // Wyzeruj zebrane drewno
            zycia = 5;          // Przywróć domyślną liczbę żyć
            punkty = 0;         // Wyzeruj punkty
            krokiGracza = 0;    // Wyzeruj liczniki
            krokiBezStrat = 0;
            zniszczoneSkaly = 0;
            uniknieteSkaly = 0;
            AktualizujStatystyki(); // Odśwież statystyki
        }

        // Obsługa przycisków w interfejsie
        private void NowaRunda_Click(object sender, RoutedEventArgs e) => ResetujGre();
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Zamknij aplikację
        }
    }
}