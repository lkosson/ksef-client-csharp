Potrzeba biznesowa: Wysłania faktury jako właściciel

@smoke @sendFA
Scenariusz: Posiadając upranienie właścicielskie wysyłamy fakturę
Zakładając, że jestem uwierzytelniony w sesji interaktywnej
Jeżeli wyślę plik zgodny ze schemą V2 faktury
Wtedy zostanie on przyjęty przez API i stanie się fakturą z unikalnym numerem KSEF


@smoke @sendFAEncrypted @regresja
Scenariusz: Posiadając upranienie właścicielskie wysyłamy szyfrowaną fakturę z nieprawidłowym numerem NIP sprzedawcy
Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
Jeżeli wyślę szyfrowany plik zgodny ze schemą V2 faktury, ale z niepoprawnym numerem NIP sprzedawcy
Wtedy zostanie on odrzucony z powodu braku autoryzacji

@smoke
Scenariusz: Posiadając uprawnienie właścicielskie pytamy o fakturę wysłaną
Zakładając, że jestem uwierzytelniony w sesji interaktywnej
Oraz wyślę plik zgodny ze schemą V2 faktury
Jeżeli zostanie on przyjęty przez API i stanie się fakturą z unikalnym numerem KSEF
Wtedy pytam o fakturę o danym numerze KSEF