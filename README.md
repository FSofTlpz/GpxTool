# GpxTool
This tool is for getting informations and processing GPX-files.

Mit dem Kommandozeilenprogramm k�nnen Informationen aus einer GPX-Datei geliefert werden und Manipulationen mit GPX-Dateien erfolgen. Die Steuerung erfolgt �ber die Angabe von Optionen und Parametern.

 
-i, --info=arg   Ausgabe von Waypoint-, Routen-, Track- und Segment-Infos auf STDOUT (Name, L�nge usw.) (Standard: true)  
--withsubdirs=arg   bei Verwendung von * oder ? werden Eingabedateien auch in Unterverzeichnissen gesucht  
-n, --name=arg   neuer Trackname (mehrfach verwendbar f�r Track 1 usw.)  
-o, --output=arg   Name der Ausgabedatei f�r die (ev. ver�nderten) GPX-Daten (auch KML/KMZ Datei m�glich)  
--overwrite=arg   eine ev. schon vorhandene GPX-Datei darf �berschrieben werden (ohne arg 'true', Standard 'false')  
--simplifygpx=arg   GPX-Datei vereinfachen (ohne arg 'true', Standard: 'false', bei mehreren Dateien immer true)  
-f, --formatted=arg   Ausgabe formatiert oder '1-zeilig' (ohne arg 'true', Standard: 'false')  
-t, --tracks=arg   Liste (mit Komma) der zu verwendenden Tracknummern (1, ...) (Standard: alle)  
-r, --routes=arg   Liste (mit Komma) der zu verwendenden Routennummern (1, ...) (Standard: alle)  
-p, --waypoints=arg   Liste (mit Komma) der zu verwendenden Waypoints (1, ...) (Standard: alle)  
--segment2track=arg   zus�tzliche Segmente in eigene Tracks umwandeln (ohne arg 'true', Standard: 'false')  
--deletetime=arg   alle Zeitstempel werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')  
--deleteheight=arg   alle H�henangaben werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')  
-G, --gapfill   fehlende H�henwerte und Zeitstempel linear interpolieren (ohne arg 'true', Standard: 'false')  
-N, --newheigth=arg   alle H�hen werden in den Trackpunkten auf einen konstanten Wert gesetzt  
-s, --simplify[=arg]   Vereinfachung der Tracks [mit Algorithmus Reumann-Witkam (RW) oder Douglas-Peucker (DP)] (Standard: keine)  
-w, --width=arg   Breite des Toleranzbereiches f�r die Vereinfachung (Standard 0.05)  
-m, --maxspeed=arg   Punkte entfernen, die mit einer h�heren Geschwindigkeit erreicht werden (in km/h; Standard: inaktiv)  
-a, --restarea=arg   Werteliste (mit Komma) f�r die Pausenplatzeliminierung, z.B. 10,1,20,60,2,25,50 (Standard: inaktiv)  
--restareaprot=arg   Name der Protokolldatei f�r die Pausenplatzeliminierung (liefert je Zeile den Original-Punktindex, die Anzahl der Polygonkreuzungen, den Radius des kleinsten Umkreises, den durchschnittlichen Abweichungswinkel und 1, wenn der Punkt gel�scht wurde)  
-O, --heightoutput=arg   Name der Ausgabedatei f�r die (ev. ver�nderten) H�hen-Daten in Abh�ngigkeit der jeweiligen Trackl�nge  
--minheight=arg   Minimalh�he; alle kleineren H�hen werden damit ersetzt  
--maxheight=arg   Maximalh�he; alle gr��eren H�hen werden damit ersetzt  
-S, --heightsimplify[=arg]   H�henprofil vereinfachen [mit Algorithmus SlidingIntegral (SI) oder SlidingMean (SM)] (Standard: keine)  
-W, --heightwidth=arg   Breite des H�hen-Integrationsbereiches in Metern (Standard 100)  
-U, --heightoutlierwidth=arg   Breite des Bereiches f�r die 'Ausrei�er'-Korrektur von H�hen (Standard 50m)  
-A, --maxascent=arg   max. g�ltiger An-/Abstieg in Prozent (Standard 25%)  
--filenametotrackname=arg   Tracknamen auf den Dateinamen setzen (Standard: false)  
--onefilepertrack=arg   jeden Track in einer eigenen Datei ausgeben (Standard: false)  
--kmltrackdata=arg   Farbe und Linienbreite f�r jeden Track bei KML-Ausgabe (Liste aus jeweils ARGB/RGB-Farbe und Breite)  
-?, --help   diese Hilfe   

Au�erdem ben�tigt das Programm mindestens 1 Argument. Die Argumente sind die Dateinamen der zu verarbeitenden GPX-Dateien. Werden mehrere Dateien verwendet, werden diese vor der weiteren Verarbeitung zusammengef�gt. Die Waypoints, Routen und Tracks sind dann entsprechend der Dateireihenfolge durchnummeriert. Enth�lt die erste und zweite Datei z.B. jeweils 2 Tracks, haben die Tracks der zweiten Datei dann die Nummern 3 und 4.

Beispiel: GPX-Dateien verbinden

gpxtool --output=dest.gpx inp1.gpx inp2.gpx inp3.gpx

Neue Tracknamen k�nnen folgenderma�en vergeben werden:

Beispiel: Tracknamen �ndern

gpxtool --name="Track 1" --name="Track 2" --output=dest.gpx inp1.gpx

Die Menge der auszugebenden Wegpunkte, Routen oder Tracks kann eingeschr�nkt oder ganz verhindert werden.

Beispiel: keine Wegpunkte und Routen ausgeben, nur Tracks 1 und 3 ausgeben

gpxtool --waypoints --routes --trakcks=1,3 --output=dest.gpx inp1.gpx

Gibt es f�r einige Trackpunkte keine H�henangaben und/oder keine Zeitangaben, k�nnen diese interpoliert werden. Das funktioniert nicht in jedem Fall. Bei der Vereinfachung des H�henprofils wird diese Option automatisch gesetzt.

Beispiel: fehlende H�hen- und/oder Zeitangaben interpolieren

gpxtool --gapfill --output=dest.gpx inp1.gpx

Trackvereinfachung und Beseitigung von Ausrei�ern

Tracks k�nnen mit 2 verschiedenen Algorithmen vereinfacht werden. Der Douglas-Peucker scheint i.A. die besseren Ergebnisse zu liefern und ist deshalb das Standardverfahren. Reumann-Witkam soll bei verschlungenen Tracks bessere Ergebnisse liefern.

Bei Douglas-Peucker wird ausgehend vom ganzen Track rekursiv f�r immer kleinere Teilstrecken gepr�ft, ob die zwischen 2 Punkten liegenden Punkte alle innerhalb eines Toleranzbereiches (rechteckiger Streifen) befinden. Ist das der Fall, entfallen die inneren Punkte. Andernfalls werden mit den "zu weit weg liegenden" Punkten neue Teilstrecken gebildet und die Untersuchung wiederholt.

Bei Reumann-Witkam werden iterativ alle Punktpaare (i, i+1) untersucht. Liegen direkt folgende Punkte i+2, ... innerhalb eines Toleranzbereiches (rechteckiger Streifen entsprechend der Punkte i und i+1), entfallen diese Punkte.

Der entscheidende Parameter ist in jedem Fall die Breite des Toleranzbereiches. Intern ist er mit 0,05 vordefiniert und hat damit i.A. nur eine sehr geringe Auswirkung. Je breiter dieser Bereich ist, je st�rker wird ein Track vereinfacht. Problematisch sind in jedem Fall einzelne "Ausreisser". Diese mit der Vereinfachung nat�rlich nicht  beseitigt und sollten vorher mit einem Editor f�r GPX-Tracks entfernt werden.

Beispiel: Trackvereinfachung

   rem ===== Douglas-Peucker mit 0,05
   gpxtool --simplify --output=dest.gpx inp1.gpx
   rem ===== Reumann-Witkam mit 4,2
   gpxtool --simplify=RW --width=4.2 --output=dest.gpx inp1.gpx

Einzelne Ausrei�er (fehlerhafte Messungen) verraten sich manchmal dadurch, dass sich f�r das Teilst�ck zum vorhergehenden Punkt eine deutlich h�here Geschwindigkeit ergibt. Man kann einen Grenzwert f�r die erlaubte Geschwindgkeit angeben. Punkte die mit h�herer Geschwindgkeit erreicht werden, werden entfernt.

   rem ===== max. erlaubte Geschwindgkeit 10,5km/h
   gpxtool --maxspeed=10.5

Rastplatzerkennung

Experimentell ist die "Rastplatz"-Vereinfachung. Damit sollen i.W. Teile im Track entfernt werden, die keine reale Bewegung darstellen. W�hrend einer l�ngeren Rast zeichnet ein GPS-Ger�t z.B. weitere Punkte auf, die sehr nahe am Rastplatz liegen. Das k�nnen in ung�nstigen F�llen mehrere 100 Meter sein.

Als Parameter wird ein Liste von nat�rlichen Zahlen ben�tigt: p,k1,r1,g1,k2,r2,g2. Mit der Punkanzahl p wird die L�nge des jeweils untersuchten Track-Teilst�ckes angegeben. K1 und k2 geben jeweils eine Anzahl vor, wie oft sich dieses Teilst�ck mit sich selbst kreuzt.K1 bedeutet dabei "k1 bis k2-1 Kreuzungen", k2 bedeutet "k2 oder mehr Kreuzungen". Je nach Anzahl der gefundenen Kreuzungen m�ssen alle p Punkte eine durchschnittliche �nderung der Bewegungsrichtung (jeweils von Punkt zu Punkt) mit dem Mindestwert g1 bzw. g2 haben und sie m�ssen alle in einen Kreis mit dem Radius r1 bzw. r2 passen.

Im folgenden Beispiel bedeutet die Parameterliste:
  * Ein Teilst�ck des Tracks ist 10 Punkte lang. (p) 
  * Es muss mindestens 1 Kreuzung vorhanden sein (k1) 
  * Der Radius des Umkreises darf h�chsten 18m sein (r1) 
  * Die durchschnittliche �nderung der Bewegungsrichtung muss mindestens 60� sein (g1) 
  * Es m�ssen mindestens 2 Kreuzungen vorhanden sein (k2). 
  * Der Radius des Umkreises darf h�chsten 25m sein (r2) 
  * Die durchschnittliche �nderung der Bewegungsrichtung muss mindestens 50� sein (g2) 
Wenn diese Bedingungen zutreffen, werden alle p Punkte bis auf einen gel�scht. Es bleibt nur der Punkt erhalten, der am dichtesten am Mittelpunkt des zutreffenden Umkreises liegt.

Beispiel: Rastplatzerkennung

   gpxtool --restarea=10,1,18,60,2,25,50

H�hengl�ttung und Beseitigung von Ausrei�ern

Die H�henangaben von Tracks k�nnen "gegl�ttet" werden. Daf�r stehen 2 Verfahren zur Verf�gung: SlidingIntegral und SlidingMean.

Bei SlidingMean wird f�r jeden Punkt der gleitende Mittelwert der H�hen der benachbarten Punkte gebildet. Dabei wird auch die jeweilige Streckenl�nge mit einbezogen (gewichtet). Die Anzahl der einzubeziehenden Punkte ist der entscheidende Parameter. Bei einer geradzahligen Anzahl wird wird vor dem zu berechnenden Punkt ein Punkt mehr ber�cksichtigt als nach diesem Punkt.

Bei SlidingIntegral wird die H�he f�r einen Punkt als durchschnittliche H�he in einem Entfernungsbereich um diesen Punkt ermittelt. Die gesamte Breite dieses Bereiches in Metern wird als Parameter angegeben. Diese Variante scheint die besten Ergebnisse zu liefern.

F�r beide Varianten wird vor der eigentlichen Gl�ttung eine D�mpfung f�r Ausreisser vorgenommen. Daf�r wird ein max. g�ltiger An- bzw. Abstieg in Prozent angegeben. Wird dieser Anstieg �berschritten, wird die entsprechende H�he als Mittelwert der vorhergehenden Punkte neu berechnet. Ist das nicht gew�nscht, sollte unrealistisch hoher Wert festgelegt werden. Vordefiniert sind 25%, d.h. 25m Anstieg auf 100m Entfernung.

Zur Erzeugung von H�henprofilen k�nnen die daf�r n�tigen Daten exportiert werden. Die Exportdatei enth�lt i.W. 2 Datenspalten durch Tabulator getrennt: Entfernung in Metern und H�he in Metern.

Beispiel: H�hengl�ttung

   rem ===== SlidingMean mit 5 Punkten
   gpxtool --output=test.kmz heightsimplify=SlidingMean --heightwidth=5 --output=dest.gpx inp1.gpx
   rem ===== SlidingIntegral mit 150m Breite
   gpxtool --heightsimplify --heightwidth=150 --output=dest.gpx inp1.gpx
   rem ===== H�hendatenexport der gegl�tteten Daten
   gpxtool --heightsimplify --heightoutput=height.txt inp1.gpx

Zur Beseitigung von "Ausrei�ern" bei den H�henangaben kann ein max. g�ltiger An- bzw. Abstieg in Prozent angegeben werden. Au�erdem wird die Breite (Wegl�nge) des Bereiches ben�tigt, der durch seinen Anfangs- und Endpunkt den Standard-Anstieg festlegt. Dieser Bereich wird schrittweise �ber den Track verschoben. Alle Punkte innerhalb des Bereiches, die den max. An-/Abstieg �bersteigen erhalten dann diesen Standard-Anstieg. Ist die Bereichsbreite 0, wird diese Funktion abgeschaltet.

   gpxtool --heightoutlierwidth=100 --maxascent=20 --output=dest.gpx inp1.gpx

Daten f�r KML/KMZ-Ausgabe

Als Argument kann eine Liste aus Farb- und Breitenangaben f�r Tracks festgelegt werden. Eine Farbe wird entweder als ARGB oder RGB-Wert in Hexadezimalschreibweise mit f�hrendem '#' angegeben. Die Breite ist eine positive ganze Zahl.

Beispiel: 

   rem leicht transparentes helles Orange f�r Track1 (und alle folgenden)
   gpxtool --output=test.kmz --kmltrackdata=#D0FF8000,4 test.gpx
   rem leicht transparentes helles Orange f�r Track1, helles Orange f�r Track2 (und alle folgenden)
   gpxtool --output=test.kmz --kmltrackdata=#D0FF8000,4,#FF8000,6 test.gpx
 

Verarbeitungsreihenfolge

Die Verarbeitung erfolgt je nach gew�hlten Optionen der Reihe nach in folgenden Schritten:
  * Zusammenf�gen der Input-Dateien 
  * Entfernung unerw�nschter Wegpunkte, Routen und Tracks 
  * Interpolation fehlender H�hen- und Zeitangaben in den Tracks 
  * L�schung aller H�henangaben in den Tracks 
  * konstante H�he setzen bzw. Minimal- und/oder Maximalh�he setzen 
  * L�schung aller Zeitangaben in den Tracks 
  * Geschwindigkeitsausrei�er l�schen 
  * "Rastplatz"-Vereinfachung (ev. mit Protokollierung) 
  * Trackvereinfachung 
  * Ausrei�erbeseitung f�r H�henangaben 
  * H�hengl�ttung 
  * Erzeugung der Profildatei 
  * Erzeugung der Ergebnisdatei 
