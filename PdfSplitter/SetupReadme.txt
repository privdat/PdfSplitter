Pdf Splitter V1.0

Telepítési és konfigurálási útmutató.

-	A programot telepíteni nem szükséges, csak fel kell másolni a gépre.
- 	Java futtatókörnyezetnek viszont telepítve kell lennie a program használatához, valamint a windows környezeti változói közt
	szerepelnie kell a java futtató parancsnak.
	Ingyenesen letölthető és használható java környezet: Amazon Corretto  
	(Program kiadás pillanatában elérhető: https://docs.aws.amazon.com/corretto/latest/corretto-11-ug/downloads-list.html )

Konfigurálás:
	- Az appsettings.json fájlban lehet a beállításokat megtenni:
		"output": "",                         	- ha üres, akkor az alkalmazásmappában automatikusan létrejön egy output mappa. Ide kerülnek a feldarabolt pdf-ek.
		"input": "",							- ha üres, akkor az alkalmazásmappában automatikusan létrejön egy input mappa. Innen veszi fel a feldarabolandó pdf-eket.
		"archive": "", 							- ha az "archivateOriginalFiles" true-ra van állítva, a darabolás után ide elmenti az eredeti fájlokat. Ez a mappa is létrejön, ha nincs megadva.
		"pagePerFile": 3,						- Itt adható meg, hogy hány oldalasra darabolja fájlonként a pdf-eket.
		"overwriteOutput": true,				- ha azonos nevű fájlt talál (azonos elérési úttal), akkor beállítható, hogy felülírja e a korábbiakat.
		"infoLogOn": true,						- be és kikapcsolható az informális logolás. A hiba logok mindenképp kiírásra kerülnek.
		"archivateOriginalFiles":  true			- ha be van kapcsolva, akkor az eredeti fájlokat az "archive" mappába elmenti.
	
	- A konfigurációs beállítások a progrm újraindítását követően lépnek életbe.
	
Futtatás:
	- A PdfSplitter.exe futtatásával indíthatjuk az alkalmazást. 
	- Amig a program fut, folyamatosan figyeli az input mappát, és ha új pdf-et talál, azt feldolgozza.
	- Hiba esetén a Error_log.txt-fájlból kaphatunk bővebb információt.
	- Informális logok a Info_log.txt fájlba kerülnek, amennyiben a konfigurációban engedélyezve van.