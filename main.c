
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/select.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>

#define LIMIT 100 //ile klientów może być połączonych do serwera

struct Sesja {  //dwa deskryptory w jednej strukturze połączone w sesję, sparowane do komunikacji z klientami 
    int cfd1;
    int cfd2;
};

const int serverPort = 1234;
int sfd = 0;   //deskryptor do nasłuchiwania
int lista_cfd[LIMIT];  //lista deskryptorów połączonych z serwerem klientów
struct Sesja lista_sesji[LIMIT]; //lista utworzyonych sesji


int wyslij(int fd, const char* komunikat) { //do kogo i string wysyłany
    const int rozmiar = write(fd, komunikat, strlen(komunikat)+1);
    printf("Wysłano komunikat %s do [cfd: %d] [%dB]\n", komunikat, fd, rozmiar);
    return 0;
}


int stworzSesje(int cfd)  {
    int i = 0;
    for(i=0; i<LIMIT; i++) {
        if(lista_sesji[i].cfd1 == -1 && lista_sesji[i].cfd2 == -1) { //szuka wolnej sesji 
            lista_sesji[i].cfd1 = cfd;                               //i jak znajdzie, to siebie wpisuje zamiast -1
            break;
        }
    }
    if(i < LIMIT) {
        printf("Stworzono sesję [numer_sesji: %d].\n", i);
    }
    return i;
}


int znajdzSesje(int cfd) {
    for(int i=0; i<LIMIT; i++) {
        if(lista_sesji[i].cfd1 == cfd || lista_sesji[i].cfd2 == cfd) {
            return i;
        }
    }
    return -1;
}


int zamknijSesje(int cfd) {
    int numer_sesji = znajdzSesje(cfd);
    if(numer_sesji >= 0) {
        close(lista_sesji[numer_sesji].cfd1);
        close(lista_sesji[numer_sesji].cfd2); //klient w funkcji Odbierz() , do socketKlient.Receive dostaje 0
        for(int i=0; i<LIMIT; i++) {
            if(lista_cfd[i] == lista_sesji[numer_sesji].cfd1) lista_cfd[i]= 0;
            if(lista_cfd[i] == lista_sesji[numer_sesji].cfd2) lista_cfd[i]= 0;
        }
        lista_sesji[numer_sesji].cfd1 = -1;
        lista_sesji[numer_sesji].cfd2 = -1;
        printf("Zamknieto sesje [numer_sesji: %d].\n", numer_sesji);
    }
    return 0;
}


int znajdzOdbiorceWSesji(int cfd) {
    for(int i=0; i<LIMIT; i++) {
        if(lista_sesji[i].cfd1 == cfd || lista_sesji[i].cfd2 == cfd) {
            if(lista_sesji[i].cfd1 == cfd) return lista_sesji[i].cfd2;
            else return lista_sesji[i].cfd1;
        }
    }
    return -1;
}


int rozpocznijGre(int numer_sesji) {
    wyslij(lista_sesji[numer_sesji].cfd1, "typ:nowagra kierunek:1 stan:11111111111100000000222222222222"); //(deskryptor klienta gdzie wysyła i komunikat)
    wyslij(lista_sesji[numer_sesji].cfd2, "typ:nowagra kierunek:-1 stan:11111111111100000000222222222222");
    return 0;
}


int analizujKomunikat(const char* komunikat, int cfd) {
    if(strstr(komunikat, "typ:rozpocznij")) {  //spr czy w komunikacie znajduje się typ:rozpocznij
        int i = 0;
        for(i=0; i<LIMIT; i++) { //dobieranie do pary
            if(lista_sesji[i].cfd1 != -1 && lista_sesji[i].cfd2 == -1) break; //w "i" zostaje index sesji, gdzie się połączę
        }
        if(i == LIMIT) {  //nie ma z kim sparować, to tworzy nową sesję (by potem do niego ktoś dołączył)
            printf("Nie znaleziono przeciwnika. Tworzenie nowej sesji.\n");
            stworzSesje(cfd);
        } else {   //ma z kim się połączyć
            printf("Zanleziono przeciwnika [numer_sesji: %d][cfd: %d].\n", i,  lista_sesji[i].cfd1);
            lista_sesji[i].cfd2= cfd; //wrzucam deskryptor jako przeciwnika
            rozpocznijGre(i); //przekazuję index sesji w której jest
        }
    } else
    if(strstr(komunikat, "typ:zmianastanu")) {
        int cfd_odbiorca = znajdzOdbiorceWSesji(cfd); //szukanie socketa z którym gram
        wyslij(cfd_odbiorca, komunikat);  //wysyła to co odebrał od jednego gracza, do drugiego gracza
    }
    return 0;
}


int main() {
    printf("HELLO!\n");

    for(int i=0; i<LIMIT; i++) {  //ustawienie w całej liście sesji deskryptory na -1 (niczyja sesja)
        lista_sesji[i].cfd1 = -1;
        lista_sesji[i].cfd2 = -1;
    }
    memset(lista_cfd, 0, sizeof(lista_cfd)); //zerowanie listy z wszystkimi deskryptorami


    int err= 0;
    socklen_t sl; 
    int fdmax = 0;//, fda = 0;
    int ret = 0; //liczba deskryptorów, które zwrócił SELECT (czyli które coś chciały zrobić)
    struct sockaddr_in saddr, caddr;
    struct timeval timeout; //struktura przechowująca czas: sekundy i mikrosekundy
    fd_set rmask, wmask; //lista/tablica/struktura z socketami: rmask-sockety czytające; wmask-sockety piszące
    
    sfd = socket(AF_INET, SOCK_STREAM, 0); //serwer otwiera deskryptor, tu bedzie potem nasluchiwal
    if(sfd == 0) {
        printf("Blad. Nie mozna utworzyc gniazda.\n");
        return 1;
    }

    int opt = 1;
    err= setsockopt(sfd, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt)); //by się socket mógł wznowić na tym samym porcie (by nie blokował portu jakiś czas po zamknięciu)
    if(err != 0) {
        printf("Blad. setsockopt.\n");
        return 1;
    }

    //dane serwera, tu gdzie sie klienci lacza
    saddr.sin_family = AF_INET;
    saddr.sin_port = htons(serverPort);
    saddr.sin_addr.s_addr = INADDR_ANY;

    //socket o deskryptorze sfd bedzie otwarty na tym adresie IP i tym porcie ze struktury saddr
    err = bind(sfd, (struct sockaddr*)&saddr, sizeof(saddr));
    err = listen(sfd, 10); //nasluchiwanie na tym deskryptorze sfd serwera, czy przychodzi jakiś klient (i dodaje do kolejki by accept zaakceptował)

    //czyszczenie struktury socketów
    FD_ZERO(&rmask);
    FD_ZERO(&wmask);

    while(1) {
        FD_ZERO(&rmask);
        FD_SET(sfd, &rmask); //funkcja umieszczająca jakiś socket na liście; tutaj socket sfd na liście rmask
        fdmax = sfd;
        for(int i=0; i<LIMIT; i++) {
            if(lista_cfd[i] > 0) {
                FD_SET(lista_cfd[i], &rmask); //dodanie cfd do rmask
            }
            if(lista_cfd[i] > fdmax) fdmax = lista_cfd[i];
        }

        timeout.tv_sec = 30; //przechowuje liczbę sekund ; określa jak długo czekać, by uznać, że już nie czekamy dalej - timeout
        timeout.tv_usec = 0; //przechowuje liczbę mikrosekund
        printf("-----------------------\n");
        ret = select(fdmax+1, &rmask, &wmask, (fd_set*)NULL, &timeout); //zwraca liczbę deskryptorów(socketów); ze wszystkich socketów zostawia tylko te co chcą czytać/pisać; przyjmuje(najwyższ otwarty deskryptor+1, lista socketów czytających, lista socektów piszących, lista socketów zwracających błąd-nie ma takiej listy-NULL, ile czasu czekać na jakiś socket)
        //select jest funkcją blokującą; czeka, aż któryś z socketów będzie mógł coś przeczytać, zwraca taką liczbę, ile socketów będzie miało możliwość coś zrobić; sockety które nie będą nic robić wyrzuca z listy rmask i wmask
        if(ret == 0) {
            printf("Nie ma zadnego socketa, z ktorego mozna czytac. Timeout.\n");
            continue;
        }

        //NOWE POLACZENIE
        if(FD_ISSET(sfd, &rmask)) {  //funkcja sprawdzająca, czy sfd znajduje się w rmask (czyli czy jakiś klient chce się połączyć)
            sl = sizeof(caddr);
            int cfd = accept(sfd, (struct sockaddr*)&caddr, &sl); //jak klient się połączy, to wypełniana struktura caddr (struktura o kliencie, który się własnie połączył)
            printf("Nowe polaczenie z: %s:%d [cfd: %d]\n",
                inet_ntoa((struct in_addr)caddr.sin_addr),
                ntohs(caddr.sin_port), cfd);
            for(int i=0; i<LIMIT; i++) {
                if(lista_cfd[i] == 0) {
                    lista_cfd[i] = cfd; // wpisanie klienta na listę klientów
                    break;
                }
            }
        }

        //KLIENT
        for(int i=0; i<LIMIT; i++) {  //czy któryś z podłączonych klientów chce coś napisać (czy serwer może czytać)
            int cfd = lista_cfd[i];  //biorę kolejne deskryptory z listy i spradzam, czy z tego deskryptora mogę czytać (rmask)
            if(FD_ISSET(cfd, &rmask)) {  //jeśli jakiś konkretny numer deskryptora znajduje się w rmask (bo coś do mnie napisał)
                char buf[100]; //bufor na odebrane dane
                memset(buf, 0, sizeof(buf));
                int rozmiar = read(cfd, buf, 100); //z cfd do bufora 100Bajtów; dosteję liczbę odczytanych bajtów
                if(rozmiar == 0) { //gdy socketKlient.Disconnect wysłał 0 w funkcji stop
                    printf("Koniec polaczenia z [cfd: %d]\n", cfd);
                    close(cfd);
                    lista_cfd[i] = 0;
                    zamknijSesje(cfd);
                } else {
                    printf("Odebrano komunikat %s z [cfd: %d] [%dB]\n", buf, cfd, rozmiar);
                    analizujKomunikat(buf, cfd); //przekazuję treść komunikatu i skąd to przyszło
                }
            }
        }
    }
    close(sfd);
    printf("BYE!\n");
    return 0;
}
