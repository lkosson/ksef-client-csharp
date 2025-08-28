language: pl
Potrzeba biznesowa: Operacje na sesji wsadowej

@smoke @Batch @regresja @KSEF20-460
Scenariusz: Wys³anie dokumentów w jednoczêœciowej paczce
Zak³adaj¹c, ¿e przygotowa³em paczkê dokumentów gotowych do wys³ania w sesji wsadowej
Je¿eli je¿eli nawi¹¿ê sesjê wsadow¹
Oraz wyœlê paczkê dokumentów na wskazany adres
Oraz zakoñczê sesjê wsadow¹
Wtedy dokumenty zostan¹ przetworzone i zostan¹ na ich podstawie wystawione faktury
Oraz zostanie to potwierdzone wygenerowaniem dokumentu UPO

@smoke @Batch @regresja @Negative @KSEF20-459
Scenariusz: Wys³anie jednoczêœciowej paczki dokumentów ze z³ym NIP
Zak³adaj¹c, ¿e przygotowa³em paczkê dokumentów z nieprawid³owym NIP gotowych do wys³ania w sesji wsadowej
Je¿eli je¿eli nawi¹¿ê sesjê wsadow¹
Oraz wyœlê paczkê dokumentów na wskazany adres
Oraz zakoñczê sesjê wsadow¹
Wtedy proces wystawiania faktury zakoñczy siê niepowodzeniem