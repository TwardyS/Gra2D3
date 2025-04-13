using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // STAŁE DOTYCZĄCE TERENÓW
        public const int LAS = 1;     // Wartość reprezentująca las na mapie
        public const int LAKA = 2;    // Wartość reprezentująca łąkę na mapie
        public const int SKALA = 3;   // Wartość reprezentująca skałę na mapie
        public const int ILE_TERENOW = 4; // Ilość różnych typów terenu

        // ZMIENNE DOTYCZĄCE MAPY
        private int[,] mapa;          // Dwuwymiarowa tablica przechowująca typy terenu
        private int szerokoscMapy = 20; // Szerokość mapy w segmentach
        private int wysokoscMapy = 15;  // Wysokość mapy w segmentach
        private Image[,] tablicaTerenu; // Tablica przechowująca kontrolki obrazów terenu

        // USTAWIENIA GRAFICZNE
        private const int RozmiarSegmentu = 32; // Rozmiar pojedynczego kafelka mapy w pikselach
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW]; // Tablica obrazów terenu
        private Image obrazGracza;    // Kontrolka przechowująca obraz gracza

        // STAN GRACZA
        private int pozycjaGraczaX = 0; // Aktualna pozycja X gracza na mapie
        private int pozycjaGraczaY = 0; // Aktualna pozycja Y gracza na mapie
        private int iloscDrewna = 0;  // Licznik zebranego drewna
        private int zycia = 5;        // Liczba pozostałych żyć gracza
        private Random generator = new Random(); // Generator liczb losowych

        public MainWindow()
        {
            InitializeComponent();

            // Inicjalizacja obrazów terenu
            WczytajObrazyTerenu();

            // Przygotowanie obrazu gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("pack://application:,,,/gracz.png"))
            };

            // Generowanie pierwszej mapy
            GenerujMape();
        }

        /// <summary>
        /// Ładuje obrazy terenu z zasobów aplikacji
        /// </summary>
        private void WczytajObrazyTerenu()
        {
            // Ładowanie obrazów z folderu Resources
            obrazyTerenu[LAS] = new BitmapImage(new Uri("pack://application:,,,/las.png"));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("pack://application:,,,/laka.png"));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("pack://application:,,,/skala.png"));
        }

        /// <summary>
        /// Generuje nową losową mapę gry
        /// </summary>
        private void GenerujMape()
        {
            // Inicjalizacja tablicy mapy
            mapa = new int[wysokoscMapy, szerokoscMapy];

            // Wypełnianie mapy losowymi terenami
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    // Rozkład terenów: 50% łąka, 30% las, 20% skała
                    int los = generator.Next(100);
                    if (los < 50) mapa[y, x] = LAKA;
                    else if (los < 80) mapa[y, x] = LAS;
                    else mapa[y, x] = SKALA;
                }
            }

            // Gwarancja, że startowe pole (0,0) jest dostępne (łąka)
            mapa[0, 0] = LAKA;

            // Narysowanie nowej mapy
            RysujMape();
        }

        /// <summary>
        /// Rysuje aktualny stan mapy w interfejsie
        /// </summary>
        private void RysujMape()
        {
            // Czyszczenie poprzedniej mapy
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            // Tworzenie wierszy i kolumn siatki
            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            // Inicjalizacja tablicy obrazów terenu
            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];

            // Rysowanie każdego segmentu mapy
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    // Tworzenie nowego obrazu terenu
                    var obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]] // Ustawienie odpowiedniego obrazu
                    };

                    // Ustawienie pozycji obrazu w siatce
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);

                    // Dodanie obrazu do siatki
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            // Dodanie gracza na mapę (na wierzchu innych elementów)
            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1);

            // Reset pozycji gracza
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();

            // Reset statystyk
            iloscDrewna = 0;
            zycia = 5;
            AktualizujStatystyki();
        }

        /// <summary>
        /// Aktualizuje pozycję gracza na mapie
        /// </summary>
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        /// <summary>
        /// Aktualizuje wyświetlane statystyki gry
        /// </summary>
        private void AktualizujStatystyki()
        {
            EtykietaDrewna.Content = $"Drewno: {iloscDrewna}";
            EtykietaZycia.Content = $"Życia: {new string('❤', zycia)}";
        }

        /// <summary>
        /// Obsługa zdarzenia naciśnięcia klawisza
        /// </summary>
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            // Tymczasowe przechowanie nowej pozycji
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            // Obsługa klawiszy ruchu
            switch (e.Key)
            {
                case Key.Up: nowyY--; break;
                case Key.Down: nowyY++; break;
                case Key.Left: nowyX--; break;
                case Key.Right: nowyX++; break;
                case Key.C: ZbierzDrewno(); return; // Zbieranie drewna
                case Key.R: GenerujMape(); return;  // Nowa runda
                default: return;
            }

            // Sprawdzenie czy nowa pozycja jest w granicach mapy
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                // Sprawdzenie czy nowa pozycja to skała
                if (mapa[nowyY, nowyX] == SKALA)
                {
                    zycia--; // Utrata życia
                    AktualizujStatystyki();

                    if (zycia <= 0)
                    {
                        MessageBox.Show("Koniec gry! Straciłeś wszystkie życia.");
                        GenerujMape(); // Restart gry
                    }
                    else
                    {
                        MessageBox.Show("Ouch! Uderzyłeś w skałę. -1 życie");
                    }
                }
                else
                {
                    // Aktualizacja pozycji gracza
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }
        }

        /// <summary>
        /// Zbieranie drewna z lasu
        /// </summary>
        private void ZbierzDrewno()
        {
            // Sprawdzenie czy gracz stoi na lesie
            if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
            {
                // Zamiana lasu na łąkę
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];

                // Losowa ilość zebranego drewna (1-3)
                iloscDrewna += generator.Next(1, 4);
                AktualizujStatystyki();
            }
        }

        /// <summary>
        /// Obsługa przycisku nowej rundy
        /// </summary>
        private void NowaRunda_Click(object sender, RoutedEventArgs e)
        {
            GenerujMape();
        }
    }
}