using FSofTUtils;
using FSofTUtils.Geography;
using FSofTUtils.Geography.PoorGpx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace GpxTool {
   class Program {

      /*
         GpxTool 22.9.2020, Copyright © FSofT 2014

         GpxTool[Optionen] gpx - input...
         -i, --info=arg                 Ausgabe von Waypoint-, Routen-, Track- und Segment-Infos auf STDOUT (Name, Länge usw.)
                                        (Standard: true)
         --withsubdirs=arg              bei Verwendung von * oder ? werden Eingabedateien auch in Unterverzeichnissen gesucht
         -n, --name=arg                 neuer Trackname (mehrfach verwendbar für Track 1 usw.)
         -o, --output=arg               Name der Ausgabedatei für die (ev. veränderten) GPX-Daten
         --overwrite=arg                eine ev. schon vorhandene GPX-Datei darf überschrieben werden (ohne arg 'true', Standard 'false')
         --simplifygpx=arg              GPX-Datei vereinfachen (ohne arg 'true', Standard: 'false', bei mehreren Dateien immer true)
         -f, --formated=arg             Ausgabe formatiert oder '1-zeilig' (ohne arg 'true', Standard: 'false')
         -t, --tracks=arg               Liste (mit Komma) der zu verwendenden Tracknummern (1, ...) (Standard: alle)
         -r, --routes=arg               Liste (mit Komma) der zu verwendenden Routennummern (1, ...) (Standard: alle)
         -p, --waypoints=arg            Liste (mit Komma) der zu verwendenden Waypoints (1, ...) (Standard: alle)
         --segment2track=arg            zusätzliche Segmente in eigene Tracks umwandeln (ohne arg 'true', Standard: 'false')
         --deletetime=arg               alle Zeitstempel werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')
         --deleteheight=arg             alle Höhenangaben werden aus den Trackpunkten entfernt (ohne arg 'true', Standard: 'false')
         -G, --gapfill=arg              fehlende Höhenwerte und Zeitstempel linear interpolieren  (ohne arg 'true', Standard: 'false')
         -N, --newheigth=arg            alle Höhen werden in den Trackpunkten auf einen konstanten Wert gesetzt
         -s, --simplify=arg             Vereinfachung der Tracks [mit Algorithmus Reumann-Witkam (RW) oder Douglas-Peucker (DP)]
                                        (Standard: keine)
         -w, --width=arg                Breite des Toleranzbereiches für die Vereinfachung (Standard 0.05)
         -m, --maxspeed=arg             Punkte entfernen, die mit einer höheren Geschwindigkeit in km/h erreicht werden
                                        (Standard: inaktiv)
         -a, --restarea=arg             Werteliste (mit Komma) für die Pausenplatzeliminierung, z.B. 10,1,20,60,2,25,50
                                        (Standard: inaktiv)
         --restareaprot=arg             Name der Protokolldatei für die Pausenplatzeliminierung
         -O, --heightoutput=arg         Name der Ausgabedatei für die (ev. veränderten) Höhen-Daten in Abhängigkeit
                                        der jeweiligen Tracklänge
         --minheight=arg                Minimalhöhe; alle kleineren Höhen werden damit ersetzt
         --maxheight=arg                Maximalhöhe; alle größeren Höhen werden damit ersetzt
         -S, --heightsimplify=arg       Höhenprofil vereinfachen [mit Algorithmus SlidingIntegral (SI) oder SlidingMean (SM)]
                                        (Standard: keine)
         -W, --heightwidth=arg          Breite des Höhen-Integrationsbereiches in Metern (Standard 100m)
         -U, --heightoutlierwidth=arg   Länge des Bereiches für die 'Ausreißer'-Korrektur von Höhen (Standard 50m)
         -A, --maxascent=arg            max. gültiger An-/Abstieg in Prozent (Standard 25%)
         --filenametotrackname=arg      Tracknamen auf den Dateinamen setzen (Standard: false)
         --onefilepertrack=arg          jeden Track in einer eigenen Datei ausgeben (Standard: false)
         --kmltrackdata=arg             Farbe und Linienbreite für jeden Track bei KML-Ausgabe (Liste aus jeweils ARGB/RGB-Farbe und Breite)
         -?, --help                     diese Hilfe
      
       * Bei der Angabe mehrerer Input-Dateien werden diese zunächst verbunden.

       * Es wird NICHT das GPX-Schema geprüft. Wegen ev. anwendungsspezifischen Erweiterungen wäre das vermutlich nicht sehr sinnvoll.
       * Deshalb bleibt die ursprüngliche XML-Datei immer so weit wie möglich erhalten!
       * 
       * GPX-Dateien, die Mapsource nicht laden kann, entsprechen i.A. tatsächlich nicht dem gültigen GPX-Normen. 
       * Wie könnte man alle ungültigen Elemente herausfiltern?
       * 
       * Laut Definition GPX 1.1:
       *    <ele> ist (wenn vorhanden) immer das 1 Element unterhalb von <trkpt> (decimal in m)
       *    <time> ist (wenn vorhanden) immer das 2 Element unterhalb von <trkpt> (dateTime in UTC)
       *    <name> ist (wenn vorhanden) immer das 1 Element unterhalb von <trk>
       * 
      */

      class GpxPointExt : GpxTrackPoint {

         /// <summary>
         /// für kumulierte Längen
         /// </summary>
         public double Length;
         /// <summary>
         /// Daten geändert?
         /// </summary>
         public bool Changed;
         /// <summary>
         /// Punkt gelöscht?
         /// </summary>
         public bool Deleted;

         public GpxPointExt() {
            Length = NOTVALID_DOUBLE;
            Changed = false;
            Deleted = false;
         }

         public GpxPointExt(GpxTrackPoint pt)
            : base(pt) {
            Length = NOTVALID_DOUBLE;
            Changed = false;
            Deleted = false;
         }

      }

      /// <summary>
      /// gesammelte Infos zu einer GPX-Datei (i.A. nur für die Anzeige bestimmt)
      /// </summary>
      class GpxInfos {

         public class TrackInfo {

            public class SegmentInfo {
               /// <summary>
               /// Anzahl der Punkte im Segment
               /// </summary>
               public int PointCount { get; }
               /// <summary>
               /// Länge in m
               /// </summary>
               public readonly double Length;
               /// <summary>
               /// niedrigste Höhe in m
               /// </summary>
               public readonly double Minheight;
               /// <summary>
               /// höchste Höhe in m
               /// </summary>
               public readonly double Maxheight;
               /// <summary>
               /// Gesamtabstieg in m
               /// </summary>
               public readonly double Descent;
               /// <summary>
               /// Gesamtanstieg in m
               /// </summary>
               public readonly double Ascent;
               /// <summary>
               /// Durchschnittsgeschwindigkeit in m/s
               /// </summary>
               public readonly double AverageSpeed;


               public SegmentInfo(GpxFile gpx, int trackno, int segmentno) {
                  Length = GetLengthAndMinMaxHeight(gpx.GetTrackSegmentPointList(trackno, segmentno, false),
                                                    out Minheight,
                                                    out Maxheight,
                                                    out Descent,
                                                    out Ascent,
                                                    out AverageSpeed);
                  PointCount = gpx.TrackSegmentPointCount(trackno, segmentno);
               }

               /// <summary>
               /// liefert die Länge und die min. und max. Höhe der Punktliste (falls vorhanden, sonst double.MaxValue bzw. double.MinValue)
               /// </summary>
               /// <param name="pt"></param>
               /// <param name="minheight"></param>
               /// <param name="maxheight"></param>
               /// <param name="descent"></param>
               /// <param name="ascent"></param>
               /// <param name="averagespeed">Durchschnittsgeschwindigkeit in m/s</param>
               /// <returns></returns>
               double GetLengthAndMinMaxHeight(List<GpxTrackPoint> pt,
                                               out double minheight,
                                               out double maxheight,
                                               out double descent,
                                               out double ascent,
                                               out double averagespeed) {
                  minheight = double.MaxValue;
                  maxheight = double.MinValue;
                  descent = 0;
                  ascent = 0;
                  averagespeed = 0;

                  double length = 0;
                  if (pt.Count > 0) {
                     if (pt[0].Elevation != BaseElement.NOTVALID_DOUBLE) {
                        minheight = Math.Min(pt[0].Elevation, minheight);
                        maxheight = Math.Max(pt[0].Elevation, maxheight);
                     }

                     int starttimeidx = -1;
                     int endtimeidx = -1;
                     if (pt[0].Time != BaseElement.NOTVALID_TIME)
                        starttimeidx = endtimeidx = 0;

                     for (int p = 1; p < pt.Count; p++) {
                        if (starttimeidx < 0 && pt[p].Time != BaseElement.NOTVALID_TIME)
                           starttimeidx = endtimeidx = p;
                        else {
                           if (pt[p].Time != BaseElement.NOTVALID_TIME)
                              endtimeidx = p;
                        }

                        if (pt[p].Elevation != BaseElement.NOTVALID_DOUBLE) {
                           minheight = Math.Min(pt[p].Elevation, minheight);
                           maxheight = Math.Max(pt[p].Elevation, maxheight);
                           if (pt[p - 1].Elevation != BaseElement.NOTVALID_DOUBLE) {
                              double diff = pt[p].Elevation - pt[p - 1].Elevation;
                              if (diff > 0)
                                 descent += diff;
                              else
                                 ascent -= diff;
                           }
                        }
                     }

                     length = GpxFileSpecial.GetLength(pt);

                     if (starttimeidx >= 0 &&
                         endtimeidx >= 0) {
                        TimeSpan ts = pt[endtimeidx].Time.Subtract(pt[starttimeidx].Time);
                        double l = GpxFileSpecial.GetLength(pt, starttimeidx, endtimeidx);
                        averagespeed = l / ts.TotalSeconds;
                     }

                  }
                  return length;
               }

            }

            public string Trackname { get; }

            public int SegmentCount { get; }

            public List<SegmentInfo> Segment { get; }


            public TrackInfo(GpxFile gpx, int trackno) {
               Trackname = gpx.GetTrackname(trackno);
               SegmentCount = gpx.TrackSegmentCount(trackno);
               Segment = new List<SegmentInfo>();
               for (int i = 0; i < SegmentCount; i++)
                  Segment.Add(new SegmentInfo(gpx, trackno, i));
            }

         }

         public string Filename { get; }

         public int WaypointCount { get; }

         public List<string> Waypointname { get; }

         public int RouteCount { get; }

         public List<string> Routename { get; }

         public int TrackCount { get; }

         public List<TrackInfo> Tracks { get; }


         public GpxInfos(GpxFile gpx, bool multitasking = false) {
            Filename = gpx.Filename;

            WaypointCount = gpx.WaypointCount();

            Waypointname = new List<string>();
            for (int i = 0; i < WaypointCount; i++)
               Waypointname.Add(gpx.GetWaypointname(i));

            RouteCount = gpx.RouteCount();

            Routename = new List<string>();
            for (int i = 0; i < RouteCount; i++)
               Routename.Add(gpx.GetRoutename(i));

            TrackCount = gpx.TrackCount();

            Tracks = new List<TrackInfo>();

            if (!multitasking || TrackCount <= 1) {

               for (int i = 0; i < TrackCount; i++)
                  Tracks.Add(new TrackInfo(gpx, i));

            } else {

               TrackInfo[] tmptrackinfo = new TrackInfo[TrackCount];

               TaskQueue tq = new TaskQueue();
               IProgress<string> progress = new Progress<string>(TaskProgress4TrackInfo);
               for (int i = 0; i < tmptrackinfo.Length; i++)
                  tq.StartTask(gpx, tmptrackinfo, i, tmptrackinfo.Length, TaskWorker4TrackInfo, progress, null);
               tq.Wait4EmptyQueue();

               for (int i = 0; i < tmptrackinfo.Length; i++)
                  Tracks.Add(tmptrackinfo[i]);

            }
         }

         public override string ToString() {
            return string.Format("{0}: {1} Waypoints, {2} Routen, {3} Tracks", Filename, WaypointCount, RouteCount, TrackCount);
         }

      }


      #region Einlesen mit Multitasking

      static GpxFile[] gpxfiles = null;

      static int TaskWorker4Read(string filename, int idx, CancellationTokenSource cancel, IProgress<string> progress) {
         gpxfiles[idx] = new GpxFile(filename) {
            InternalGpx = GpxFile.InternalGpxForm.OnlyPoorGpx // GpxFile.InternalGpxForm.NormalAndPoor;
         };
         TaskProgress4Read(string.Format("lese {0} ...", filename));
         gpxfiles[idx].Read();
         return 0;
      }

      static void TaskProgress4Read(string txt) {
         Console.Error.WriteLine(txt);
      }

      #endregion

      #region Infos ermitteln mit Multitasking

      static int TaskWorker4Info(GpxFile gpx, GpxInfos[] infos, int idx, CancellationTokenSource cancel, IProgress<string> progress) {
         TaskProgress4Info(string.Format("ermittle Infos zu {0} ...", gpx.Filename));
         infos[idx] = new GpxInfos(gpx);
         TaskProgress4Info(string.Format("Infos zu {0} ermittelt", gpx.Filename));
         return 0;
      }

      static void TaskProgress4Info(string txt) {
         Console.Error.WriteLine(txt);
      }

      #endregion

      #region Trackinfos ermitteln mit Multitasking

      static int TaskWorker4TrackInfo(GpxFile gpx, GpxInfos.TrackInfo[] trackinfos, int idx, int gesamt, CancellationTokenSource cancel, IProgress<string> progress) {
         TaskProgress4TrackInfo(string.Format("ermittle Infos zu Track {0} von {1} ...", idx + 1, gesamt));
         trackinfos[idx] = new GpxInfos.TrackInfo(gpx, idx);
         return 0;
      }

      static void TaskProgress4TrackInfo(string txt) {
         Console.Error.WriteLine(txt);
      }

      #endregion



      static void Main(string[] args) {

         Assembly a = Assembly.GetExecutingAssembly();
         string title = ((AssemblyProductAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyProductAttribute)))).Product + " " +
                        ((AssemblyInformationalVersionAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyInformationalVersionAttribute)))).InformationalVersion + ", " +
                        ((AssemblyCopyrightAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyCopyrightAttribute)))).Copyright;
         Console.Error.WriteLine(title);
         Console.Error.WriteLine();

         Options opt = new Options();
         try {
            opt.Evaluate(args);
         } catch (Exception ex) {
            Console.Error.WriteLine("Exception beim Ermitteln der Programmoptionen: " + ex.Message);
            return;
         }

         if (opt.Inputfiles.Count > 0) {

            List<string> Inputfiles = PrepareInputData(opt.Inputfiles, opt.InputWithSubdirs); // ev. Unterverzeichnisse und Wildcards einbeziehen

            try {
               GpxFileSpecial gpxfile = ReadAndMergeGpxfiles(Inputfiles, opt.Outputfile, opt.ShowInfo, opt.SimplifyGPX, title);

               if (opt.Outputfile.Length > 0) {

                  if (File.Exists(opt.Outputfile) && !opt.OutputOverwrite) {
                     Console.Error.WriteLine("Die Datei '" + opt.Outputfile + "' existiert schon, darf aber nicht überschrieben werden.");
                  } else {

                     ProcessGpxfile(gpxfile, opt);

                     if (opt.OneFilePerTrack)
                        SplitAndSaveGpxfile(gpxfile, opt.OneFilePerTrack, opt.Outputfile, opt.SimplifyGPX, opt.FormatedOutput, Inputfiles.Count, opt.KmlTrackdata);
                     else
                        SaveGpxfile(gpxfile, opt.Outputfile, opt.SimplifyGPX, opt.FormatedOutput, Inputfiles.Count, opt.KmlTrackdata);

                  }
               }

            } catch (Exception ex) {
               Console.Error.WriteLine(ex.Message);
            }

         }
      }

      /// <summary>
      /// liest (und fügt die Dateien zusammen) und zeigt ev. Infos der Dateien an
      /// </summary>
      /// <param name="Inputfiles">Liste der Eingabedateien</param>
      /// <param name="destfile">Name der Zieldatei</param>
      /// <param name="showinfo">Dateiinfos anzeigen</param>
      /// <param name="simplify">GPX vereinfachen</param>
      /// <param name="gpxcreator">Creator der neuen Datei</param>
      /// <returns></returns>
      static GpxFileSpecial ReadAndMergeGpxfiles(List<string> Inputfiles, string destfile, bool showinfo, bool simplify, string gpxcreator) {
         GpxFileSpecial gpxfile = null;

         if (Inputfiles.Count == 1) {

            gpxfile = new GpxFileSpecial(Inputfiles[0]) {
               InternalGpx = simplify ?
                                 GpxFile.InternalGpxForm.OnlyPoorGpx :
                                 GpxFile.InternalGpxForm.NormalAndPoor
            };
            gpxfile.Read();

            if (showinfo)
               ShowInfoOnStdOut(new GpxInfos(gpxfile, true));

         } else { // > 1, dann implizit GpxFile.InternalGpxForm.OnlyPoorGpx bilden

            gpxfiles = new GpxFile[Inputfiles.Count];

            TaskQueue tq = new TaskQueue();
            IProgress<string> progress = new Progress<string>(TaskProgress4Read);
            for (int i = 0; i < Inputfiles.Count; i++)
               tq.StartTask(Inputfiles[i], i, TaskWorker4Read, progress, null);
            tq.Wait4EmptyQueue();
            if (tq.ExeptionMessage != "")
               throw new Exception(tq.ExeptionMessage);

            if (showinfo) {
               GpxInfos[] infos = new GpxInfos[gpxfiles.Length];
               progress = new Progress<string>(TaskProgress4Info);
               for (int i = 0; i < gpxfiles.Length; i++)
                  tq.StartTask(gpxfiles[i], infos, i, TaskWorker4Info, progress, null);
               tq.Wait4EmptyQueue();
               if (tq.ExeptionMessage != "")
                  throw new Exception(tq.ExeptionMessage);

               for (int i = 0; i < infos.Length; i++)
                  ShowInfoOnStdOut(infos[i]);
            }

            // alle Dateien zusammenfügen
            if (destfile.Length > 0) {
               gpxfile = new GpxFileSpecial(Inputfiles[0], gpxcreator) {
                  InternalGpx = GpxFile.InternalGpxForm.OnlyPoorGpx
               }; // nur Name der 1. Datei, aber nicht einlesen !
               for (int i = 0; i < Inputfiles.Count; i++) {
                  Console.Error.WriteLine("füge Daten aus {0} ein", Inputfiles[i]);
                  gpxfile.Concat(gpxfiles[i]);
               }
            }

         }

         return gpxfile;
      }

      /// <summary>
      /// verarbeitet die GPX-Datei entsprechend der Anforderungen
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="opt"></param>
      static void ProcessGpxfile(GpxFileSpecial gpxfile, Options opt) {
         gpxfile.RemoveElements(opt.Output4Waypoints, GpxFileSpecial.ObjectType.Waypoint);
         gpxfile.RemoveElements(opt.Output4Routes, GpxFileSpecial.ObjectType.Route);
         gpxfile.RemoveElements(opt.Output4Tracks, GpxFileSpecial.ObjectType.Track);

         List<string> NewTrackName = new List<string>();
         if (opt.NewTrackName != null)
            NewTrackName.AddRange(opt.NewTrackName);

         if (opt.Filename2TrackName) { // noch nicht explizit gesetzte Tracknamen auf den Dateinamen setzen
            int trackcount = gpxfile.TrackCount();
            string trackname = Path.GetFileNameWithoutExtension(opt.Outputfile);
            if (NewTrackName.Count == trackcount - 1)
               NewTrackName.Add(trackname);
            else { // ein Zähler ist wegen eindeutiger Tracknamen nötig
               int count = 1;
               while (NewTrackName.Count < trackcount) {
                  NewTrackName.Add(trackname + " (" + count.ToString() + ")");
                  count++;
               }
            }
         }

         if (NewTrackName.Count > 0)
            gpxfile.SetTracknames(NewTrackName);

         if (opt.Segment2Track) {
            gpxfile.Segment2Track2();
         }

         if (opt.GapFill)
            gpxfile.GapFill();

         if (opt.DeleteHeight)
            gpxfile.DeleteTrackHeight();

         if (opt.ConstantHeight != double.MinValue ||
             opt.MinHeight != double.MinValue ||
             opt.MaxHeight != double.MaxValue)
            gpxfile.HeightSetting(opt.ConstantHeight, opt.MinHeight, opt.MaxHeight);

         if (opt.DeleteTimestamp)
            gpxfile.DeleteTrackTimestamp();

         if (opt.HorizontalMaxSpeed > 0)
            gpxfile.RemoveOutlier(opt.HorizontalMaxSpeed);

         if (opt.HorizontalRestArea != null &&
             opt.HorizontalRestArea.Length == 7)
            gpxfile.RemoveRestingplace(opt.HorizontalRestArea[0],
                                       opt.HorizontalRestArea[1], opt.HorizontalRestArea[2], opt.HorizontalRestArea[3],
                                       opt.HorizontalRestArea[4], opt.HorizontalRestArea[5], opt.HorizontalRestArea[6],
                                       opt.HorizontalRestAreaProt);

         if (opt.HorizontalSimplification != Options.HSimplification.Nothing)
            gpxfile.HorizontalSimplification(opt.HorizontalSimplification, opt.HorizontalWidth);

         if (opt.VerticalOutlierWidth > 0)
            gpxfile.RemoveHeigthOutlier(opt.VerticalOutlierWidth, opt.MaxAscent);

         if (opt.VerticalSimplification != Options.VSimplification.Nothing)
            gpxfile.VerticalSimplification(opt.VerticalSimplification, opt.VerticalWidth);

         if (opt.HeightOutputfile.Length > 0) {
            if (File.Exists(opt.HeightOutputfile) && !opt.OutputOverwrite)
               Console.Error.WriteLine("Die Datei '" + opt.HeightOutputfile + "' existiert schon, darf aber nicht überschrieben werden.");
            else
               gpxfile.SaveHeight(opt.HeightOutputfile);
         }
      }

      /// <summary>
      /// aus der GPX-Datei einzelne Objekte jeweils als eigene Datei speichern
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="onefilepertrack">jeden Track einzeln speichern</param>
      static void SplitAndSaveGpxfile(GpxFileSpecial gpxfile, 
                                      bool onefilepertrack, 
                                      string destfile, 
                                      bool simplify, 
                                      bool formated, 
                                      int inputcount,
                                      List<Options.KmlTrackData> kmltrackdata) {
         List<GpxFileSpecial> gpxfiles = new List<GpxFileSpecial>() { gpxfile };
         List<string> destfiles = new List<string>() { destfile };

         if (onefilepertrack) {
            int track = 0;
            while (gpxfile.TrackCount() > 0) {
               string trackname = gpxfile.GetTrackname(0);

               // trackname "bereinigen": alles außer Buchstaben, Ziffern und () entfernen
               //trackname.Replace()
               trackname = Regex.Replace(trackname, "[^a-zA-Z0-9ßäöüÄÖÜ=\\s{\\[\\]}(),!;\\.\\+\\-#\\*]", "_"); // alle nichterlaubten Zeichen durch '_' ersetzen


               string trackdestfile = Path.Combine(Path.GetDirectoryName(destfile), Path.GetFileNameWithoutExtension(destfile) + "_" + track.ToString() + "_" + trackname + ".gpx"); // Nummer wegen eindeutiger Namen

               GpxFileSpecial gpxfiletrack = new GpxFileSpecial(trackdestfile, gpxfile.gpxcreator);
               gpxfiletrack.InsertTrack(0, gpxfile, 0);
               gpxfile.DeleteTrack(0);

               destfiles.Add(gpxfiletrack.Filename);
               gpxfiles.Add(gpxfiletrack);

               track++;
            }
         }

         SaveGpxfile(gpxfiles, destfiles, simplify, formated, inputcount, kmltrackdata);
      }

      static void SaveGpxfile(List<GpxFileSpecial> gpxfiles, 
                              List<string> destfiles, 
                              bool simplify, 
                              bool formated,
                              int inputcount,
                              List<Options.KmlTrackData> kmltrackdata) {
         int count = Math.Min(gpxfiles.Count, destfiles.Count);
         for (int i = 0; i < count; i++)
            SaveGpxfile(gpxfiles[i], destfiles[i], simplify, formated, inputcount, kmltrackdata);
      }

      /// <summary>
      /// speichert eine GPX-Datei
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="destfile"></param>
      /// <param name="simplify"></param>
      /// <param name="formated"></param>
      /// <param name="inputcount"></param>
      /// <param name="kmltrackdata"></param>
      static void SaveGpxfile(GpxFileSpecial gpxfile,
                              string destfile,
                              bool simplify,
                              bool formated,
                              int inputcount,
                              List<Options.KmlTrackData> kmltrackdata) {
         Console.Error.WriteLine("speichere Ergebnis in {0} ...", destfile);

         uint[] cola = new uint[kmltrackdata.Count];
         uint[] colr = new uint[kmltrackdata.Count];
         uint[] colg = new uint[kmltrackdata.Count];
         uint[] colb = new uint[kmltrackdata.Count];
         uint[] width = new uint[kmltrackdata.Count];
         for (int i = 0; i < kmltrackdata.Count; i++) {
            cola[i] = kmltrackdata[i].ColorA;
            colr[i] = kmltrackdata[i].ColorR;
            colg[i] = kmltrackdata[i].ColorG;
            colb[i] = kmltrackdata[i].ColorB;
            width[i] = kmltrackdata[i].LineWidth;
         }

         if (simplify ||
             gpxfile.InternalGpx == GpxFile.InternalGpxForm.OnlyPoorGpx ||
             inputcount > 1) {
            gpxfile.SavePoorGpx(destfile, 
                                formated, 
                                simplify ? 1 : int.MaxValue,
                                cola,
                                colr,
                                colg,
                                colb,
                                width);
         } else {
            gpxfile.Save(destfile, 
                         formated,
                         cola,
                         colr,
                         colg,
                         colb,
                         width);
         }
      }


      /// <summary>
      /// erzeugt die Liste <see cref="InputFiles"/> aller Input-Dateien
      /// <para>Wildcards im Dateinamen (NICHT Pfad) werden ausgewertet.</para>
      /// <para>Mehrfach angegebene Dateien werden entfernt.</para>
      /// <para>Es werden die vollständigen Dateipfade ergänzt.</para>
      /// <para>Die Reihenfolge der Dateinamen bleibt erhalten.</para>
      /// </summary>
      /// <param name="input"></param>
      /// <param name="withsubdirs"></param>
      /// <param name="wildcards"></param>
      /// <returns>true, wenn min. 1 Datei angegeben ist</returns>
      static List<string> PrepareInputData(IList<string> input, bool withsubdirs, char[] wildcards = null) {
         List<string> InputFiles = new List<string>();
         if (wildcards == null)
            wildcards = new char[] { '?', '*' };

         // Wenn ein Pfad oder eine Maske angeben ist, werden die einzelnen Dateien ermittelt.
         foreach (string inp in input) {
            string file = Path.GetFileName(inp);

            if (file.IndexOfAny(wildcards) >= 0) {    // Wildcards beziehen sich nur auf Dateien
               string filepath = Path.GetDirectoryName(inp);
               if (filepath == "")
                  filepath = ".";
               InputFiles.AddRange(Directory.GetFiles(filepath, file, withsubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
               continue;
            }

            file = Path.GetFullPath(inp);          // <- löst auch eine Exception bei ungültigem Pfadaufbau aus (illegale Zeichen o.ä.)

            if (Directory.Exists(file)) {
               InputFiles.AddRange(Directory.GetFiles(file, "*.*", withsubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));  // alle Dateien im Verzeichnis und in den Unterverzeichnissen
               continue;
            }

            if (File.Exists(file)) {
               InputFiles.Add(file);
               continue;
            }

            Console.Error.WriteLine("Der Input '" + inp + "' existiert nicht und wird ignoriert.");
         }

         // Dubletten entfernen
         for (int i = 0; i < InputFiles.Count; i++) {
            string txt = InputFiles[i].ToUpper();
            for (int j = i + 1; j < InputFiles.Count; j++)
               if (InputFiles[j].ToUpper() == txt)
                  InputFiles.RemoveAt(j--);
         }

         for (int i = 0; i < InputFiles.Count; i++)
            InputFiles[i] = Path.GetFullPath(InputFiles[i]);

         if (InputFiles.Count == 0)
            Console.Error.WriteLine("Keine Daten zur Verarbeitung angegeben.");

         Console.WriteLine("Anzahl der Eingabedateien: {0}", InputFiles.Count);

         return InputFiles;
      }

      /// <summary>
      /// gibt Infos für eine GPX-Datei aus
      /// </summary>
      /// <param name="info"></param>
      static void ShowInfoOnStdOut(GpxInfos info) {
         Console.WriteLine("Datei '{0}'", info.Filename);

         Console.WriteLine("{0} Waypoint/s", info.WaypointCount);
         for (int i = 0; i < info.WaypointCount; i++)
            Console.WriteLine("   Waypoint {0}: {1}", i + 1, info.Waypointname[i]);

         Console.WriteLine("{0} Route/n", info.RouteCount);
         for (int i = 0; i < info.RouteCount; i++)
            Console.WriteLine("   Waypoint {0}: {1}", i + 1, info.Routename[i]);

         Console.WriteLine("{0} Track/s", info.TrackCount);
         for (int t = 0; t < info.TrackCount; t++) {
            GpxInfos.TrackInfo ti = info.Tracks[t];
            Console.WriteLine("   Track {0}: {1}", t + 1, ti.Trackname);

            for (int s = 0; s < info.Tracks[t].SegmentCount; s++) {
               GpxInfos.TrackInfo.SegmentInfo si = ti.Segment[s];

               Console.Write("      Segment {0}: {1} Punkte, {2:N3}km",
                              s + 1,
                              si.PointCount,
                              si.Length / 1000);
               if (si.Minheight != double.MaxValue) {
                  Console.Write(", Höhe {0:F0} .. {1:F0}m, Anstieg {2:F0}, Abstieg {3:F0}",
                                si.Minheight,
                                si.Maxheight,
                                si.Descent,
                                si.Ascent);
               }
               if (si.AverageSpeed > 0)
                  Console.Write(", Durchschnittsgeschw. {0:F1}km/h", si.AverageSpeed * 3.6);
               Console.WriteLine();
            }
         }
      }


   }
}
