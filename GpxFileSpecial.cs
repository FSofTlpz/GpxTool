using System;
using System.Collections.Generic;
using FSofTUtils.Geography;
using FSofTUtils.Geography.PoorGpx;

namespace GpxTool {
   /// <summary>
   /// hier werden die speziellen Arbeiten an der GPX-Datei verrichtet
   /// </summary>
   class GpxFileSpecial : GpxFile {

      public enum ObjectType {
         Waypoint,
         Route,
         Track
      }

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
      /// erzeugt eine XML-Struktur ohne GPX-Daten
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="gpxcreator"></param>
      /// <param name="gpxversion"></param>
      public GpxFileSpecial(string filename, string gpxcreator = "GpxFile", string gpxversion = "1.1") :
         base(filename, gpxcreator, gpxversion) { }


      /// <summary>
      /// Ex. der Objektname schon ?
      /// </summary>
      /// <param name="name"></param>
      /// <param name="type"></param>
      /// <returns></returns>
      bool ObjectNameExists(string name, ObjectType type) {
         int count = 0;
         switch (type) {
            case ObjectType.Waypoint:
               count = WaypointCount();
               break;

            case ObjectType.Route:
               count = RouteCount();
               break;

            case ObjectType.Track:
               count = TrackCount();
               break;
         }

         for (int i = 0; i < count; i++)
            switch (type) {
               case ObjectType.Waypoint:
                  if (GetWaypointname(i) == name)
                     return true;
                  break;

               case ObjectType.Route:
                  if (GetRoutename(i) == name)
                     return true;
                  break;

               case ObjectType.Track:
                  if (GetTrackname(i) == name)
                     return true;
                  break;
            }
         return false;
      }

      /// <summary>
      /// liefert einen eindeutigen Objektnamen, der durch das Anhängen eines Zählers an den Basisnamen entsteht
      /// </summary>
      /// <param name="baseobjectname"></param>
      /// <param name="type"></param>
      /// <returns></returns>
      string CreateUniqueObjectName(string baseobjectname, ObjectType type) {
         int no = 1;
         string newobjectname = baseobjectname;
         while (ObjectNameExists(newobjectname, type)) {
            newobjectname = baseobjectname + " " + no.ToString("d2");
            no++;
         }
         return newobjectname;
      }

      /// <summary>
      /// ev. alle oder einige Elemente löschen
      /// </summary>
      /// <param name="idx">Indexliste</param>
      /// <param name="type">Elementtyp</param>
      public void RemoveElements(int[] idx, ObjectType type) {
         if (idx == null)                           // alle Elemente ausgeben
            return;

         int count = 0;
         string sType = "";
         switch (type) {
            case ObjectType.Waypoint:
               count = WaypointCount();
               sType = "Waypoint";
               break;

            case ObjectType.Route:
               count = RouteCount();
               sType = "Route";
               break;

            case ObjectType.Track:
               count = TrackCount();
               sType = "Track";
               break;
         }
         Console.Error.WriteLine("Löschung für '{0}' ...", sType);

         if (idx.Length == 0) {                     // alle Elemente löschen
            Console.Error.WriteLine("   alle {0} Elemente ...", count);
            while (count-- > 0)
               switch (type) {
                  case ObjectType.Waypoint:
                     DeleteWaypoint(0);
                     break;

                  case ObjectType.Route:
                     DeleteRoute(0);
                     break;

                  case ObjectType.Track:
                     DeleteTrack(0);
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
                  case ObjectType.Waypoint:
                     DeleteWaypoint(i);
                     break;

                  case ObjectType.Route:
                     DeleteRoute(i);
                     break;

                  case ObjectType.Track:
                     DeleteTrack(i);
                     break;
               }
            }
         Console.Error.WriteLine(")");
      }

      /// <summary>
      /// neue Tracknamen setzen
      /// </summary>
      /// <param name="Tracknames"></param>
      public void SetTracknames(IList<string> Tracknames) {
         int tracks = TrackCount();
         for (int i = 0; i < Tracknames.Count && i < tracks; i++) {
            Console.Error.WriteLine("Track {0}: '{1}' -> '{2}'", i + 1, GetTrackname(i), Tracknames[i]);
            SetTrackname(i, Tracknames[i]);
         }
      }

      /// <summary>
      /// löscht für jeden Trackpunkt die Höhe
      /// </summary>
      public void DeleteTrackHeight() {
         Console.Error.WriteLine("lösche Höhen in den Tracks ...");
         for (int t = 0; t < TrackCount(); t++)
            for (int s = 0; s < TrackSegmentCount(t); s++)
               for (int p = 0; p < TrackSegmentPointCount(t, s); p++)
                  ChangeTrackSegmentPointData(t, s, p, BaseElement.NOTUSE_TIME, BaseElement.NOTVALID_DOUBLE);
      }

      /// <summary>
      /// setzt für jeden Trackpunkt die gleiche Höhe
      /// </summary>
      /// <param name="constantheight"></param>
      public void HeightSetting(double constantheight, double minheight, double maxheight) {
         if (constantheight != double.MinValue) {
            Console.Error.WriteLine("setze Höhen in den Tracks konstant auf {0:F0}m ...", constantheight);
            for (int t = 0; t < TrackCount(); t++)
               for (int s = 0; s < TrackSegmentCount(t); s++)
                  for (int p = 0; p < TrackSegmentPointCount(t, s); p++)
                     ChangeTrackSegmentPointData(t, s, p, BaseElement.NOTUSE_TIME, constantheight);
         } else {
            if (minheight != double.MinValue)
               Console.Error.WriteLine("setze Höhen in den Tracks auf Minimum {0:F0}m ...", minheight);
            if (maxheight != double.MaxValue)
               Console.Error.WriteLine("setze Höhen in den Tracks auf Maximum {0:F0}m ...", maxheight);

            int countmin = 0, countmax = 0;
            for (int t = 0; t < TrackCount(); t++)
               for (int s = 0; s < TrackSegmentCount(t); s++) {
                  for (int p = 0; p < TrackSegmentPointCount(t, s); p++) {
                     double ele = GetTrackSegmentPoint(t, s, p).Elevation;
                     if (ele < minheight || ele > maxheight) {
                        if (ele < minheight) {
                           ele = minheight;
                           countmin++;
                        }
                        if (ele > maxheight) {
                           ele = maxheight;
                           countmax++;
                        }
                        ChangeTrackSegmentPointData(t, s, p, BaseElement.NOTUSE_TIME, ele);
                     }
                  }
                  if (countmin > 0 || countmax > 0) {
                     Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte: ", t + 1, s + 1, TrackSegmentPointCount(t, s));
                     Console.Error.WriteLine(string.Format("{0} Punkte auf Min. gesetzt, {1} Punkte auf Max. gesetzt", countmin, countmax));
                  }
               }


         }
      }

      /// <summary>
      /// löscht für jeden Trackpunkt die Zeit
      /// </summary>
      public void DeleteTrackTimestamp() {
         Console.Error.WriteLine("lösche Timestamps in den Tracks ...");
         for (int t = 0; t < TrackCount(); t++)
            for (int s = 0; s < TrackSegmentCount(t); s++)
               for (int p = 0; p < TrackSegmentPointCount(t, s); p++)
                  ChangeTrackSegmentPointData(t, s, p,
                                              BaseElement.NOTVALID_TIME,
                                              GetTrackSegmentPoint(t, s, p).Elevation);
      }

      /// <summary>
      /// versucht, Lücken bei den Höhe und der Zeit der Trackpunkte zu interpolieren
      /// </summary>
      public void GapFill() {
         Console.Error.WriteLine("fehlende Werte für Höhen und Timestamps interpolieren ...");
         for (int t = 0; t < TrackCount(); t++)
            for (int s = 0; s < TrackSegmentCount(t); s++) {
               List<GpxTrackPoint> ptlst = GetTrackSegmentPointList(t, s);
               List<GpxPointExt> lst = CreateGpxPointExtList(ptlst);

               int changed_h = InterpolateHeigth(lst);
               int changed_t = InterpolateTime(lst);
               Console.Error.Write("   Track {0}, Segment {1}, {2} Werte für Höhe, {3} Werte für Zeit", t + 1, s + 1, changed_h, changed_t);

               for (int i = 0; i < lst.Count; i++)
                  if (lst[i].Changed)
                     ChangeTrackSegmentPointData(t, s, i, lst[i].Time, lst[i].Elevation);
            }
         Console.Error.WriteLine();
      }

      /// <summary>
      /// (horizontale) Trackvereinfachung
      /// </summary>
      /// <param name="type">Art des Vereinfachungalgorithmus</param>
      /// <param name="width">Streifenbreite, innerhalb der die Vereinfachung erfolgt</param>
      public void HorizontalSimplification(Options.HSimplification type, double width) {
         if (type != Options.HSimplification.Nothing) {

            Console.Error.WriteLine("horizontale Track-Vereinfachung: {0}", type.ToString());
            Console.Error.WriteLine("   Breite {0}", width);
            for (int t = 0; t < TrackCount(); t++)
               for (int s = 0; s < TrackSegmentCount(t); s++) {
                  List<GpxTrackPoint> ptlst = GetTrackSegmentPointList(t, s);
                  if (ptlst.Count > 2) {
                     Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte, {3:F3}km", t + 1, s + 1, ptlst.Count, GetLength(ptlst) / 1000);
                     FSofTUtils.Geometry.PolylineSimplification.PointListExt pl = CreateList4Simplification(ptlst);
                     pl.Get(pl.Length - 1).IsLocked = true;

                     switch (type) {
                        case Options.HSimplification.Reumann_Witkam:
                           pl.ReumannWitkam(width);
                           break;

                        case Options.HSimplification.Douglas_Peucker:
                           pl.DouglasPeucker(width);
                           break;
                     }

                     for (int p = pl.Length - 1; p > 0; p--)
                        if (!pl.Get(p).IsValid) {
                           DeleteTrackSegmentPoint(t, s, p);
                           ptlst.RemoveAt(p);
                        }

                     Console.Error.WriteLine(" --> {0} Punkte, {1:F3}km",
                                             TrackSegmentPointCount(t, s),
                                             GetLength(ptlst) / 1000);
                  }
               }

         }
      }

      /// <summary>
      /// Höhenglättung der Tracks
      /// </summary>
      /// <param name="type">Art des Vereinfachungalgorithmus</param>
      /// <param name="width">Parameter für den Vereinfachungalgorithmus</param>
      public void VerticalSimplification(Options.VSimplification type, double width) {
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

            for (int t = 0; t < TrackCount(); t++)
               for (int s = 0; s < TrackSegmentCount(t); s++) {
                  List<GpxTrackPoint> pt = GetTrackSegmentPointList(t, s);

                  Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

                  bool bPointsNotValid = false;
                  for (int i = 0; i < pt.Count; i++)
                     if (pt[i].Elevation == GpxTrackPoint.NOTVALID_DOUBLE) {
                        bPointsNotValid = true;
                        break;
                     }
                  if (bPointsNotValid || pt.Count < 2) {
                     Console.Error.WriteLine(": zu wenig Punkte oder Punkte ohne Höhenangabe");
                     continue;
                  }
                  Console.Error.WriteLine();

                  // Daten übernehmen
                  FSofTUtils.Geometry.PolylineSimplification.PointListExt profile = CreateProfileList(pt);
                  Console.Error.WriteLine(string.Format("   Gesamtlänge:        {0:F0}m", profile.Get(profile.Length - 1).X));

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
                  for (int p = 0; p < profile.Length; p++)
                     ChangeTrackSegmentPointData(t, s, p, BaseElement.NOTUSE_TIME, profile.Get(p).Y);

               }
         }
      }

      /// <summary>
      /// Entfernung von "Ausreißer"-Höhen
      /// </summary>
      /// <param name="width">Untersuchungslänge des Wegstückes</param>
      /// <param name="maxascend">max. An-/Abstieg</param>
      public void RemoveHeigthOutlier(double width, double maxascend) {

         Console.Error.WriteLine("vertikale Ausreisser-Entfernung");
         Console.Error.WriteLine("   Breite {0}m, max. erlaubter Anstieg {1}%", width, maxascend);
         for (int t = 0; t < TrackCount(); t++)
            for (int s = 0; s < TrackSegmentCount(t); s++) {
               List<GpxTrackPoint> pt = GetTrackSegmentPointList(t, s);

               Console.Error.Write("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

               bool bPointsNotValid = false;
               for (int i = 0; i < pt.Count; i++)
                  if (pt[i].Elevation == GpxTrackPoint.NOTVALID_DOUBLE) {
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
               for (int p = 0; p < lst.Count; p++)
                  if (lst[p].Changed) {
                     ChangeTrackSegmentPointData(t, s, p, BaseElement.NOTUSE_TIME, lst[p].Elevation);
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
      /// Entfernung von "Ausreißer"-Geschwindigkeiten
      /// </summary>
      /// <param name="maxv"></param>
      public void RemoveOutlier(double maxv) {
         Console.Error.WriteLine("horizontale Ausreisser-Entfernung");
         Console.Error.WriteLine("   Maximalgeschwindigkeit {0}km/h", maxv);
         maxv /= 3.6;         // km/h --> m/s
         for (int t = 0; t < TrackCount(); t++)
            for (int s = 0; s < TrackSegmentCount(t); s++) {
               List<GpxTrackPoint> pt = GetTrackSegmentPointList(t, s);

               Console.Error.WriteLine("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, pt.Count);

               Dictionary<int, int> removed = new Dictionary<int, int>();
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
                  DeleteTrackSegmentPoint(t, s, tmp[p]);

               Console.Error.WriteLine(string.Format("      {0} Punkte wegen zu hoher Geschwindigkeit entfernt", removed.Count));
            }
      }

      /// <summary>
      /// Entfernung von Punkten für einen "Rastplatz" (eine Mindestanzahl von aufeinanderfolgenden Punkten innerhalb eines bestimmten Radius 
      /// mit einer bestimmten durchschnittlichen Mindestrichtungsänderung)
      /// </summary>
      /// <param name="mincount">Mindestanzahl von Punkten</param>
      /// <param name="maxradius1">Radius</param>
      /// <param name="minturnaround">durchschnittlichen Mindestrichtungsänderung</param>
      public void RemoveRestingplace(int ptcount,
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

            for (int t = 0; t < TrackCount(); t++)
               for (int s = 0; s < TrackSegmentCount(t); s++) {
                  List<GpxTrackPoint> ptlst = GetTrackSegmentPointList(t, s);

                  Console.Error.WriteLine("   Track {0}, Segment {1}, {2} Punkte", t + 1, s + 1, ptlst.Count);

                  FSofTUtils.Geometry.PolylineSimplification.PointListExt lst = CreateList4Simplification(ptlst);
                  lst.RemoveRestingplace(ptcount, crossing1, maxradius1, minturnaround1, crossing2, maxradius2, minturnaround2, protfile);

                  int lastvalid = -1;
                  int lstunvalid = -1;
                  List<string> sDel = new List<string>();
                  for (int p = lst.Length - 1; p > 0; p--)
                     if (!lst.Get(p).IsValid) {
                        DeleteTrackSegmentPoint(t, s, p);
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

                  Console.Error.WriteLine("   --> {0} Punkte", TrackSegmentPointCount(t, s));

                  //Console.Error.WriteLine(string.Format("      {0} Punkte wegen zu hoher Geschwindigkeit entfernt", removed.Count));
               }
         }
      }

      /// <summary>
      /// speichert die aktuellen Höhenangaben und die kumulierte Länge in einer Datei (z.B. um ein Höhenprofil zu erzeugen)
      /// <para>Vor jedem Tracksegment steht eine Zeile mit dem Tracknamen und der Segmentnummer.</para>
      /// </summary>
      /// <param name="file"></param>
      public void SaveHeight(string file) {
         Console.Error.WriteLine("speichere Höhenprofil in {0} ...", file);
         double length = 0;
         using (System.IO.StreamWriter stream = new System.IO.StreamWriter(file)) {
            for (int t = 0; t < TrackCount(); t++) {
               for (int s = 0; s < TrackSegmentCount(t); s++) {
                  stream.WriteLine("# Track {0} ({1}), Segment {2}", t + 1, GetTrackname(t), s + 1);
                  List<GpxTrackPoint> pt = GetTrackSegmentPointList(t, s, false);
                  if (pt.Count > 0)
                     stream.WriteLine("0\t{0}", pt[0].Elevation != BaseElement.NOTVALID_DOUBLE ? pt[0].Elevation.ToString("f1") : "");
                  for (int p = 1; p < pt.Count; p++) {
                     length += GeoHelper.Wgs84Distance(pt[p - 1].Lon, pt[p].Lon, pt[p - 1].Lat, pt[p].Lat, GeoHelper.Wgs84DistanceCompute.ellipsoid);
                     stream.WriteLine("{0:F1}\t{1}",
                        length,
                        pt[p].Elevation != BaseElement.NOTVALID_DOUBLE ? pt[p].Elevation.ToString("f1") : "");
                  }
               }
            }
         }
      }

      /// <summary>
      /// Tracks mit mehreren Segmenten splitten
      /// </summary>
      public void Segment2Track2() {
         int trackcount = TrackCount();
         for (int t = 0; t < trackcount; t++) {
            int tracksegmentcount = TrackSegmentCount(t);
            if (tracksegmentcount > 1) {
               GpxTrack tr = GetPoorTrack(t, true);
               if (tr == null) {
                  tr = new GpxTrack {
                     Name = GetTrackname(t)
                  };

                  while (TrackSegmentCount(t) > 1) {
                     GpxTrackSegment ts = new GpxTrackSegment();
                     ts.Points.AddRange(GetTrackSegmentPointList(t, 1));
                     tr.Segments.Add(ts);
                  }

               }
               while (TrackSegmentCount(t) > 1)
                  DeleteSegment(t, 1);

               for (int s = 0; s < tr.Segments.Count; s++) {
                  GpxTrack newtr = new GpxTrack();
                  newtr.Segments.Add(tr.Segments[s]);
                  newtr.Name = CreateUniqueObjectName(newtr.Name, ObjectType.Track);
                  InsertTrack(t + 1 + s, newtr);
               }

               t += tracksegmentcount - 1;
               trackcount += tracksegmentcount - 1;
            }
         }
      }

      #region static-Hilfsfunktionen

      /// <summary>
      /// liefert die Länge, die sich aus einem Teil der Punktliste ergibt
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="startidx">Index des 1. Punktes</param>
      /// <param name="count">Länge des Listenteiles (i.A. min. 2 Punkte)</param>
      /// <returns></returns>
      public static double GetLength(List<GpxTrackPoint> pt, int startidx = 0, int count = -1) {
         count = Math.Min(count, pt.Count - startidx);
         if (count < 0)
            count = pt.Count - startidx;
         double length = 0;
         for (int p = startidx + 1; p < startidx + count; p++)
            length += PointDistance(pt[p - 1], pt[p]);
         return length;
      }

      /// <summary>
      /// Punktabstand
      /// </summary>
      /// <param name="p1"></param>
      /// <param name="p2"></param>
      /// <returns></returns>
      public static double PointDistance(GpxTrackPoint p1, GpxTrackPoint p2) {
         return GeoHelper.Wgs84Distance(p1.Lon, p2.Lon, p1.Lat, p2.Lat, GeoHelper.Wgs84DistanceCompute.ellipsoid);
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

      /// <summary>
      /// erzeugt eine Liste mt erweiterten Punktdaten
      /// </summary>
      /// <param name="pt"></param>
      /// <returns></returns>
      static List<GpxPointExt> CreateGpxPointExtList(List<GpxTrackPoint> pt) {
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
      static FSofTUtils.Geometry.PolylineSimplification.PointListExt CreateProfileList(List<GpxTrackPoint> pt) {
         FSofTUtils.Geometry.PolylineSimplification.PointListExt profile = new FSofTUtils.Geometry.PolylineSimplification.PointListExt(pt.Count);
         profile.Set(0, 0, pt[0].Elevation);
         for (int i = 1; i < profile.Length; i++)
            profile.Set(i,
                        profile.Get(i - 1).X + PointDistance(pt[i - 1], pt[i]),
                        pt[i].Elevation);
         return profile;
      }

      static FSofTUtils.Geometry.PolylineSimplification.PointListExt CreateProfileList(List<GpxPointExt> pt) {
         FSofTUtils.Geometry.PolylineSimplification.PointListExt profile = new FSofTUtils.Geometry.PolylineSimplification.PointListExt(pt.Count);
         profile.Set(0, 0, pt[0].Elevation);
         for (int i = 1; i < profile.Length; i++)
            profile.Set(i,
                        profile.Get(i - 1).X + PointDistance(pt[i - 1], pt[i]),
                        pt[i].Elevation);
         return profile;
      }

      static FSofTUtils.Geometry.PolylineSimplification.PointListExt CreateList4Simplification(List<GpxTrackPoint> pt) {
         FSofTUtils.Geometry.PolylineSimplification.PointListExt lst = new FSofTUtils.Geometry.PolylineSimplification.PointListExt(pt.Count);
         lst.Set(0, 0, 0);
         lst.Get(0).IsLocked = true;
         for (int i = 1; i < lst.Length; i++) {
            GeoHelper.Wgs84ShortXYDelta(pt[i - 1].Lon, pt[i].Lon, pt[i - 1].Lat, pt[i].Lat, out double dx, out double dy);
            lst.Set(i,
                    lst.Get(i - 1).X + dx,
                    lst.Get(i - 1).Y + dy);
         }
         return lst;
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
               if (lst[i].Elevation == GpxTrackPoint.NOTVALID_DOUBLE) {
                  int startidx = i;
                  int endidx = lst.Count - 1;
                  for (int j = i; j < lst.Count; j++) {
                     if (lst[j].Elevation != GpxTrackPoint.NOTVALID_DOUBLE) {
                        i = j - 1;
                        endidx = j - 1;
                        break;
                     }
                  }

                  double height1 = GpxTrackPoint.NOTVALID_DOUBLE;
                  double height2 = GpxTrackPoint.NOTVALID_DOUBLE;
                  if (startidx > 0)
                     height1 = lst[startidx - 1].Elevation;
                  if (endidx < lst.Count - 1)
                     height2 = lst[endidx + 1].Elevation;

                  if (height1 == GpxTrackPoint.NOTVALID_DOUBLE) {      // die ersten Punkte mit der ersten gültigen Höhe auffüllen (wenn vorhanden)
                     for (int k = startidx; k <= endidx; k++) {
                        lst[k].Elevation = height2;
                        lst[k].Changed = true;
                        changed++;
                     }
                  } else
                     if (height2 == GpxTrackPoint.NOTVALID_DOUBLE) {   // die letzten Punkte mit der letzten gültigen Höhe auffüllen (wenn vorhanden)
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
               if (lst[i].Time == BaseElement.NOTVALID_TIME) {
                  int startidx = i;
                  int endidx = lst.Count - 1;
                  for (int j = i; j < lst.Count; j++) {
                     if (lst[j].Time != BaseElement.NOTVALID_TIME) {
                        i = j - 1;
                        endidx = j - 1;
                        break;
                     }
                  }

                  DateTime time1 = BaseElement.NOTVALID_TIME;
                  DateTime time2 = BaseElement.NOTVALID_TIME;
                  DateTime time3 = BaseElement.NOTVALID_TIME;
                  DateTime time4 = BaseElement.NOTVALID_TIME;
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
                  if (time2 != BaseElement.NOTVALID_TIME &&
                      time3 != BaseElement.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden begrenzenden Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, startidx - 1, endidx - startidx + 3);
                     double sec = time3.Subtract(time2).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  } else
                     if (time1 != BaseElement.NOTVALID_TIME &&
                         time2 != BaseElement.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden letzten Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, startidx - 1, 2);
                     double sec = time2.Subtract(time1).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  } else
                        if (time3 != BaseElement.NOTVALID_TIME &&
                            time4 != BaseElement.NOTVALID_TIME) {
                     // Geschwindigkeit aus den beiden ersten Punkten mit gültiger Zeit bestimmen
                     double length = GetLength(lst, endidx + 1, 2);
                     double sec = time4.Subtract(time3).TotalSeconds;
                     if (length > 0 && sec > 0)
                        v = length / sec;
                  }

                  if (v > 0) {            // sonst ist keine Interpolation möglich 
                     if (time2 == BaseElement.NOTVALID_TIME) {        // Bereich am Anfang
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
               if (pt[i].Elevation != BaseElement.NOTVALID_DOUBLE &&
                   pt[i - 1].Elevation != BaseElement.NOTVALID_DOUBLE &&
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
      /// eine einfache statistische Anzeige für die Höhen der spez. Punktliste
      /// </summary>
      /// <param name="profile"></param>
      static void ShowHeightData(FSofTUtils.Geometry.PolylineSimplification.PointListExt profile) {
         double dAscent = 0.0;
         double dDescent = 0.0;
         double dMaxHeigth = double.MinValue;
         double dMinHeigth = double.MaxValue;
         double dStartHeigth = double.MaxValue;
         double dEndHeigth = double.MaxValue;

         double dLastHeigth = double.MaxValue;
         for (int i = 0; i < profile.Length; i++)
            if (profile.Get(i).IsValid) {
               double heigth = profile.Get(i).Y;
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

      #endregion


   }
}
