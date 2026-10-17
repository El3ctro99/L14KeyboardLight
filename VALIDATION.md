# Ellenőrzés – 2026. október 17.

Környezet: ThinkPad L14 Gen 3 Intel (21C2), Windows 11 build 26300, beépített .NET Framework 4.8.1, a már telepített Lenovo IBMPmDrv driver.

- Fordítás: Windows Framework64 v4 C# fordító, C# 5, x64, optimalizálás, figyelmeztetések hibaként. Sikeres.
- 20 állapotgép-teszt: sikeres. Fényerő 0/1/2; automatikus Off nem válik mentett választássá; kézi felülbírálás; tálcából választás; inaktivitás és ébresztés szimulációja; olvasási/írási/mentési hiba; részlegesen sikerült írás; újrapróbálás; gyors teljesképernyő-váltás; letiltás és kilépés.
- 30 geometriai teszt: sikeres. Több monitor és negatív koordináták; teljes monitorméret és munkaterület megkülönböztetése; láthatatlan/minimalizált/rejtett/shell ablak kizárása; normál és keret nélküli maximalizálás; 2 pixeles tolerancia.
- A laptopon végzett tényleges driverpróba: mindhárom fokozat kiolvasása és beállítása, eredeti állapot visszaállítása. Sikeres.
- Látható saját tesztablakkal, a telepített tálcaprogrammal végzett integrációs próba: valódi teljes képernyő felismerése, fizikai fényerő Off-ra állítása, ablakos módra visszatérve a korábbi fokozat visszaállítása, új teljes képernyős alkalom ismételt lekapcsolása, teljes képernyő közbeni külső/kézi fokozatváltás megtartása, normál maximalizálás kizárása, szabályos programkilépéskor visszaállítás. Sikeres; a próba eredeti fényereje visszaállítva.
- Végállapot: program normál felhasználóként fut, HKCU automatikus indulás beállítva; korábbi BacklightResetService leállítva és letiltva, előző indítási beállításai mentve.

A Windows tényleges zárolását, kijelzőkikapcsolását, alvását és leállítását nem idéztük elő a felhasználó munkája közben; az ezekhez tartozó állapotkezelést szimulált tesztek ellenőrizték. Több fizikai monitorral és exkluzív teljes képernyős játékkal nem történt helyi próba.
