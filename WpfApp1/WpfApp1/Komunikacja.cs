using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Printing.IndexedProperties;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WpfApp1
{
    internal class Komunikacja {

        private readonly string serverIp = "127.0.0.1";
        private readonly int serverPort = 1234;
        private byte[] bufforOdbieranie = new byte[1024];
        private bool start = true;

        Socket socketKlient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //tworzę socket u siebie, takie fd , mój socket do komunikacji z serwerem

        MainWindow win = null;

        Thread watekOdbierajacy = new Thread(new ParameterizedThreadStart(FunkcjaWatkuOdbierajacego)); //wątek, podaję w argumencie funkcję, którą ma wykonać, kiedy wątek się uruchomi


        public Komunikacja(MainWindow win) {
            Trace.WriteLine("Komunikacja ...");
            this.win= win;
            try {
                IPAddress ip = IPAddress.Parse(serverIp);  //wpisany ADRES SERWERA, gdzie się łączę; przerabiany ze string na obiekt ipaddr
                Trace.WriteLine("Trwa łączenie z serwerem ...");
                socketKlient.Connect(new IPEndPoint(ip, serverPort)); //na deskryptorze wołam CONNECT; argument(tworzę strukturę z IP i PORT gdzie się łączę)
                Trace.WriteLine("Połączono z serwerem!");

            } catch(SocketException ex) {
                Trace.WriteLine("Nie udało się połączyć z serwerem. " + ex.Message);
            }
            watekOdbierajacy.Start(this);
            Trace.WriteLine("Komunikacja. Done.");
        }


        public void stop() {
            Trace.WriteLine("STOP!");
            socketKlient.Disconnect(true);  //rozłączenie od serwera (serwer na to rozłącza drugiego i zamyka sesję)
            start = false;
            watekOdbierajacy.Join(); //czekanie aż wątek Odbierający się zamknie
        }


        public void wyslijStan(int[,] stan) {
            string stanString = "";
            for(int i=0; i<8; i++) { //zamiana macieży stanu gry na string do wysłania
                for(int j=i%2; j<8; j+=2) {
                    if(stan[i,j] == 0) stanString+= "0";
                    else if(stan[i,j] == 1) stanString+= "1"; 
                    else if(stan[i,j] == 2) stanString+= "2"; 
                }
            }
            Wyslij("typ:zmianastanu stan:"+stanString);
        }


        public void wyslijProsbeORozpoczecie() {
            Wyslij("typ:rozpocznij");
        }


        private int AnalizujKomunikat(string komunikat) {  //typ:nowagra stan:11110020200 kierunek:-1
            Trace.WriteLine("AnalizujKomunikat ...");
            Dictionary<string, string> slownik = new Dictionary<string, string>();
            string[] dane = komunikat.Split(" "); //[ typ:nowagra, stan:11110020200, kierunek:-1 ]
            for (int i=0; i<dane.Length; i++) {  //Dodawanie do słownika elementów komunikatu
                string[] kluczWartosc = dane[i].Split(":"); //[ typ, nowagra ]
                slownik[kluczWartosc[0]] = kluczWartosc[1]; //slownik["typ"] = "nowagra"
            }
            if(slownik.ContainsKey("typ")) {
                if(slownik["typ"] == "nowagra") AnalizujKomunikatNowaGra(slownik);
                if(slownik["typ"] == "zmianastanu") AnalizujKomunikatZmianaStanu(slownik);
            }
            Trace.WriteLine("AnalizujKomunikat. Done.");
            return 0;
        }


        private void AnalizujKomunikatNowaGra(Dictionary<string, string> slownik) {
            if(slownik.ContainsKey("kierunek")) {
                win.ustawKierunek(Int32.Parse(slownik["kierunek"]));
            }
            if(slownik.ContainsKey("stan")) {
                win.ustawStan(slownik["stan"]);
            }
        }


       private void AnalizujKomunikatZmianaStanu(Dictionary<string, string> slownik) {
            if(slownik.ContainsKey("stan")) {
                win.ustawStan(slownik["stan"]);
                win.ustawTure();
            }
        }


        private string Odbierz() {
            int rozmiar = socketKlient.Receive(bufforOdbieranie);  //czekanie na odebranie danych; zwraca rozmiar w Byte
            if(rozmiar == 0) return "";
            Trace.WriteLine("Odebrano. ["+rozmiar+"B].");
            string komunikat = Encoding.ASCII.GetString(bufforOdbieranie, 0, rozmiar); //zamiana tablicy Bajtów na string
            return komunikat;
        }


        private void Wyslij(string komunikat) {
            byte[] bufor = Encoding.ASCII.GetBytes(komunikat+"\0"); //zamiana stringa na tablicę Bajtów
            int rozmiar = socketKlient.Send(bufor);  //wysyłanie danych do serwera
            Trace.WriteLine("Wyslano. ["+rozmiar+"B].");
        }


        private static void FunkcjaWatkuOdbierajacego(object obj) {
            Trace.WriteLine("HELLO!");
            Komunikacja komunikacja = (Komunikacja)obj;
            while(komunikacja.start) {
                string komunikat = komunikacja.Odbierz();
                if(komunikat.Length > 0) {
                    komunikacja.AnalizujKomunikat(komunikat);
                } else {
                    komunikacja.start = false;
                }
                Thread.Sleep(10);
            }
            Trace.WriteLine("BYE!");
        }

    }
}
