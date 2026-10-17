# Ellenőrzés – 2026. október 17.

Környezet: ThinkPad L14 Gen 3 Intel (21C2), Windows 11 build 26300, beépített .NET Framework 4.8.1, a már telepített Lenovo IBMPmDrv driver.

- Fordítás: Windows Framework64 v4 C# fordító, C# 5, x64, optimalizálás, figyelmeztetések hibaként. Sikeres.
- 20 állapotgép-teszt: sikeres. Fényerő 0/1/2; automatikus Off nem válik mentett választássá; kézi felülbírálás; tálcából választás; inaktivitás és ébresztés szimulációja; olvasási/írási/mentési hiba; részlegesen sikerült írás; újrapróbálás; gyors teljesképernyő-váltás; letiltás és kilépés.
- 30 geometriai teszt: sikeres. Több monitor és negatív koordináták; teljes monitorméret és munkaterület megkülönböztetése; láthatatlan/minimalizált/rejtett/shell ablak kizárása; normál és keret nélküli maximalizálás; 2 pixeles tolerancia.
- A laptopon végzett tényleges driverpróba: mindhárom fokozat kiolvasása és beállítása, eredeti állapot visszaállítása. Sikeres.
- Látható saját tesztablakkal, a telepített tálcaprogrammal végzett integrációs próba: valódi teljes képernyő felismerése, fizikai fényerő Off-ra állítása, ablakos módra visszatérve a korábbi fokozat visszaállítása, új teljes képernyős alkalom ismételt lekapcsolása, teljes képernyő közbeni külső/kézi fokozatváltás megtartása, normál maximalizálás kizárása, szabályos programkilépéskor visszaállítás. Sikeres; a próba eredeti fényereje visszaállítva.
- Végállapot: program normál felhasználóként fut, HKCU automatikus indulás beállítva; korábbi BacklightResetService leállítva és letiltva, előző indítási beállításai mentve.

A Windows tényleges zárolását, kijelzőkikapcsolását, alvását és leállítását nem idéztük elő a felhasználó munkája közben; az ezekhez tartozó állapotkezelést szimulált tesztek ellenőrizték. Több fizikai monitorral és exkluzív teljes képernyős játékkal nem történt helyi próba.

## 1.1.0 – angol és magyar felület

2026. október 17.: a figyelmeztetéseket hibaként kezelő C# 5 fordítás sikeres. Mind a 67 automatizált teszt sikeres: az eredeti 20 állapotgép- és 30 geometriai teszt mellett 17 nyelvi ellenőrzés. Utóbbiak ellenőrzik a Windows-nyelv szerinti alapértéket, a kifejezett választás elsőbbségét, a régi JSON-beállítások változatlan fényerővel történő migrációját, a mentett angol nyelv visszaolvasását, a teljes fordításkészletet, a tooltip hosszát és a nyelvváltás után újrafordítható driverhibákat. A nyelvváltás nem hívja meg a fényerőszabályzatot.

## 1.1.1 – kijelzőkikapcsolás

2026. október 17.: mind a 102 automatizált teszt sikeres. A korábbi 67 mellett 19 új állapotgép-teszt ellenőrzi a kijelzőki/-be viselkedést, a teljes képernyős automatika kikapcsolt állapotát, a kézi felülbírálás megőrzését, az alvás/zárolás alatti visszaállítás halasztását és a sikertelen driverhívások újrapróbálását. További 16 teszt a Windows `POWERBROADCAST_SETTING` értesítés tényleges adatelrendezését és a 0/1/2 állapot kezelését ellenőrzi.

A laptopon a Windows valódi kijelzőki értesítése megérkezett, a program lekapcsolta a billentyűzetfényt, és utána a Lenovo-driverből 0-s tényleges fényerőt olvastunk vissza. A mentett Erős (2) fokozat és a többi felhasználói beállítás megmaradt. A külön értesítéses integrációs próba aktív asztalt igénylő előfeltétele nem teljesült, mert a fizikai kijelző ekkor már kikapcsolt állapotban volt; ezt a próbát nem tekintjük sikeres teljes kijelzőki/-be hardverpróbának. A visszatérés, a dimmelt kijelző és a zárolás kezelése automatizált tesztekkel ellenőrzött; tényleges feloldást vagy ébresztést nem kényszerítettünk ki.
