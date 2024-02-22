using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Printing;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp1
{

    class Pozycja {
        public int x;
        public int y;
    };


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private Komunikacja? komunikacja = null;

        private readonly int[,] stanGry = new int[8,8];
        private Pozycja? zaznaczonaPozycja = null;
        private Brush kolorBialy;
        private Brush kolorCzarny;

        private int kierunek = 1;
        private int id_gracz = 1;
        private int id_przeciwnik = 2;
        int tura = 1;

        public MainWindow() {
            InitializeComponent();
            komunikacja = new Komunikacja(this); //obiekt służący do komunikacji z serwerem
            for(int i=0; i<8; i++) {
                for(int j=0; j<8; j++) {
                    stanGry[i,j] = 0;
                }
            }
            List<Button> listaPol = plansza.Children.Cast<Button>().ToList();
            int id = 0;
            listaPol.ForEach(button => {;
                Pozycja pos = new Pozycja();
                pos.y = id/8;
                pos.x = id%8 + (pos.y%2);
                button.Tag= pos;
                id += 2;
            });
            kolorBialy = new SolidColorBrush(Color.FromRgb((byte)255, (byte)255, (byte)255));
            kolorCzarny = new SolidColorBrush(Color.FromRgb((byte)0, (byte)0, (byte)0));
            OdswiezUI();
        }


        private void OdswiezUI() {
            List<Button> listaPol = plansza.Children.Cast<Button>().ToList();
            listaPol.ForEach(button => {
                Pozycja pos = (Pozycja)button.Tag;
                if(stanGry[pos.y, pos.x] == 0) {
                    button.Content = "";
                } else
                if(stanGry[pos.y, pos.x] == 1) {
                    if(button.Tag == zaznaczonaPozycja) {
                        button.Content = "😸";  
                    } else {
                        button.Content = "😺";
                    }
                    button.Foreground = kolorBialy;
                } else
                if(stanGry[pos.y, pos.x] == 2) {
                    if(button.Tag == zaznaczonaPozycja) {
                        button.Content = "😸";
                    } else {
                        button.Content = "😺";
                    }
                    button.Foreground = kolorCzarny;
                }
            });
        }


        private bool Ruch(Pozycja pozycja) {
            if(zaznaczonaPozycja == null) return false;
            if(stanGry[pozycja.y, pozycja.x] == id_gracz) return false;
            if(stanGry[pozycja.y, pozycja.x] == id_przeciwnik) return false;
            if(pozycja.y == zaznaczonaPozycja.y+kierunek && (pozycja.x == zaznaczonaPozycja.x-1 || pozycja.x == zaznaczonaPozycja.x+1)) {
                stanGry[zaznaczonaPozycja.y, zaznaczonaPozycja.x] = 0;
                stanGry[pozycja.y, pozycja.x] = id_gracz;
                zaznaczonaPozycja = null;
                return true;
            }
            return false;
        }


        private bool Odznaczenie(Pozycja pozycja) {
            if(zaznaczonaPozycja == null) return false;
            if(pozycja.y == zaznaczonaPozycja.y && pozycja.x == zaznaczonaPozycja.x) {
                zaznaczonaPozycja = null;
                return true;
            }
            return false;
        }


        private bool Bicie(Pozycja pozycja) {
            if(zaznaczonaPozycja == null) return false;
            if(stanGry[pozycja.y, pozycja.x] == id_gracz) return false;
            if(stanGry[pozycja.y, pozycja.x] == id_przeciwnik) return false;
            int x = zaznaczonaPozycja.x - pozycja.x;
            int y = zaznaczonaPozycja.y - pozycja.y;
            if(Math.Abs(x) == 2 && Math.Abs(y) == 2) {
                x = zaznaczonaPozycja.x - x/2;
                y = zaznaczonaPozycja.y - y/2;
                if(stanGry[y, x] == id_przeciwnik) {
                    stanGry[y, x] = 0;
                    stanGry[zaznaczonaPozycja.y, zaznaczonaPozycja.x] = 0;
                    stanGry[pozycja.y, pozycja.x] = id_gracz;
                    zaznaczonaPozycja = null;
                    return true;
                }
            }
            return false;
        }
        

        private void Akcja(object sender, RoutedEventArgs e) {
            if(tura != id_gracz) return;
            Button button = (Button)sender;
            Pozycja pozycja = (Pozycja)button.Tag;
            //WSKAZYWANIE ŹRÓDŁOWEGO KAFELKA
            if(zaznaczonaPozycja == null) {
                if(stanGry[pozycja.y, pozycja.x] == id_gracz) {
                    zaznaczonaPozycja = pozycja;
                }
            }
            //WSKAZYWANIE DOCELOWEGO KAFELKA
            else {
                bool zrobione = false;
                if(!zrobione) zrobione = Ruch(pozycja);
                if(!zrobione) zrobione = Bicie(pozycja);
                if(zrobione) {
                    oddajTure();
                    komunikacja.wyslijStan(stanGry);
                }
                if(!zrobione) zrobione = Odznaczenie(pozycja);
            }
            OdswiezUI();
        }


        private void ButtonRozpocznij_Click(object sender, RoutedEventArgs e) {
            komunikacja.wyslijProsbeORozpoczecie();
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            komunikacja.stop();
        }


        public void ustawKierunek(int kierunek) {
            this.kierunek = kierunek;
            if(kierunek == 1) {
                id_gracz = 1;
                id_przeciwnik = 2;
                tura = id_gracz;
            } 
            else {
                id_gracz = 2;
                id_przeciwnik = 1;
                tura = id_przeciwnik;
            }
            Application.Current.Dispatcher.Invoke(() => {
                if(id_gracz == 1) {
                    this.label.Content = "Biały kot";
                    this.labelTura.Content = "Twoja tura";
                } else
                if(id_gracz == 2) {
                    this.label.Content = "Czarny kot";
                    this.labelTura.Content = "Tura przeciwnika";
                }
            });
        }


        public void ustawStan(string stanString) {
            Application.Current.Dispatcher.Invoke(() => {
                int k = 0;
                for(int i=0; i<8; i++) {
                    for(int j=i%2; j<8; j+=2) {
                        if(stanString[k] == '0') {stanGry[i, j] = 0;}
                        else if(stanString[k] == '1') {stanGry[i, j] = 1;}
                        else if(stanString[k] == '2') {stanGry[i, j] = 2;}
                        k++;
                    }
                }
                OdswiezUI();
            });
        }


        public void ustawTure() {
            Application.Current.Dispatcher.Invoke(() => {
                tura = id_gracz;
                this.labelTura.Content = "Twoja tura";
            });
        }


        public void oddajTure() {
            Trace.WriteLine("ODDAJ TURE! "+tura+" "+id_gracz);
            if(tura == id_gracz) {
                tura = id_przeciwnik;
            }
            this.labelTura.Content = "Tura przeciwnika";
        }
    }
}