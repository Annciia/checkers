# Warcaby

Projekt jest klasyczną grą w warcaby. Do jednej sesji łączy się dwóch użytkowników. Może być wiele sesji jednocześnie.

Projekt składa się z dwóch programów - serwera i klienta. Do zadań serwera należy pośrednictwo w komunikacji klientów oraz zarządzanie organizacją klientów (tj. tworzenie sesji łączącej ze sobą klientów, którzy będą komunikować się w ramach danej rozgrywki). Program klienta odpowiada za logikę gry oraz UI.

**Kompilacja serwera:**

Serwer jest napisany w C na system Linux. Serwer można skompilować z poziomu linii komend za pomocą kompilatora gcc przy użyciu poniższej komendy:

`gcc main.c`

Następnie, aby uruchomić serwer należy wprowadzić następujące polecenie:

`./a.out`

**Kompilacja klienta:**

Klient jest aplikacją WPF napisaną w C# na systemy Windows. Konieczne może być wcześniejsze zainstalowanie środowiska .NET Framework. Aby skompilować program klienta należy mieć zaintalowane IDE Visual Studio 2022 z dodanym pakietem "Programowanie aplikacji klasyczny dla platformy .NET". Przed kompilacją należy ustawić wartość zmiennej `serverIp` w pliku `Komunikacja.cs` na rzeczywisty adres IPv4 serwera do którego klient ma się połączyć.

Aby uruchomić aplikację klienta uruchamiamy:

`WpfApp1.exe`