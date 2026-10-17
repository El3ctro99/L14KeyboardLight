# L14 Keyboard Light

Kis Windows-tálcaprogram, amely kikapcsolt kijelzőnél és az előtérben lévő teljes képernyős ablaknál lekapcsolja a ThinkPad billentyűzetvilágítását, majd visszaállítja a korábbi fényerőt.

**Támogatott típus: ThinkPad L14 Gen 3 Intel, 21C2 géptípus, világítós billentyűzettel és a Lenovo meglévő energiagazdálkodási driverével.** A program más géptípuson vagy ismeretlen driverválasznál leáll. Egy ilyen laptopon, Windows 11 alatt próbáltuk ki.

[English documentation](README.md)

A menü, az állapotablak, az ikon súgószövege és a program saját üzenetei **magyarul és angolul** is elérhetők. Első indításkor magyar Windows-megjelenítési nyelvnél magyarul, egyébként angolul indul. A tálcamenü **Nyelv → English / Magyar** pontjával azonnal válthatsz; a választást a következő indításra is megjegyzi.

## Követelmények és működés

- Windows 11 x64 és ThinkPad L14 Gen 3 Intel **21C2**.
- Világítós billentyűzet és működő Lenovo-driver, amely elérhetővé teszi a `\\.\IBMPmDrv` eszközt.
- A Windows .NET Framework 4.8 vagy újabb környezete. A fejlesztés és helyi ellenőrzés a beépített 4.8.1-es verzióval történt.

A program normál felhasználóként fut. A meglévő Lenovo-drivert használja; nem tartalmaz WinRing0-t, TVicPortot, PawnIO-t vagy új kernel-drivert. Vantage, külön .NET Desktop Runtime és .NET SDK nem szükséges hozzá. Nincs hálózati kommunikáció, telemetria vagy automatikus frissítő.

Más billentyűzetvilágítás-vezérlővel egyszerre ne futtasd: a két program felülírhatja egymás fényerő-beállításait.

## Fordítás és hordozható használat

A Windows Forms alkalmazás C# 5 forrásból készül. A `build.ps1` a Windows .NET Framework fordítóját használja, letöltés és NuGet-csomagok nélkül.

A forrás mappájában, PowerShell 7-ből:

```powershell
pwsh -File .\build.ps1
.\bin\L14KeyboardLight.exe
```

Az EXE közvetlenül is futtatható, telepítés nélkül. A beállításokat ilyenkor is a saját felhasználói adatmappába menti. Ha a tálcamenüből engedélyezed az automatikus indulást, utána hagyd a programot ugyanazon az elérési úton.

Csak megbízható forrásból származó szkripteket futtass, a gépedre vagy szervezetedre érvényes PowerShell-szabályzat szerint.

## Telepítés és eltávolítás

Felhasználónkénti telepítés:

```powershell
pwsh -File .\install.ps1
```

A telepítő a `%LOCALAPPDATA%\Programs\L14KeyboardLight` mappába másolja a programot és az eltávolító szkripteket, Start menü parancsikont készít, engedélyezi az automatikus indulást a saját Windows-fiókodban, majd elindítja a programot. A szokásos telepítéshez nem kell rendszergazdai jog.

Ha a korábbi **Keyboard Backlight for Lenovo** vezérlő telepítve van, előbb állítsd le, és kapcsold ki az automatikus indulását, illetve a fényerőt visszaállító szolgáltatását. Ezt a telepítő külön kért átállításként is el tudja végezni, az ismert korábbi beállítások mentésével:

```powershell
pwsh -File .\install.ps1 -MigratePrevious
```

Ehhez egyszeri Windows-rendszergazdai jóváhagyás kell, mert az előző vezérlő gépszintű beállításait módosítja. A mentés tartós helye: `%LOCALAPPDATA%\L14KeyboardLight\legacy-backup`. A Lenovo hardverdriverét és a közös futtatókörnyezeteket nem távolítja el.

Eltávolítás a telepített szkripttel:

```powershell
pwsh -File "$env:LOCALAPPDATA\Programs\L14KeyboardLight\remove.ps1"
```

A `-RestorePrevious` kapcsolóval a korábban átállított vezérlő indulási beállításait is visszaállítja; ehhez rendszergazdai jóváhagyás szükséges. A saját beállítások és az átállítás mentése megmaradnak.

## Használat

A program az óra melletti tálcaikonok között fut. Ha rejtett, a felfelé mutató nyíllal találod meg. Jobb kattintás az ikonon:

| Magyar menüpont | Angol menüpont | Művelet |
| --- | --- | --- |
| **Ki / Halvány / Erős** | **Off / Low / High** | Fényerő beállítása és megjegyzése. |
| **Teljes képernyőn automatikusan kikapcsol** | **Turn off automatically in fullscreen** | Teljes képernyőn lekapcsolja a fényt, majd visszaállítja a korábbi fokozatot. A jelölés kikapcsolása csak a teljes képernyős vezérlést szünetelteti. |
| **Indítás a Windowszal** | **Start with Windows** | Automatikus indulás a saját Windows-fiókodban. |
| **Nyelv → English / Magyar** | **Language → English / Magyar** | Azonnali nyelvváltás, a választás megjegyzésével. |
| **Állapot…** | **Status…** | A géptípus, a működés és az utolsó hiba megjelenítése. Dupla kattintással is megnyitható. |
| **Kilépés** | **Exit** | Leállítja a programot. Bekapcsolt kijelzőnél és aktív munkamenetben visszaállítja az általa lekapcsolt fényt, ha közben nem történt kézi módosítás. |

Az **Fn+Space továbbra is működik**. Ha teljes képernyő közben kézzel visszakapcsolod a fényt, a program az adott teljes képernyős alkalom végéig elfogadja a választásodat. Új teljes képernyős alkalommal ismét automatikusan lekapcsol. A tálcamenüben választott fokozat is kézi felülbírálásnak számít.

A teljes képernyős vezérlés szüneteltetése és a szabályos kilépés bekapcsolt kijelzőnél, aktív munkamenetben visszaállítja a program által lekapcsolt fényt, de nem írja felül az időközben kézzel választott fényerőt.

A kijelző kikapcsolásakor történő lekapcsolás mindig működik, a teljes képernyős kapcsolótól és a teljes képernyő közbeni kézi felülbírálástól függetlenül. A Windows kijelzőkikapcsolási értesítésére a program azonnal lekapcsolja a billentyűzetfényt, zárolt munkamenetben is. Amíg a Windows engedi futni, körülbelül két másodpercenként ismét ellenőrzi. A kijelző fényerejének csökkentése önmagában nem kapcsolja le a billentyűzetfényt. A korábbi fényerőt akkor állítja vissza, amikor a kijelző és a munkamenet is aktív, és a teljes képernyős vezérlés nem indokolja a lekapcsolva tartást. Kikapcsolt kijelzőnél a szüneteltetés és a kilépés sem kapcsolja vissza a fényt.

## A teljes képernyő felismerése és korlátai

Az előtérben lévő ablakot a hozzá tartozó monitor teljes méretével hasonlítja össze, körülbelül 350 ms-onként. A normál maximalizálás, az asztal és a zárolási képernyő nem aktiválja. A geometriai felismerés több monitorral, negatív koordinátákkal és eltérő DPI-vel is számol.

A felismerés az **ablak méretét** figyeli, nem a videó lejátszását. A szüneteltetett teljes képernyős videót is teljes képernyőnek tekinti. Egy kézzel pontosan monitor méretűre húzott, keret nélküli ablak is annak számíthat. Több fizikai monitorral és exkluzív teljes képernyős játékkal nem történt helyi próba.

Amíg a Windows inaktívnak jelzi a munkamenetet, a program nem menti a firmware által lekapcsolt fényt új választásként. Visszatéréskor körülbelül 1,4 másodperc várakozás után újraértékeli a teljes képernyős állapotot, és szükség szerint visszaállítja a megjegyzett fokozatot. A kijelző visszakapcsolásának értesítése önmagában nem kapcsolja vissza a billentyűzetfényt, ha a munkamenet továbbra is zárolt vagy felfüggesztett. Az értesítések feldolgozását és ezeket az állapotváltásokat automatizált tesztek ellenőrzik.

## Beállítások és diagnosztika

A beállítások és a diagnosztikai naplók helye: `%LOCALAPPDATA%\L14KeyboardLight`. A napló körülbelül 256 KB-nál fordul, és egy korábbi fájlt tart meg.

A választott nyelv `en` vagy `hu` értékként kerül a `settings.json` fájlba. Az 1.0.0-s beállítások betöltésekor a program hozzáadja az alapértelmezett nyelvet, a mentett fényerő és az automatikus vezérlés beállításának módosítása nélkül. A nyelvváltás nem változtatja meg a billentyűzet fényerejét.

Ezek a parancsok nem indítanak második tálcavezérlőt:

```powershell
# Mind a 102 állapotgép-, kijelző-, energiaértesítési, geometriai és nyelvi teszt; hardverírás nélkül.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--self-test', 'self-test.txt' -Wait
Get-Content .\self-test.txt

# A géptípus, a hardver fényereje és az előtérben lévő ablak kiolvasása.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--status', 'status.txt' -Wait

# Az ugyanebben a Windows-munkamenetben futó példány szabályos leállítása.
.\bin\L14KeyboardLight.exe --stop
```

Állapotjelentés megosztása előtt nézd át a benne lévő gép- és ablakadatokat.

## Ellenőrzés

- Mind a **102 automatizált teszt** sikeres: 20 korábbi állapotgép-, 19 kijelzőkikapcsolási, 16 energiaértesítési, 30 geometriai és 17 nyelvi teszt.
- Egy L14 Gen 3 Intel 21C2 laptopon mindhárom tényleges fényerő kiolvasása és beállítása sikeres volt.
- Látható saját tesztablakkal sikeresen ellenőriztük a teljes képernyős lekapcsolást, az ablakos módra váltáskor történő visszaállítást, az ismételt belépést, a kézi felülbírálást, a normál maximalizálás kizárását és a szabályos kilépéskor történő visszaállítást.
- Az exkluzív teljes képernyős játékok és a több fizikai monitoros működés helyileg még nem volt kipróbálva. Az energiaesemények ellenőrzését, a beadott értesítések és a tényleges hardveres ciklusok megkülönböztetésével, a [VALIDATION.md](VALIDATION.md) részletezi.

A részletes eredeti feljegyzés a [VALIDATION.md](VALIDATION.md), a változások listája a [CHANGELOG.md](CHANGELOG.md) fájlban található.

## A driverprotokoll referenciája

A Lenovo driver interfészének ellenőrzéséhez a [SaltwaterC/KeyboardBacklightForLenovo](https://github.com/SaltwaterC/KeyboardBacklightForLenovo) projekt szolgált referenciaként. Az L14-specifikus válaszértékeket a támogatott laptopon külön kiolvastuk és mindhárom fokozattal kipróbáltuk.

A program az `IBMPmDrv` `0x00222680` kiolvasó és `0x00222684` beállító IOCTL-hívását használja. Ezen a hardveren a Ki / Halvány / Erős válaszértéke `0x00250200`, `0x00250201`, illetve `0x00250202`. Ismeretlen típusnál vagy válasznál hibával leáll; nincs közvetlen EC- vagy fizikai memóriahozzáférést használó tartalék megoldás.

## Licenc

[MIT](LICENSE). Copyright (c) 2026 El3ctro99. A Lenovo és a ThinkPad a jogosultjaik védjegyei; ez egy független projekt.
