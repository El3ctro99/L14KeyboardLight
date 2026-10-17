# L14 Keyboard Light

Kis Windows-tálcaprogram, amely az előtérben lévő teljes képernyős ablaknál lekapcsolja a ThinkPad billentyűzetvilágítását, majd visszaállítja a korábbi fényerőt.

**Támogatott típus: ThinkPad L14 Gen 3 Intel, 21C2 géptípus, világítós billentyűzettel és a Lenovo meglévő energiagazdálkodási driverével.** A program más géptípuson vagy ismeretlen driverválasznál leáll. Egy ilyen laptopon, Windows 11 alatt próbáltuk ki.

[English documentation](README.md)

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

- **Ki / Halvány / Erős:** fényerő beállítása és megjegyzése.
- **Teljes képernyőn automatikusan kikapcsol:** az előtérben lévő teljes képernyős alkalmazásnál lekapcsolja a billentyűzet fényét, kilépéskor visszaállítja a korábbi fokozatot. A jelölés kikapcsolásával szüneteltethető az automatikus vezérlés.
- **Indítás a Windowszal:** automatikus indulás a saját Windows-fiókodban.
- **Állapot…:** a géptípus, a működés és az utolsó hiba megjelenítése. Dupla kattintással is megnyitható.
- **Kilépés:** leállítja a programot, és visszaállítja az általa automatikusan lekapcsolt fényt, ha közben nem történt kézi módosítás.

Az **Fn+Space továbbra is működik**. Ha teljes képernyő közben kézzel visszakapcsolod a fényt, a program az adott teljes képernyős alkalom végéig elfogadja a választásodat. Új teljes képernyős alkalommal ismét automatikusan lekapcsol. A tálcamenüben választott fokozat is kézi felülbírálásnak számít.

Az automatikus vezérlés szüneteltetése és a szabályos kilépés visszaállítja a program által lekapcsolt fényt, de nem írja felül az időközben kézzel választott fényerőt.

## A teljes képernyő felismerése és korlátai

Az előtérben lévő ablakot a hozzá tartozó monitor teljes méretével hasonlítja össze, körülbelül 350 ms-onként. A normál maximalizálás, az asztal és a zárolási képernyő nem aktiválja. A geometriai felismerés több monitorral, negatív koordinátákkal és eltérő DPI-vel is számol.

A felismerés az **ablak méretét** figyeli, nem a videó lejátszását. A szüneteltetett teljes képernyős videót is teljes képernyőnek tekinti. Egy kézzel pontosan monitor méretűre húzott, keret nélküli ablak is annak számíthat. Több fizikai monitorral és exkluzív teljes képernyős játékkal nem történt helyi próba.

Amíg a Windows inaktívnak jelzi a munkamenetet, a program nem menti a firmware által lekapcsolt fényt új választásként. Visszatéréskor rövid várakozás után újraértékeli a teljes képernyős állapotot, és szükség szerint visszaállítja a megjegyzett fokozatot. A tényleges zárolást, kijelzőkikapcsolást és alvást nem idéztük elő a helyi próba során; ezek állapotkezelését szimulált tesztek ellenőrzik.

## Beállítások és diagnosztika

A beállítások és a diagnosztikai naplók helye: `%LOCALAPPDATA%\L14KeyboardLight`. A napló körülbelül 256 KB-nál fordul, és egy korábbi fájlt tart meg.

Ezek a parancsok nem indítanak második tálcavezérlőt:

```powershell
# 20 állapotgép- és 30 geometriai teszt; hardverírás nélkül.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--self-test', 'self-test.txt' -Wait
Get-Content .\self-test.txt

# A géptípus, a hardver fényereje és az előtérben lévő ablak kiolvasása.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--status', 'status.txt' -Wait

# Az ugyanebben a Windows-munkamenetben futó példány szabályos leállítása.
.\bin\L14KeyboardLight.exe --stop
```

Állapotjelentés megosztása előtt nézd át a benne lévő gép- és ablakadatokat.

## Ellenőrzés

- Mind az **50 szimulált teszt** sikeres: 20 állapotgép- és 30 geometriai teszt.
- Egy L14 Gen 3 Intel 21C2 laptopon mindhárom tényleges fényerő kiolvasása és beállítása sikeres volt.
- Látható saját tesztablakkal sikeresen ellenőriztük a teljes képernyős lekapcsolást, az ablakos módra váltáskor történő visszaállítást, az ismételt belépést, a kézi felülbírálást, a normál maximalizálás kizárását és a szabályos kilépéskor történő visszaállítást.
- A tényleges alvás, zárolás és kijelzőkikapcsolás, az exkluzív teljes képernyős játékok és a több fizikai monitoros működés helyileg még nem volt kipróbálva.

A részletes eredeti feljegyzés a [VALIDATION.md](VALIDATION.md), a változások listája a [CHANGELOG.md](CHANGELOG.md) fájlban található.

## A driverprotokoll referenciája

A Lenovo driver interfészének ellenőrzéséhez a [SaltwaterC/KeyboardBacklightForLenovo](https://github.com/SaltwaterC/KeyboardBacklightForLenovo) projekt szolgált referenciaként. Az L14-specifikus válaszértékeket a támogatott laptopon külön kiolvastuk és mindhárom fokozattal kipróbáltuk.

A program az `IBMPmDrv` `0x00222680` kiolvasó és `0x00222684` beállító IOCTL-hívását használja. Ezen a hardveren a Ki / Halvány / Erős válaszértéke `0x00250200`, `0x00250201`, illetve `0x00250202`. Ismeretlen típusnál vagy válasznál hibával leáll; nincs közvetlen EC- vagy fizikai memóriahozzáférést használó tartalék megoldás.

## Licenc

[MIT](LICENSE). Copyright (c) 2026 El3ctro99. A Lenovo és a ThinkPad a jogosultjaik védjegyei; ez egy független projekt.
