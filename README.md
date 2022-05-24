# GpxTool
This commandline tool is for getting informations and processing GPX-files.

Mit dem Kommandozeilenprogramm können Informationen aus einer GPX-Datei geliefert werden und Manipulationen mit GPX-Dateien erfolgen. Die Steuerung erfolgt über die Angabe von Optionen und Parametern.

* **-i, --info=arg**   Ausgabe von Waypoint-, Routen-, Track- und Segment-Infos auf STDOUT (Name, Länge usw.) (Standard: true)  
* **--withsubdirs=arg**   bei Verwendung von * oder ? werden Eingabedateien auch in Unterverzeichnissen gesucht  
* **-n, --name=arg**   neuer Trackname (mehrfach verwendbar für Track 1 usw.)  
* **-o, --output=arg**   Name der Ausgabedatei für die (ev. veränderten) GPX-Daten (auch KML/KMZ Datei möglich)  
* **--overwrite=arg**   eine ev. schon vorhandene GPX-Datei darf überschrieben werden (ohne arg 'true', Standard 'false')  
* **--simplifygpx=arg**   GPX-Datei vereinfachen (ohne arg 'true', Standard: 'false', bei mehreren Dateien immer true)  
* **-f, --formatted=arg**   Ausgabe formatiert oder '1-zeilig' (ohne arg 'true', Standard: 'false')  
* **-t, --tracks=arg**   Liste (mit Komma) der zu verwendenden Tracknummern (1, ...) (Standard: alle)  
* **-r, --routes=arg**   Liste (mit Komma) der zu verwendenden Routennummern (1, ...) (Standard: alle)  
* **-p, --waypoints=arg**   Liste (mit Komma) der zu verwendenden Waypoints (1, ...) (Standard: alle)  
* **--segment2track=arg**   zusätzliche Segmente in eigene Tracks umwandeln (ohne arg 'true', Standard: 'false')  
* **--deletetime=arg**   alle Zeitstempel werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')  
* **--deleteheight=arg**   alle Höhenangaben werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')  
* **-G, --gapfill**   fehlende Höhenwerte und Zeitstempel linear interpolieren (ohne arg 'true', Standard: 'false')  
* **-N, --newheigth=arg**   alle Höhen werden in den Trackpunkten auf einen konstanten Wert gesetzt  
* **-s, --simplify[=arg]**   Vereinfachung der Tracks [mit Algorithmus Reumann-Witkam (RW) oder Douglas-Peucker (DP)] (Standard: keine)  
* **-w, --width=arg**   Breite des Toleranzbereiches für die Vereinfachung (Standard 0.05)  
* **-m, --maxspeed=arg**   Punkte entfernen, die mit einer höheren Geschwindigkeit erreicht werden (in km/h; Standard: inaktiv)  
* **-a, --restarea=arg**   Werteliste (mit Komma) für die Pausenplatzeliminierung, z.B. 10,1,20,60,2,25,50 (Standard: inaktiv)  
* **--restareaprot=arg**   Name der Protokolldatei für die Pausenplatzeliminierung (liefert je Zeile den Original-Punktindex, die Anzahl der Polygonkreuzungen, den Radius des kleinsten Umkreises, den durchschnittlichen Abweichungswinkel und 1, wenn der Punkt gelöscht wurde)  
* **-O, --heightoutput=arg**   Name der Ausgabedatei für die (ev. veränderten) Höhen-Daten in Abhängigkeit der jeweiligen Tracklänge  
* **--minheight=arg**   Minimalhöhe; alle kleineren Höhen werden damit ersetzt  
* **--maxheight=arg**   Maximalhöhe; alle größeren Höhen werden damit ersetzt  
* **-S, --heightsimplify[=arg]**   Höhenprofil vereinfachen [mit Algorithmus SlidingIntegral (SI) oder SlidingMean (SM)] (Standard: keine)  
* **-W, --heightwidth=arg**   Breite des Höhen-Integrationsbereiches in Metern (Standard 100)  
* **-U, --heightoutlierwidth=arg**   Breite des Bereiches für die 'Ausreißer'-Korrektur von Höhen (Standard 50m)  
* **-A, --maxascent=arg**   max. gültiger An-/Abstieg in Prozent (Standard 25%)  
* **--filenametotrackname=arg**   Tracknamen auf den Dateinamen setzen (Standard: false)  
* **--onefilepertrack=arg**   jeden Track in einer eigenen Datei ausgeben (Standard: false)  
* **--kmltrackdata=arg**   Farbe und Linienbreite für jeden Track bei KML-Ausgabe (Liste aus jeweils ARGB/RGB-Farbe und Breite)  
* **-?, --help**   diese Hilfe   

Außerdem benötigt das Programm mindestens 1 Argument. Die Argumente sind die Dateinamen der zu verarbeitenden GPX-Dateien. Werden mehrere Dateien verwendet, werden diese vor der weiteren Verarbeitung zusammengefügt. Die Waypoints, Routen und Tracks sind dann entsprechend der Dateireihenfolge durchnummeriert. Enthält die erste und zweite Datei z.B. jeweils 2 Tracks, haben die Tracks der zweiten Datei dann die Nummern 3 und 4.

*Beispiel: GPX-Dateien verbinden*

`gpxtool --output=dest.gpx inp1.gpx inp2.gpx inp3.gpx`

Neue Tracknamen können folgendermaßen vergeben werden:

*Beispiel: Tracknamen ändern*

`gpxtool --name="Track 1" --name="Track 2" --output=dest.gpx inp1.gpx`

Die Menge der auszugebenden Wegpunkte, Routen oder Tracks kann eingeschränkt oder ganz verhindert werden.

*Beispiel: keine Wegpunkte und Routen ausgeben, nur Tracks 1 und 3 ausgeben*

`gpxtool --waypoints --routes --trakcks=1,3 --output=dest.gpx inp1.gpx`

Gibt es für einige Trackpunkte keine Höhenangaben und/oder keine Zeitangaben, können diese interpoliert werden. Das funktioniert nicht in jedem Fall. Bei der Vereinfachung des Höhenprofils wird diese Option automatisch gesetzt.

*Beispiel: fehlende Höhen- und/oder Zeitangaben interpolieren*

`gpxtool --gapfill --output=dest.gpx inp1.gpx`

### Trackvereinfachung und Beseitigung von Ausreißern

Tracks können mit 2 verschiedenen Algorithmen vereinfacht werden. Der Douglas-Peucker scheint i.A. die besseren Ergebnisse zu liefern und ist deshalb das Standardverfahren. Reumann-Witkam soll bei verschlungenen Tracks bessere Ergebnisse liefern.

Bei Douglas-Peucker wird ausgehend vom ganzen Track rekursiv für immer kleinere Teilstrecken geprüft, ob die zwischen 2 Punkten liegenden Punkte alle innerhalb eines Toleranzbereiches (rechteckiger Streifen) befinden. Ist das der Fall, entfallen die inneren Punkte. Andernfalls werden mit den "zu weit weg liegenden" Punkten neue Teilstrecken gebildet und die Untersuchung wiederholt.

Bei Reumann-Witkam werden iterativ alle Punktpaare (i, i+1) untersucht. Liegen direkt folgende Punkte i+2, ... innerhalb eines Toleranzbereiches (rechteckiger Streifen entsprechend der Punkte i und i+1), entfallen diese Punkte.

Der entscheidende Parameter ist in jedem Fall die Breite des Toleranzbereiches. Intern ist er mit 0,05 vordefiniert und hat damit i.A. nur eine sehr geringe Auswirkung. Je breiter dieser Bereich ist, je stärker wird ein Track vereinfacht. Problematisch sind in jedem Fall einzelne "Ausreisser". Diese mit der Vereinfachung natürlich nicht  beseitigt und sollten vorher mit einem Editor für GPX-Tracks entfernt werden.

Beispiel: Trackvereinfachung

   ```
   rem ===== Douglas-Peucker mit 0,05
   gpxtool --simplify --output=dest.gpx inp1.gpx
   rem ===== Reumann-Witkam mit 4,2
   gpxtool --simplify=RW --width=4.2 --output=dest.gpx inp1.gpx
   ```

Einzelne Ausreißer (fehlerhafte Messungen) verraten sich manchmal dadurch, dass sich für das Teilstück zum vorhergehenden Punkt eine deutlich höhere Geschwindigkeit ergibt. Man kann einen Grenzwert für die erlaubte Geschwindgkeit angeben. Punkte die mit höherer Geschwindgkeit erreicht werden, werden entfernt.

   ```
   rem ===== max. erlaubte Geschwindgkeit 10,5km/h
   gpxtool --maxspeed=10.5
   ```

### Rastplatzerkennung

Experimentell ist die "Rastplatz"-Vereinfachung. Damit sollen i.W. Teile im Track entfernt werden, die keine reale Bewegung darstellen. Während einer längeren Rast zeichnet ein GPS-Gerät z.B. weitere Punkte auf, die sehr nahe am Rastplatz liegen. Das können in ungünstigen Fällen mehrere 100 Meter sein.

Als Parameter wird ein Liste von natürlichen Zahlen benötigt: p,k1,r1,g1,k2,r2,g2. Mit der Punkanzahl p wird die Länge des jeweils untersuchten Track-Teilstückes angegeben. K1 und k2 geben jeweils eine Anzahl vor, wie oft sich dieses Teilstück mit sich selbst kreuzt.K1 bedeutet dabei "k1 bis k2-1 Kreuzungen", k2 bedeutet "k2 oder mehr Kreuzungen". Je nach Anzahl der gefundenen Kreuzungen müssen alle p Punkte eine durchschnittliche Änderung der Bewegungsrichtung (jeweils von Punkt zu Punkt) mit dem Mindestwert g1 bzw. g2 haben und sie müssen alle in einen Kreis mit dem Radius r1 bzw. r2 passen.

Im folgenden Beispiel bedeutet die Parameterliste:
  * Ein Teilstück des Tracks ist 10 Punkte lang. (p) 
  * Es muss mindestens 1 Kreuzung vorhanden sein (k1) 
  * Der Radius des Umkreises darf höchsten 18m sein (r1) 
  * Die durchschnittliche Änderung der Bewegungsrichtung muss mindestens 60° sein (g1) 
  * Es müssen mindestens 2 Kreuzungen vorhanden sein (k2). 
  * Der Radius des Umkreises darf höchsten 25m sein (r2) 
  * Die durchschnittliche Änderung der Bewegungsrichtung muss mindestens 50° sein (g2) 
Wenn diese Bedingungen zutreffen, werden alle p Punkte bis auf einen gelöscht. Es bleibt nur der Punkt erhalten, der am dichtesten am Mittelpunkt des zutreffenden Umkreises liegt.

Beispiel: Rastplatzerkennung

   `gpxtool --restarea=10,1,18,60,2,25,50`

### Höhenglättung und Beseitigung von Ausreißern

Die Höhenangaben von Tracks können "geglättet" werden. Dafür stehen 2 Verfahren zur Verfügung: SlidingIntegral und SlidingMean.

Bei SlidingMean wird für jeden Punkt der gleitende Mittelwert der Höhen der benachbarten Punkte gebildet. Dabei wird auch die jeweilige Streckenlänge mit einbezogen (gewichtet). Die Anzahl der einzubeziehenden Punkte ist der entscheidende Parameter. Bei einer geradzahligen Anzahl wird wird vor dem zu berechnenden Punkt ein Punkt mehr berücksichtigt als nach diesem Punkt.

Bei SlidingIntegral wird die Höhe für einen Punkt als durchschnittliche Höhe in einem Entfernungsbereich um diesen Punkt ermittelt. Die gesamte Breite dieses Bereiches in Metern wird als Parameter angegeben. Diese Variante scheint die besten Ergebnisse zu liefern.

Für beide Varianten wird vor der eigentlichen Glättung eine Dämpfung für Ausreisser vorgenommen. Dafür wird ein max. gültiger An- bzw. Abstieg in Prozent angegeben. Wird dieser Anstieg überschritten, wird die entsprechende Höhe als Mittelwert der vorhergehenden Punkte neu berechnet. Ist das nicht gewünscht, sollte unrealistisch hoher Wert festgelegt werden. Vordefiniert sind 25%, d.h. 25m Anstieg auf 100m Entfernung.

Zur Erzeugung von Höhenprofilen können die dafür nötigen Daten exportiert werden. Die Exportdatei enthält i.W. 2 Datenspalten durch Tabulator getrennt: Entfernung in Metern und Höhe in Metern.

Beispiel: Höhenglättung

```
rem ===== SlidingMean mit 5 Punkten
gpxtool --output=test.kmz heightsimplify=SlidingMean --heightwidth=5 --output=dest.gpx inp1.gpx
rem ===== SlidingIntegral mit 150m Breite
gpxtool --heightsimplify --heightwidth=150 --output=dest.gpx inp1.gpx
rem ===== Höhendatenexport der geglätteten Daten
gpxtool --heightsimplify --heightoutput=height.txt inp1.gpx
```

Zur Beseitigung von "Ausreißern" bei den Höhenangaben kann ein max. gültiger An- bzw. Abstieg in Prozent angegeben werden. Außerdem wird die Breite (Weglänge) des Bereiches benötigt, der durch seinen Anfangs- und Endpunkt den Standard-Anstieg festlegt. Dieser Bereich wird schrittweise über den Track verschoben. Alle Punkte innerhalb des Bereiches, die den max. An-/Abstieg übersteigen erhalten dann diesen Standard-Anstieg. Ist die Bereichsbreite 0, wird diese Funktion abgeschaltet.

   `gpxtool --heightoutlierwidth=100 --maxascent=20 --output=dest.gpx inp1.gpx`

### Daten für KML/KMZ-Ausgabe

Als Argument kann eine Liste aus Farb- und Breitenangaben für Tracks festgelegt werden. Eine Farbe wird entweder als ARGB oder RGB-Wert in Hexadezimalschreibweise mit führendem '#' angegeben. Die Breite ist eine positive ganze Zahl.

Beispiel: 

```
rem leicht transparentes helles Orange für Track1 (und alle folgenden)
gpxtool --output=test.kmz --kmltrackdata=#D0FF8000,4 test.gpx
rem leicht transparentes helles Orange für Track1, helles Orange für Track2 (und alle folgenden)
gpxtool --output=test.kmz --kmltrackdata=#D0FF8000,4,#FF8000,6 test.gpx
```

## Verarbeitungsreihenfolge

Die Verarbeitung erfolgt je nach gewählten Optionen der Reihe nach in folgenden Schritten:
  * Zusammenfügen der Input-Dateien 
  * Entfernung unerwünschter Wegpunkte, Routen und Tracks 
  * Interpolation fehlender Höhen- und Zeitangaben in den Tracks 
  * Löschung aller Höhenangaben in den Tracks 
  * konstante Höhe setzen bzw. Minimal- und/oder Maximalhöhe setzen 
  * Löschung aller Zeitangaben in den Tracks 
  * Geschwindigkeitsausreißer löschen 
  * "Rastplatz"-Vereinfachung (ev. mit Protokollierung) 
  * Trackvereinfachung 
  * Ausreißerbeseitung für Höhenangaben 
  * Höhenglättung 
  * Erzeugung der Profildatei 
  * Erzeugung der Ergebnisdatei 
