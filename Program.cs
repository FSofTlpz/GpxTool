using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace GpxTool {
   class Program {

      /*
       * GpxTool [Optionen] gpx-input ...
       * 
       *    -i, --info=bool         Ausgabe von Track- und Segment-Infos (Name, Länge usw.)
       *    -n, --name=text         neuer Trackname (mehrfach verwendbar)
       *    -o, --output=file       Name der Ausgabedatei für die (ev. veränderten) GPX-Daten
       *    -O, --heightoutput=file Name der Ausgabedatei für die (ev. veränderten) Höhen-Daten in Abhängigkeit der jeweiligen Tracklänge
       *                            (Standard: Input-Datei + '.txt' bei -S)
       *    -t, --tracks=zahlen     Liste (mit Komma) der zu verwendenden Tracknummern (1, ...)
       *        --deletetime        alle Zeitstempel entfernen
       *        --deleteheight      alle Höhen entfernen
       *    -N, --newheigth=zahl    alle Höhen konstant auf einen Wert setzen
       *    -G, --gapfill           fehlende Höhenwerte und Zeitstempel linear interpolieren
       *    -s, --simplify[=name]   Vereinfachung des Tracks [mit Algorithmus Reumann-Witkam oder Douglas-Peucker (Standard)]
       *    -w, --width=zahl        Breite des Toleranzbereiches für die Vereinfachung (in m, Standard 5)
       *    -S, --heightsimplify    Höhenprofil vereinfachen (implizit -G)
       *    -W, --heightwidth=zahl  Breite des Höhen-Integrationsbereiches in Metern (Standard 250m)
       *    -A, --maxascent=zahl    max. gültiger An-/Abstieg in Prozent (Standard 25%)
       *    
       * Bei der Angabe mehrerer Input-Dateien werden diese zunächst verbunden.

       * Es wird NICHT das GPX-Schema geprüft. Wegen ev. anwendungsspezifischen Erweiterungen wäre das nicht sehr sinnvoll.
       * Deshalb bleibt die ursprüngliche XML-Datei immer so weit wie möglich erhalten!
       * 
       * Laut Definition GPX 1.1:
       *    <ele> ist (wenn vorhanden) immer das 1 Element unterhalb von <trkpt> (decimal in m)
       *    <time> ist (wenn vorhanden) immer das 2 Element unterhalb von <trkpt> (dateTime in UTC)
       *    <name> ist (wenn vorhanden) immer das 1 Element unterhalb von <trk>
       * 
      */

      enum Element {
         Track, Route, Waypoint
      }


      class GpxPointExt : GpxFile.GpxPoint {

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
            Length = GpxFile.GpxPoint.NOTVALID;
            Changed = false;
            Deleted = false;
         }

         public GpxPointExt(GpxFile.GpxPoint pt)
            : base(pt) {
            Length = GpxFile.GpxPoint.NOTVALID;
            Changed = false;
            Deleted = false;
         }

      }


      static void Main(string[] args) {

         System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
         Console.Error.WriteLine(((AssemblyProductAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyProductAttribute)))).Product + " " +
                ((AssemblyInformationalVersionAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyInformationalVersionAttribute)))).InformationalVersion + ", " +
                ((AssemblyCopyrightAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyCopyrightAttribute)))).Copyright);
         Console.Error.WriteLine();

         Options opt = new Options();
         try {
            opt.Evaluate(args);
         } catch (Exception ex) {
            Console.Error.WriteLine("Exception beim Ermitteln der Programmoptionen: " + ex.Message);
            return;
         }

         if (opt.Inputfile.Count > 0) {

            try {
               GpxFile gpxfile = new GpxFile(opt.Inputfile[0]);
               gpxfile.Read();

               if (opt.ShowInfo)
                  ShowInfoOnStdOut(gpxfile);

               // wenn mehr als eine Inputdatei gegeben ist, werden die weiteren Dateien jetzt angehängt
               for (int i = 1; i < opt.Inputfile.Count; i++) {
                  GpxFile gpx = new GpxFile(opt.Inputfile[i]);
                  gpx.Read();

                  if (opt.ShowInfo)
                     ShowInfoOnStdOut(gpx);

                  if (opt.Outputfile.Length > 0) {
                     Console.Error.WriteLine("Daten aus {0} verbinden mit Daten aus {1}", gpxfile.Filename, gpx.Filename);
                     gpxfile.Concat(gpx);
                  }
               }

               if (opt.Outputfile.Length > 0) {
                  RemoveElements(gpxfile, opt.Output4Waypoints, Element.Waypoint);
                  RemoveElements(gpxfile, opt.Output4Routes, Element.Route);
                  RemoveElements(gpxfile, opt.Output4Tracks, Element.Track);

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
                     SetTracknames(gpxfile, NewTrackName);

                  if (opt.GapFill)
                     GapFill(gpxfile);

                  if (opt.DeleteHeight)
                     DeleteTrackHeight(gpxfile);

                  if (opt.ConstantHeight != double.MinValue ||
                      opt.MinHeight != double.MinValue ||
                      opt.MaxHeight != double.MaxValue)
                     HeightSetting(gpxfile, opt.ConstantHeight, opt.MinHeight, opt.MaxHeight);

                  if (opt.DeleteTimestamp)
                     DeleteTrackTimestamp(gpxfile);

                  if (opt.HorizontalMaxSpeed > 0)
                     RemoveOutlier(gpxfile, opt.HorizontalMaxSpeed);

                  if (opt.HorizontalRestArea != null &&
                      opt.HorizontalRestArea.Length == 7)
                     RemoveRestingplace(gpxfile, opt.HorizontalRestArea[0],
                                          opt.HorizontalRestArea[1], opt.HorizontalRestArea[2], opt.HorizontalRestArea[3],
                                          opt.HorizontalRestArea[4], opt.HorizontalRestArea[5], opt.HorizontalRestArea[6],
                                          opt.HorizontalRestAreaProt);

                  if (opt.HorizontalSimplification != Options.HSimplification.Nothing)
                     HorizontalSimplification(gpxfile, opt.HorizontalSimplification, opt.HorizontalWidth);

                  if (opt.VerticalOutlierWidth > 0)
                     RemoveHeigthOutlier(gpxfile, opt.VerticalOutlierWidth, opt.MaxAscent);

                  if (opt.VerticalSimplification != Options.VSimplification.Nothing)
                     VerticalSimplification(gpxfile, opt.VerticalSimplification, opt.VerticalWidth);

                  if (opt.HeightOutputfile.Length > 0)
                     SaveHeight(gpxfile, opt.HeightOutputfile);

                  Console.Error.WriteLine("speichere Ergebnis in {0} ...", opt.Outputfile);
                  gpxfile.Save(opt.Outputfile, opt.FormatedOutput);
               }

            } catch (Exception ex) {
               Console.Error.WriteLine(ex.Message);
            }

         }
      }


      /// <summary>
      /// gibt Infos für eine GPX-Datei aus
      /// </summary>
      /// <param name="gpx"></param>
      static void ShowInfoOnStdOut(GpxFile gpx) {
         Console.WriteLine("Datei '{0}'", gpx.Filename);
         gpx.Read();
         Console.WriteLine("{0} Waypoint/s", gpx.WaypointCount());
         for (int j = 0; j < gpx.WaypointCount(); j++)
            Console.WriteLine("   Waypoint {0}: {1}", j + 1, gpx.GetWaypointname(j));
         Console.WriteLine("{0} Route/n", gpx.RouteCount());
         for (int j = 0; j < gpx.RouteCount(); j++)
            Console.WriteLine("   Waypoint {0}: {1}", j + 1, gpx.GetRoutename(j));
         Console.WriteLine("{0} Track/s", gpx.TrackCount());
         for (int t = 0; t < gpx.TrackCount(); t++) {
            Console.WriteLine("   Track {0}: {1}", t + 1, gpx.GetTrackname(t));
            for (int s = 0; s < gpx.TrackSegmentCount(t); s++) {
               double minheight, maxheight;
               double length = GetLengthAndMinMaxHeight(gpx.GetTrackSegmentPointList(t, s), out minheight, out maxheight);

               Console.Write("      Segment {0}: {1} Punkte, {2:N3}km",
                              s + 1,
                              gpx.TrackSegmentPointCount(t, s),
                              length / 1000);
               if (minheight != double.MaxValue)
                  Console.Write(", Höhe {0:F0} .. {1:F0}m",
                                 minheight,
                                 maxheight);
               Console.WriteLine();
            }
         }
      }

      /// <summary>
      /// liefert die Länge des Tracksegments
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="track"></param>
      /// <param name="segment"></param>
      /// <returns></returns>
      static double GetTrackSegmentLength(GpxFile gpxfile, int track, int segment) {
         return GetLength(gpxfile.GetTrackSegmentPointList(track, segment));
      }

      /// <summary>
      /// liefert die Länge, die sich aus einem Teil der Punktliste ergibt
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="startidx">Index des 1. Punktes</param>
      /// <param name="count">Länge des Listenteiles (i.A. min. 2 Punkte)</param>
      /// <returns></returns>
      static double GetLength(List<GpxFile.GpxPoint> pt, int startidx = 0, int count = -1) {
         count = Math.Min(count, pt.Count - startidx);
         if (count < 0)
            count = pt.Count - startidx;
         double length = 0;
         for (int p = startidx + 1; p < startidx + count; p++)
            length += PointDistance(pt[p - 1], pt[p]);
         return length;
      }

      /// <summary>
      /// liefert die Länge und die min. und max. Höhe der Punktliste (falls vorhanden, sonst double.MaxValue bzw. double.MinValue)
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="minheight"></param>
      /// <param name="maxheight"></param>
      /// <returns></returns>
      static double GetLengthAndMinMaxHeight(List<GpxFile.GpxPoint> pt, out double minheight, out double maxheight) {
         minheight = double.MaxValue;
         maxheight = double.MinValue;
         if (pt[0].Elevation != GpxFile.GpxPoint.NOTVALID) {
            minheight = Math.Min(pt[0].Elevation, minheight);
            maxheight = Math.Max(pt[0].Elevation, maxheight);
         }
         double length = 0;
         for (int p = 1; p < pt.Count; p++) {
            length += PointDistance(pt[p - 1], pt[p]);
            if (pt[p].Elevation != GpxFile.GpxPoint.NOTVALID) {
               minheight = Math.Min(pt[p].Elevation, minheight);
               maxheight = Math.Max(pt[p].Elevation, maxheight);
            }
         }
         return length;
      }


      static double GetLength(List<GpxPointExt> pt, int startidx = 0, int count = -1) {
         count = Math.Min(count, pt.Count - startidx);
         if (count < 0)
            count = pt.Count - startidx;
         double length = 0;
         for (int p = startidx + 1; p < startidx + count; p++)
            length += PointDistance(pt[p - 1], pt[p]);
         return length;
      }

      static double PointDistance(GpxFile.GpxPoint p1, GpxFile.GpxPoint p2) {
         return FSoftUtils.GeoHelper.Wgs84Distance(p1.Lon, p2.Lon, p1.Lat, p2.Lat, FSoftUtils.GeoHelper.Wgs84DistanceCompute.ellipsoid);
      }

      /// <summary>
      /// erzeugt eine Liste mt erweiterten Punktdaten
      /// </summary>
      /// <param name="pt"></param>
      /// <returns></returns>
      static List<GpxPointExt> CreateGpxPointExtList(List<GpxFile.GpxPoint> pt) {
         List<GpxPointExt> lst = new List<GpxPointExt>();
         for (int i = 0; i < pt.Count; i++)
            lst.Add(new GpxPointExt(pt[i]));
         return lst;
      }

      /// <summary>
      /// erzeugt eine Profilliste (kumulierte Entfernungen und Höhen)
      /// </summary>
      /// <param name="pt"></param>
      /// <returns></returns>
      static FSoftUtils.PolylineSimplification.SimplificationPointList CreateProfileList(List<GpxFile.GpxPoint> pt) {
         FSoftUtils.PolylineSimplification.SimplificationPointList profile = new FSoftUtils.PolylineSimplification.SimplificationPointList(pt.Count);
         profile.Set(0, 0, pt[0].Elevation);
         for (int i = 1; i < profile.Count; i++)
            profile.Set(i,
                        profile.pt[i - 1].X + PointDistance(pt[i - 1], pt[i]),
                        pt[i].Elevation);
         return profile;
      }

      static FSoftUtils.PolylineSimplification.SimplificationPointList CreateProfileList(List<GpxPointExt> pt) {
         FSoftUtils.PolylineSimplification.SimplificationPointList profile = new FSoftUtils.PolylineSimplification.SimplificationPointList(pt.Count);
         profile.Set(0, 0, pt[0].Elevation);
         for (int i = 1; i < profile.Count; i++)
            profile.Set(i,
                        profile.pt[i - 1].X + PointDistance(pt[i - 1], pt[i]),
                        pt[i].Elevation);
         return profile;
      }

      static FSoftUtils.PolylineSimplification.SimplificationPointList CreateList4Simplification(List<GpxFile.GpxPoint> pt) {
         FSoftUtils.PolylineSimplification.SimplificationPointList lst = new FSoftUtils.PolylineSimplification.SimplificationPointList(pt.Count);
         lst.Set(0, 0, 0, true);
         for (int i = 1; i < lst.Count; i++) {
            double dx, dy;
            FSoftUtils.GeoHelper.Wgs84ShortXYDelta(pt[i - 1].Lon, pt[i].Lon, pt[i - 1].Lat, pt[i].Lat, out dx, out dy);
            lst.Set(i,
                    lst.pt[i - 1].X + dx,
                    lst.pt[i - 1].Y + dy);
         }
         return lst;
      }

      /// <summary>
      /// ev. alle oder einige Elemente löschen
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="idx">Indexliste</param>
      /// <param name="type">Elementtyp</param>
      static void RemoveElements(GpxFile gpxfile, int[] idx, Element type) {
         if (idx == null)                           // alle Elemente ausgeben
            return;

         int count = 0;
         string sType = "";
         switch (type) {
            case Element.Waypoint:
               count = gpxfile.WaypointCount();
               sType = "Waypoint";
               break;

            case Element.Route:
               count = gpxfile.RouteCount();
               sType = "Route";
               break;

            case Element.Track:
               count = gpxfile.TrackCount();
               sType = "Track";
               break;
         }
         Console.Error.WriteLine("Löschung für '{0}' ...", sType);

         if (idx.Length == 0) {                     // alle Elemente löschen
            Console.Error.WriteLine("   alle {0} Elemente ...", count);
            while (count-- > 0)
               switch (type) {
                  case Element.Waypoint:
                     gpxfile.DeleteWaypoint(0);
                     break;

                  case Element.Route:
                     gpxfile.DeleteRoute(0);
                     break;

                  case Element.Track:
                     gpxfile.DeleteTrack(0);
                     break;
               }
            return;
         }

         // nur festgelegte Elemente ausgeben
         List<bool> removeidx = new List<bool>();
         for (int i = 0; i < count; i++)
            removeidx.Add(true);
         for (int i = 0; i < idx.Length; i++)
            if (0 <= idx[i] && idx[i] < count)
               removeidx[idx[i]] = false;
            else
               Console.Error.WriteLine("   {0} {1} existiert nicht für die Ausgabe (max. {2}).", sType, idx[i] + 1, count);      // nur Info
         Console.Error.Write("   ( ");
         for (int i = removeidx.Count - 1; i >= 0; i--)
            if (removeidx[i]) {
               Console.Error.Write("{0} ", i + 1);
               switch (type) {
                  case Element.Waypoint:
                     gpxfile.DeleteWaypoint(i);
                     break;

                  case Element.Route:
                     gpxfile.DeleteRoute(i);
                     break;

                  case Element.Track:
                     gpxfile.DeleteTrack(i);
                     break;
               }
            }
         Console.Error.WriteLine(")");
      }

      /// <summary>
      /// neue  Tracknamen setzen
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="Tracknames"></param>
      static void SetTracknames(GpxFile gpxfile, IList<string> Tracknames) {
         int tracks = gpxfile.TrackCount();
         for (int i = 0; i < Tracknames.Count && i < tracks; i++) {
            Console.Error.WriteLine("Track {0}: '{1}' -> '{2}'", i + 1, gpxfile.GetTrackname(i), Tracknames[i]);
            gpxfile.SetTrackname(i, Tracknames[i]);
         }
      }

      /// <summary>
      /// löscht für jeden Trackpunkt die Höhe
      /// </summary>
      /// <param name="gpxfile"></param>
      static void DeleteTrackHeight(GpxFile gpxfile) {
         Console.Error.WriteLine("lösche Höhen in den Tracks ...");
         for (int t = 0; t < gpxfile.TrackCount(); t++)
            for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++)
               for (int p = 0; p < gpxfile.TrackSegmentPointCount(t, s); p++)
                  gpxfile.ChangeTrackSegmentPointElevation(t, s, p);
      }

      /// <summary>
      /// setzt für jeden Trackpunkt die gleiche Höhe
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="constantheight"></param>
      static void HeightSetting(GpxFile gpxfile, double constantheight, double minheight, double maxheight) {
         if (constantheight != double.MinValue) {
            Console.Error.WriteLine("setze Höhen in den Tracks konstant auf {0:F0}m ...", constantheight);
            for (int t = 0; t < gpxfile.TrackCount(); t++)
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++)
                  for (int p = 0; p < gpxfile.TrackSegmentPointCount(t, s); p++)
                     gpxfile.ChangeTrackSegmentPointElevation(t, s, p, constantheight);
         } else {
            if (minheight != double.MinValue)
               Console.Error.WriteLine("setze Höhen in den Tracks auf Minimum {0:F0}m ...", minheight);
            if (maxheight != double.MaxValue)
               Console.Error.WriteLine("setze Höhen in den Tracks auf Maximum {0:F0}m ...", maxheight);

            int countmin = 0, countmax = 0;
            for (int t = 0; t < gpxfile.TrackCount(); t++)
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
                  for (int p = 0; p < gpxfile.TrackSegmentPointCount(t, s); p++) {
                     double ele = gpxfile.GetTrackSegmentPoint(t, s, p).Elevation;
                     if (ele < minheight || ele > maxheight) {
                        if (ele < minheight) {
                           ele = minheight;
                           countmin++;
                        }
                        if (ele > maxheight) {
                           ele = maxheight;
                           countmax++;
                        }
                        gpxfile.ChangeTrackSegmentPointElevation(t, s, p, ele);
                     }
                  }
                  if (countmin > 0 || countmax > 0) {
                     Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte: ", t + 1, s + 1, gpxfile.TrackSegmentPointCount(t, s));
                     Console.Error.WriteLine(string.Format("{0} Punkte auf Min. gesetzt, {1} Punkte auf Max. gesetzt", countmin, countmax));
                  }
               }


         }
      }

      /// <summary>
      /// löscht für jeden Trackpunkt die Zeit
      /// </summary>
      /// <param name="gpxfile"></param>
      static void DeleteTrackTimestamp(GpxFile gpxfile) {
         Console.Error.WriteLine("lösche Timestamps in den Tracks ...");
         for (int t = 0; t < gpxfile.TrackCount(); t++)
            for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++)
               for (int p = 0; p < gpxfile.TrackSegmentPointCount(t, s); p++)
                  gpxfile.ChangeTrackSegmentPointTime(t, s, p, GpxFile.GpxPoint.NOTVALID_TIME);
      }

      /// <summary>
      /// versucht, Lücken bei den Höhe und der Zeit der Trackpunkte zu interpolieren
      /// </summary>
      /// <param name="gpx"></param>
      static void GapFill(GpxFile gpxfile) {
         Console.Error.WriteLine("fehlende Werte für Höhen und Timestamps interpolieren ...");
         for (int t = 0; t < gpxfile.TrackCount(); t++)
            for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
               List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);
               List<GpxPointExt> lst = CreateGpxPointExtList(pt);

               int changed_h = InterpolateHeigth(lst);
               int changed_t = InterpolateTime(lst);
               Console.Error.Write("   Track {0}, Segment {1}, {2} Werte für Höhe, {3} Werte für Zeit", t + 1, s + 1, changed_h, changed_t);

               for (int i = 0; i < lst.Count; i++)
                  if (lst[i].Changed) {
                     gpxfile.ChangeTrackSegmentPointElevation(t, s, i, lst[i].Elevation);
                     gpxfile.ChangeTrackSegmentPointTime(t, s, i, lst[i].Time);
                  }

            }
      }

      /// <summary>
      /// interpoliert unbekannte Höhen
      /// </summary>
      /// <param name="lst"></param>
      /// <returns>Anzahl der interpolierten Werte</returns>
      static int InterpolateHeigth(List<GpxPointExt> lst) {
         int changed = 0;
         if (lst.Count > 0) {

            for (int i = 0; i < lst.Count; i++) {
               // Bereichsgrenzen ungültiger Höhen ermitteln
               if (lst[i].Elevation == GpxFile.GpxPoint.NOTVALID) {
                  int startidx = i;
                  int endidx = lst.Count - 1;
                  for (int j = i; j < lst.Count; j++) {
                     if (lst[j].Elevation != GpxFile.GpxPoint.NOTVALID) {
                        i = j - 1;
                        endidx = j - 1;
                        break;
                     }
                  }

                  double height1 = GpxFile.GpxPoint.NOTVALID;
                  double height2 = GpxFile.GpxPoint.NOTVALID;
                  if (startidx > 0)
                     height1 = lst[startidx - 1].Elevation;
                  if (endidx < lst.Count - 1)
                     height2 = lst[endidx + 1].Elevation;

                  if (height1 == GpxFile.GpxPoint.NOTVALID) {      // die ersten Punkte mit der ersten gültigen Höhe auffüllen (wenn vorhanden)
                     for (int k = startidx; k <= endidx; k++) {
                        lst[k].Elevation = height2;
                        lst[k].Changed = true;
                        changed++;
                     }
                  } else
                     if (height2 == GpxFile.GpxPoint.NOTVALID) {   // die letzten Punkte mit der letzten gültigen Höhe auffüllen (wenn vorhanden)
                     for (int k = startidx; k <= endidx; k++) {
                        lst[k].Elevation = height1;
                        lst[k].Changed = true;
                        changed++;
                     }
                  } else {                            // interpolieren
                     // Es wird einfach nur eine konstante Höhenänderung von Punkt zu Punkt angenommen.
                     // Andere Varianten (z.B. Länge der Teilstrecken) scheinen auch nicht sinnvoller zu sein.
                     double step = (height2 - height1) / (2 + endidx - startidx);
                     for (int k = startidx; k <= endidx; k++) {
                        lst[k].Elevation = height1 + (k - startidx + 1) * step;
                        lst[k].Changed = true;
                        changed++;
                     }
                  }
               }
            }

         }
         return changed;
      }

      /// <summary>
      /// interpoliert unbekannte Zeiten
      /// </summary>
      /// <param name="lst"></param>
      /// <returns>Anzahl der interpolierten Werte</returns>
      static int InterpolateTime(List<GpxPointExt> lst) {
         int changed = 0;
         if (lst.Count > 0) {

            for (int i = 0; i < lst.Count; i++) {
               // Bereichsgrenzen ungültiger Höhen ermitteln
               if (lst[i].Time == GpxFile.GpxPoint.NOTVALID_TIME) {
                  int startidx = i;
                  int endidx = lst.Count - 1;
                  for (int j = i; j < lst.Count; j++) {
                     if (lst[j].Time != GpxFile.GpxPoint.NOTVALID_TIME) {
                        i = j - 1;
                        endidx = j - 1;
                        break;
                     }
                  }

                  DateTime time1 = GpxFile.GpxPoint.NOTVALID_TIME;
                  DateTime time2 = GpxFile.GpxPoint.NOTVALID_TIME;
                  DateTime time3 = GpxFile.GpxPoint.NOTVALID_TIME;
                  DateTime time4 = GpxFile.GpxPoint.NOTVALID_TIME;
                  if (startidx > 1) {
                     time1 = lst[startidx - 2].Time;
                     time2 = lst[startidx - 1].Time;
                  } else
                     if (startidx > 0)
                     time2 = lst[startidx - 1].Time;

                  if (endidx < lst.Count - 2) {
                     time3 = lst[endidx + 1].Time;
                     time4 = lst[endidx + 3].Time;
                  } else
                     if (endidx < lst.Count - 1)
                     time3 = lst[endidx + 1].Time;

                  double v = 0;              // Geschwindigkeit für die Interpolation
                  if (time2 != GpxFile.GpxPoint.NOTVALID_TIME &&
                      time3 != GpxFile.GpxPoint.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden begrenzenden Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, startidx - 1, endidx - startidx + 3);
                     double sec = time3.Subtract(time2).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  } else
                     if (time1 != GpxFile.GpxPoint.NOTVALID_TIME &&
                         time2 != GpxFile.GpxPoint.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden letzten Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, startidx - 1, 2);
                     double sec = time2.Subtract(time1).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  } else
                        if (time3 != GpxFile.GpxPoint.NOTVALID_TIME &&
                            time4 != GpxFile.GpxPoint.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden ersten Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, endidx + 1, 2);
                     double sec = time4.Subtract(time3).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  }

                  if (v > 0) {            // sonst ist keine Interpolation möglich 
                     if (time2 == GpxFile.GpxPoint.NOTVALID_TIME) {        // Bereich am Anfang
                        lst[startidx].Time = time2 = time3.AddSeconds(-GetLength(lst, 0, endidx + 2) / v);
                        lst[startidx].Changed = true;
                        startidx++;
                        changed++;
                     }
                     double difflength = 0;
                     for (int k = startidx; k <= endidx; k++) {
                        difflength += GetLength(lst, k - 1, 2);
                        lst[k].Time = time2.AddSeconds(difflength / v);
                        lst[k].Changed = true;
                        changed++;
                     }
                  } else {                // Wie??? Mehrere Punkte mit identischer Zeit scheinen sinnlos (?) zu sein.


                  }

               }
            }

         }
         return changed;
      }

      /// <summary>
      /// (horizontale) Trackvereinfachung
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="type">Art des Vereinfachungalgorithmus</param>
      /// <param name="width">Streifenbreite, innerhalb der die Vereinfachung erfolgt</param>
      static void HorizontalSimplification(GpxFile gpxfile, Options.HSimplification type, double width) {
         if (type != Options.HSimplification.Nothing) {

            Console.Error.WriteLine("horizontale Track-Vereinfachung: {0}", type.ToString());
            Console.Error.WriteLine("   Breite {0}", width);
            for (int t = 0; t < gpxfile.TrackCount(); t++)
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
                  List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);
                  if (pt.Count > 2) {
                     Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte, {3:F3}km", t + 1, s + 1, pt.Count, GetLength(pt) / 1000);
                     FSoftUtils.PolylineSimplification.SimplificationPointList pl = CreateList4Simplification(pt);
                     pl.pt[pl.Count - 1].IsLocked = true;

                     switch (type) {
                        case Options.HSimplification.Reumann_Witkam:
                           pl.ReumannWitkam(width);
                           break;

                        case Options.HSimplification.Douglas_Peucker:
                           pl.DouglasPeucker(width);
                           break;
                     }

                     for (int p = pl.Count - 1; p > 0; p--)
                        if (!pl.pt[p].IsValid) {
                           gpxfile.DeleteTrackSegmentPoint(t, s, p);
                           pt.RemoveAt(p);
                        }

                     Console.Error.WriteLine(" --> {0} Punkte, {1:F3}km",
                                             gpxfile.TrackSegmentPointCount(t, s),
                                             GetLength(pt) / 1000);
                  }
               }

         }
      }

      /// <summary>
      /// Höhenglättung der Tracks
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="type">Art des Vereinfachungalgorithmus</param>
      /// <param name="width">Parameter für den Vereinfachungalgorithmus</param>
      static void VerticalSimplification(GpxFile gpxfile, Options.VSimplification type, double width) {
         if (type != Options.VSimplification.Nothing) {

            Console.Error.WriteLine("vertikale Track-Glättung: {0}", type.ToString());
            switch (type) {
               case Options.VSimplification.SlidingMean:
                  int ptcount = Math.Max(2, (int)Math.Round(width));    // >= 2
                  width = ptcount;
                  Console.Error.WriteLine("   {0} Punkte", width);
                  break;

               case Options.VSimplification.SlidingIntegral:
                  Console.Error.WriteLine("   Breite {0}m", width);
                  break;
            }

            for (int t = 0; t < gpxfile.TrackCount(); t++)
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
                  List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);

                  Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

                  bool bPointsNotValid = false;
                  for (int i = 0; i < pt.Count; i++)
                     if (pt[i].Elevation == GpxFile.GpxPoint.NOTVALID) {
                        bPointsNotValid = true;
                        break;
                     }
                  if (bPointsNotValid || pt.Count < 2) {
                     Console.Error.WriteLine(": zu wenig Punkte oder Punkte ohne Höhenangabe");
                     continue;
                  }
                  Console.Error.WriteLine();

                  // Daten übernehmen
                  FSoftUtils.PolylineSimplification.SimplificationPointList profile = CreateProfileList(pt);
                  Console.Error.WriteLine(string.Format("   Gesamtlänge:        {0:F0}m", profile.pt[profile.Count - 1].X));

                  Console.Error.WriteLine("   Ausgangsdaten:");
                  ShowHeightData(profile);

                  switch (type) {
                     case Options.VSimplification.SlidingMean:
                        profile.HeigthProfileWithSlidingMean(Math.Max(3, (int)Math.Round(width)));
                        break;

                     case Options.VSimplification.SlidingIntegral:
                        profile.HeigthProfileWithSlidingIntegral(width);
                        break;
                  }
                  Console.Error.WriteLine("   geglättete Daten:");
                  ShowHeightData(profile);

                  // Daten speichern
                  for (int p = 0; p < profile.Count; p++)
                     gpxfile.ChangeTrackSegmentPointElevation(t, s, p, profile.pt[p].Y);

               }
         }
      }

      /// <summary>
      /// Entfernung von "Ausreißer"-Höhen
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="width">Untersuchungslänge des Wegstückes</param>
      /// <param name="maxascend">max. An-/Abstieg</param>
      static void RemoveHeigthOutlier(GpxFile gpxfile, double width, double maxascend) {

         Console.Error.WriteLine("vertikale Ausreisser-Entfernung");
         Console.Error.WriteLine("   Breite {0}m, max. erlaubter Anstieg {1}%", width, maxascend);
         for (int t = 0; t < gpxfile.TrackCount(); t++)
            for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
               List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);

               Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

               bool bPointsNotValid = false;
               for (int i = 0; i < pt.Count; i++)
                  if (pt[i].Elevation == GpxFile.GpxPoint.NOTVALID) {
                     bPointsNotValid = true;
                     break;
                  }
               if (bPointsNotValid || pt.Count < 2) {
                  Console.Error.WriteLine(": zu wenig Punkte (min. 2) oder Punkte ohne Höhenangabe");
                  continue;
               }
               Console.Error.WriteLine();

               List<GpxPointExt> lst = CreateGpxPointExtList(pt);

               Console.Error.WriteLine("   Ausgangsdaten:");
               ShowHeightData(CreateProfileList(pt));

               maxascend /= 100;

               // Höhen mit einem durchschnittlichen Anstieg neu berechnen, wenn der max. Anstieg überschritten wird
               for (int i = 1; i < lst.Count; i++) {
                  double dist = PointDistance(lst[i - 1], lst[i]);
                  if (Math.Abs(lst[i].Elevation - lst[i - 1].Elevation) / dist > maxascend) {
                     double meanascend = GetMeanAscendBefore(lst, i - 1, width);
                     if (double.IsNaN(meanascend))
                        meanascend = 0;
                     double meanelevation = lst[i - 1].Elevation + dist * meanascend; // wenn es mit dem bisher mittleren Anstieg weitergehen würde

                     lst[i].Elevation -= (lst[i].Elevation - meanelevation) / 2; // auf 1/2 des zusätzl. Anstiegs abziehen -> "Ausreißer" wird gedämpft

                     //lst[i].Elevation = lst[i - 1].Elevation + dist * meanascend;
                     lst[i].Changed = true;
                  }
               }

               // Daten übernehmen
               int iChanged = 0;
               for (int i = 0; i < lst.Count; i++)
                  if (lst[i].Changed) {
                     gpxfile.ChangeTrackSegmentPointElevation(t, s, i, lst[i].Elevation);
                     iChanged++;
                  }
               Console.Error.WriteLine(string.Format("   {0}-mal zu starker An-/Abstieg korrigiert", iChanged));

               if (iChanged > 0) {
                  Console.Error.WriteLine("   korrigierte Daten:");
                  ShowHeightData(CreateProfileList(lst));
               }
            }
      }

      /// <summary>
      /// ermittelt den durchschnittlichen Anstieg bis zum Punkt mit dem Index 'start', max. aber für eine Länge 'width'
      /// <para>Voraussetzung ist, dass alle Höhen der Punkte vorher gültig sind</para>
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="start"></param>
      /// <param name="width"></param>
      /// <returns></returns>
      static double GetMeanAscendBefore(List<GpxPointExt> pt, int start, double width) {
         double meanascend = double.NaN;
         if (start > 0 &&
             width > 0) {
            double length = 0;
            double h_start = pt[start].Elevation;
            double dist = 0;
            int i;
            for (i = start; i > 0; i--) {
               dist = PointDistance(pt[i], pt[i - 1]); // Punktabstand
               length += dist;
               meanascend = (h_start - pt[i - 1].Elevation) / length; // Näherungswert
               if (length >= width)
                  break;
            }
            if (length > width && i > 0) {
               if (pt[i].Elevation != GpxFile.GpxPoint.NOTVALID &&
                   pt[i - 1].Elevation != GpxFile.GpxPoint.NOTVALID &&
                   dist > 0) { // Höhe auf letzter Teilstrecke interpolieren
                  double h = pt[i - 1].Elevation;
                  h += (length - width) / dist * (pt[i].Elevation - pt[i - 1].Elevation);
                  meanascend = (h_start - h) / width;
               }
            }
         }
         return meanascend;
      }

      /// <summary>
      /// Entfernung von "Ausreißer"-Geschwindigkeiten
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="maxv"></param>
      static void RemoveOutlier(GpxFile gpxfile, double maxv) {
         Console.Error.WriteLine("horizontale Ausreisser-Entfernung");
         Console.Error.WriteLine("   Maximalgeschwindigkeit {0}km/h", maxv);
         maxv /= 3.6;         // km/h --> m/s
         for (int t = 0; t < gpxfile.TrackCount(); t++)
            for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
               List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);

               Console.Error.WriteLine("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

               SortedList<int, int> removed = new SortedList<int, int>();
               int pa = 0;
               for (int pe = 1; pe < pt.Count; pe++) {
                  double distance = Math.Abs(PointDistance(pt[pe], pt[pa]));
                  double time = Math.Abs(pt[pe].Time.Subtract(pt[pa].Time).TotalSeconds);
                  double v = distance / time;

                  // Punkte, die mit einer Geschwindigkeit über der Maximalgeschwindigkeit erreicht werden, werden entfernt.
                  if (v > maxv)
                     removed.Add(pe, 0);
                  else
                     do {
                        pa++;
                     }
                     while (removed.ContainsKey(pa));
               }
               int[] tmp = new int[removed.Count];
               removed.Keys.CopyTo(tmp, 0);
               for (int p = tmp.Length - 1; p >= 0; p--)
                  gpxfile.DeleteTrackSegmentPoint(t, s, tmp[p]);

               Console.Error.WriteLine(string.Format("      {0} Punkte wegen zu hoher Geschwindigkeit entfernt", removed.Count));
            }
      }

      /// <summary>
      /// Entfernung von Punkten für einen "Rastplatz" (eine Mindestanzahl von aufeinanderfolgenden Punkten innerhalb eines bestimmten Radius 
      /// mit einer bestimmten durchschnittlichen Mindestrichtungsänderung)
      /// </summary>
      /// <param name="gpxfile"></param>
      /// <param name="mincount">Mindestanzahl von Punkten</param>
      /// <param name="maxradius1">Radius</param>
      /// <param name="minturnaround">durchschnittlichen Mindestrichtungsänderung</param>
      static void RemoveRestingplace(GpxFile gpxfile, int ptcount,
                                     int crossing1, double maxradius1, double minturnaround1,
                                     int crossing2, double maxradius2, double minturnaround2,
                                     string protfile = null) {
         if (ptcount >= 3 &&
             crossing1 >= 0 && maxradius1 > 0 && minturnaround1 > 0 &&
             crossing2 > 0 && maxradius2 > 0 && minturnaround2 > 0) {

            Console.Error.WriteLine("horizontale 'Rastplatz'-Entfernung");
            Console.Error.WriteLine("   Punktanzahl {0}", ptcount, crossing1, maxradius1, minturnaround1);
            Console.Error.WriteLine("      Kreuzungen {0}, Radius {1}m, durchschnittl. Richtungsänderung {2}°", crossing1, maxradius1, minturnaround1);
            Console.Error.WriteLine("      Kreuzungen {0}, Radius {1}m, durchschnittl. Richtungsänderung {2}°", crossing2, maxradius2, minturnaround2);

            for (int t = 0; t < gpxfile.TrackCount(); t++)
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
                  List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);

                  Console.Error.WriteLine("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

                  FSoftUtils.PolylineSimplification.SimplificationPointList lst = CreateList4Simplification(pt);
                  lst.RemoveRestingplace(ptcount, crossing1, maxradius1, minturnaround1, crossing2, maxradius2, minturnaround2, protfile);

                  int lastvalid = -1;
                  int lstunvalid = -1;
                  List<string> sDel = new List<string>();
                  for (int p = lst.Count - 1; p > 0; p--)
                     if (!lst.pt[p].IsValid) {
                        gpxfile.DeleteTrackSegmentPoint(t, s, p);
                        lstunvalid = p;
                     } else {
                        if (lstunvalid >= 0) {
                           sDel.Add(string.Format("{0}-{1}", lstunvalid + 1, lastvalid));
                           lstunvalid = -1;
                        }
                        lastvalid = p;
                     }
                  if (sDel.Count > 0) {
                     Console.Error.Write("   gelöscht: ");
                     for (int i = sDel.Count - 1; i >= 0; i--)
                        Console.Error.Write(sDel[i] + (i > 0 ? "," : ""));
                     Console.Error.WriteLine();
                  }

                  Console.Error.WriteLine("   --> {0} Punkte", gpxfile.TrackSegmentPointCount(t, s));

                  //Console.Error.WriteLine(string.Format("      {0} Punkte wegen zu hoher Geschwindigkeit entfernt", removed.Count));
               }
         }
      }

      /// <summary>
      /// eine einfache statistische Anzeige für die Höhen der spez. Punktliste
      /// </summary>
      /// <param name="profile"></param>
      static void ShowHeightData(FSoftUtils.PolylineSimplification.SimplificationPointList profile) {
         double dAscent = 0.0;
         double dDescent = 0.0;
         double dMaxHeigth = double.MinValue;
         double dMinHeigth = double.MaxValue;
         double dStartHeigth = double.MaxValue;
         double dEndHeigth = double.MaxValue;

         double dLastHeigth = double.MaxValue;
         for (int i = 0; i < profile.Count; i++)
            if (profile.pt[i].IsValid) {
               double heigth = profile.pt[i].Y;
               if (dLastHeigth != double.MaxValue &&
                   heigth != double.MaxValue) {
                  if (heigth - dLastHeigth > 0)
                     dAscent += heigth - dLastHeigth;
                  else
                     dDescent += dLastHeigth - heigth;
               }
               dLastHeigth = heigth;
               if (heigth != double.MaxValue) {
                  if (dMaxHeigth < heigth)
                     dMaxHeigth = heigth;
                  if (dMinHeigth > heigth)
                     dMinHeigth = heigth;
                  if (dStartHeigth == double.MaxValue)
                     dStartHeigth = heigth;
                  dEndHeigth = heigth;
               }
            }

         Console.Error.WriteLine(string.Format("      Start-/Endhöhe:    {0,6:F0} / {1,6:F0}m", dStartHeigth, dEndHeigth));
         Console.Error.WriteLine(string.Format("      Min./Max.:         {0,6:F0} / {1,6:F0}m", dMinHeigth, dMaxHeigth));
         Console.Error.WriteLine(string.Format("      Summe An-/Abstieg: {0,6:F0} / {1,6:F0}m", dAscent, -dDescent));
      }


      /// <summary>
      /// speichert die aktuellen Höhenangaben und die kumulierte Länge in einer Datei (z.B. um ein Höhenprofil zu erzeugen)
      /// <para>Vor jedem Tracksegment steht eine Zeile mit dem Tracknamen und der Segmentnummer.</para>
      /// </summary>
      /// <param name="gpx"></param>
      /// <param name="file"></param>
      static void SaveHeight(GpxFile gpxfile, string file) {
         Console.Error.WriteLine("speichere Höhenprofil in {0} ...", file);
         double length = 0;
         using (System.IO.StreamWriter stream = new System.IO.StreamWriter(file)) {
            for (int t = 0; t < gpxfile.TrackCount(); t++) {
               for (int s = 0; s < gpxfile.TrackSegmentCount(t); s++) {
                  stream.WriteLine("# Track {0} ({1}), Segment {2}", t + 1, gpxfile.GetTrackname(t), s + 1);
                  List<GpxFile.GpxPoint> pt = gpxfile.GetTrackSegmentPointList(t, s);
                  if (pt.Count > 0)
                     stream.WriteLine("0\t{0}", pt[0].Elevation != GpxFile.GpxPoint.NOTVALID ? pt[0].Elevation.ToString("f1") : "");
                  for (int p = 1; p < pt.Count; p++) {
                     length += FSoftUtils.GeoHelper.Wgs84Distance(pt[p - 1].Lon, pt[p].Lon, pt[p - 1].Lat, pt[p].Lat, FSoftUtils.GeoHelper.Wgs84DistanceCompute.ellipsoid);
                     stream.WriteLine("{0:F1}\t{1}",
                        length,
                        pt[p].Elevation != GpxFile.GpxPoint.NOTVALID ? pt[p].Elevation.ToString("f1") : "");
                  }
               }
            }
         }
      }

   }
}
