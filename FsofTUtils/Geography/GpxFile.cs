using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using FSofTUtils.Geography.PoorGpx;

namespace FSofTUtils.Geography {

   /// <summary>
   /// Klasse zur Verwaltung einer GPX-Datei
   /// </summary>
   public class GpxFile {

      /// <summary>
      /// XPath's zu einigen Elementen
      /// </summary>
      class XPaths {

         public static string Master() {
            return "/x:gpx";
         }

         #region Objekt-Anzahl

         /// <summary>
         /// Anzahl der Waypoints
         /// </summary>
         /// <returns></returns>
         public static string Count_Waypoint() {
            return "/x:gpx/x:wpt";
         }
         /// <summary>
         /// Anzahl der Routen
         /// </summary>
         /// <returns></returns>
         public static string Count_Route() {
            return "/x:gpx/x:rte";
         }
         /// <summary>
         /// Anzahl der Tracks
         /// </summary>
         /// <returns></returns>
         public static string Count_Track() {
            return "/x:gpx/x:trk";
         }
         /// <summary>
         /// Anzahl der Links im Track
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string Count_TrackLink(int track) {
            return Track(track) + "/x:link";
         }
         /// <summary>
         /// Anzahl der Segmente im Track
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string Count_TrackSegment(int track) {
            return Track(track) + "/x:trkseg";
         }
         /// <summary>
         /// Anzahl der Punkte im Segmente eines Tracks
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <returns></returns>
         public static string Count_TrackSegmentPoint(int track, int segment) {
            return TrackSegment(track, segment) + "/x:trkpt";
         }

         #endregion

         #region GPX-Ebene

         /// <summary>
         /// Metadaten der Datei
         /// </summary>
         /// <returns></returns>
         public static string Metadata() {
            return "/x:gpx/x:metadata";
         }
         /// <summary>
         /// Waypoint
         /// </summary>
         /// <param name="waypoint">Waypointnummer (0...)</param>
         /// <returns></returns>
         public static string Waypoint(int waypoint) {
            return "/x:gpx/x:wpt[" + (++waypoint).ToString() + "]";
         }
         /// <summary>
         /// Route
         /// </summary>
         /// <param name="route">Routennummer (0...)</param>
         /// <returns></returns>
         public static string Route(int route) {
            return "/x:gpx/x:rte[" + (++route).ToString() + "]";
         }
         /// <summary>
         /// Track
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string Track(int track) {
            return "/x:gpx/x:trk[" + (++track).ToString() + "]";
         }

         #endregion

         #region Track-Ebene

         /// <summary>
         /// Track-Segment
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegment(int track, int segment) {
            return Track(track) + "/x:trkseg[" + (++segment).ToString() + "]";
         }
         /// <summary>
         /// Trackname
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackName(int track) {
            return Track(track) + "/x:name";
         }
         /// <summary>
         /// Trackkommentar
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackComment(int track) {
            return Track(track) + "/x:cmt";
         }
         /// <summary>
         /// Trackbeschreibung
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackDescription(int track) {
            return Track(track) + "/x:desc";
         }
         /// <summary>
         /// Trackquelle
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackSource(int track) {
            return Track(track) + "/x:src";
         }
         /// <summary>
         /// Tracklink
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="link">Linknummer (0..)</param>
         /// <returns></returns>
         public static string TrackLink(int track, int link) {
            return Track(track) + "/x:link[" + (++link).ToString() + "]";
         }
         /// <summary>
         /// Tracknummer
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackNumber(int track) {
            return Track(track) + "/x:number";
         }
         /// <summary>
         /// Tracktyp
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackType(int track) {
            return Track(track) + "/x:type";
         }
         /// <summary>
         /// Trackerweiterung
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <returns></returns>
         public static string TrackExtensions(int track) {
            return Track(track) + "/x:extensions";
         }

         #endregion

         #region Segment-Ebene eines Tracks

         /// <summary>
         /// Punkt eines Track-Segments
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <param name="point">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegmentPoint(int track, int segment, int point) {
            return TrackSegment(track, segment) + "/x:trkpt[" + (++point).ToString() + "]";
         }

         #endregion

         #region Point-Ebene eines Track-Segments

         /// <summary>
         /// Latitude eines Track-Segment-Punktes
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <param name="point">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegmentPointLatitude(int track, int segment, int point) {
            return TrackSegmentPoint(track, segment, point) + "/@lat";
         }
         /// <summary>
         /// Longitude eines Track-Segment-Punktes
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <param name="point">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegmentPointLongitude(int track, int segment, int point) {
            return TrackSegmentPoint(track, segment, point) + "/@lon";
         }
         /// <summary>
         /// Höhe eines Track-Segment-Punktes
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <param name="point">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegmentPointElevation(int track, int segment, int point) {
            return TrackSegmentPoint(track, segment, point) + "/x:ele";
         }
         /// <summary>
         /// Zeit eines Track-Segment-Punktes
         /// </summary>
         /// <param name="track">Tracknummer (0...)</param>
         /// <param name="segment">Segmentnummer (0...)</param>
         /// <param name="point">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string TrackSegmentPointTime(int track, int segment, int point) {
            return TrackSegmentPoint(track, segment, point) + "/x:time";
         }

         #endregion

         #region Waypoint-Ebene

         /// <summary>
         /// Waypointname
         /// </summary>
         /// <param name="track">Waypointnummer (0...)</param>
         /// <returns></returns>
         public static string WaypointName(int wp) {
            return Waypoint(wp) + "/x:name";
         }
         /// <summary>
         /// Latitude eines Waypoint
         /// </summary>
         /// <param name="wp">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string WaypointLatitude(int wp) {
            return Waypoint(wp) + "/@lat";
         }
         /// <summary>
         /// Longitude eines Waypoint
         /// </summary>
         /// <param name="wp">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string WaypointLongitude(int wp) {
            return Waypoint(wp) + "/@lon";
         }
         /// <summary>
         /// Höhe eines Waypoint
         /// </summary>
         /// <param name="wp">Punktnummer (0...)</param>
         /// <returns></returns>
         public static string WaypointElevation(int wp) {
            return Waypoint(wp) + "/@ele";
         }
         /// <summary>
         /// Zeit eines Waypoints
         /// </summary>
         /// <param name="wp">Waypointnummer (0...)</param>
         /// <returns></returns>
         public static string WaypointPointTime(int wp) {
            return Waypoint(wp) + "/x:time";
         }

         #endregion

         #region Route-Ebene

         /// <summary>
         /// Routename
         /// </summary>
         /// <param name="track">Routenummer (0...)</param>
         /// <returns></returns>
         public static string RouteName(int r) {
            return Route(r) + "/x:name";
         }

         #endregion

      }

      /// <summary>
      /// für die Standard-GPX-Bearbeitung
      /// </summary>
      SimpleXmlDocument2 stdgpx;

      /// <summary>
      /// für die GPX-Bearbeitung um Elemente zu entfernen
      /// </summary>
      GpxAll poorgpx = null;
      public readonly string gpxcreator;
      public readonly string gpxversion;

      public enum InternalGpxForm {
         OnlyNormalGpx,
         OnlyPoorGpx,
         NormalAndPoor,
      }


      public InternalGpxForm InternalGpx { get; set; } = InternalGpxForm.NormalAndPoor;

      /// <summary>
      /// Dateiname
      /// </summary>
      public string Filename { get { return stdgpx.XmlFilename; } }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="gpxcreator"></param>
      /// <param name="gpxversion"></param>
      public GpxFile(string filename,
                     string gpxcreator = "GpxFile",
                     string gpxversion = "1.1") {
         this.gpxcreator = gpxcreator;
         this.gpxversion = gpxversion;
         stdgpx = buildDummyGpx(filename);
         if (InternalGpx != InternalGpxForm.OnlyNormalGpx)
            poorgpx = new GpxAll();
      }

      /// <summary>
      /// erzeugt nur eine XML-Basis-Struktur und registriert ev. schon einen Dateiname
      /// </summary>
      /// <param name="filename">Dateiname oder null</param>
      /// <returns></returns>
      SimpleXmlDocument2 buildDummyGpx(string filename) {
         SimpleXmlDocument2 sgpx = new SimpleXmlDocument2(filename, "gpx");

         sgpx.CreateInternData();
         sgpx.Validating = false;
         sgpx.XsdFilename = null;

         string ext = "";
         if (!string.IsNullOrEmpty(gpxversion))
            ext += "  version=\"" + gpxversion + "\"";
         if (!string.IsNullOrEmpty(gpxcreator))
            ext += "  creator=\"" + gpxcreator + "\"";

         sgpx.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
                      "<gpx xmlns=\"http://www.topografix.com/GPX/1/1\"" + ext + ">" +
                      // xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\">"+
                      "</gpx>");
         sgpx.AddNamespace("x");
         return sgpx;
      }

      /// <summary>
      /// liest die Datei ein
      /// </summary>
      /// <param name="inputstream">Falls das Objekt nicht mit einem Dateiname erzeugt wurde, ist hier ein Stream nötig.</param>
      public void Read(Stream inputstream = null) {
         if (InternalGpx != InternalGpxForm.OnlyPoorGpx) {
            if (string.IsNullOrEmpty(stdgpx.XmlFilename) && inputstream == null)
               throw new ArgumentException("Kein Input-Stream für Read() angegegben.");
            if (stdgpx.LoadData(string.IsNullOrEmpty(stdgpx.XmlFilename) ?
                                             null :
                                             inputstream))
               stdgpx.AddNamespace("x");
         }

         poorgpx = null;
         string[] tmp;

         try {

            switch (InternalGpx) {
               case InternalGpxForm.NormalAndPoor:
                  tmp = stdgpx.XReadOuterXml("/x:gpx");
                  if (tmp != null)
                     poorgpx = new GpxAll(tmp[0], true);
                  break;

               case InternalGpxForm.OnlyPoorGpx:
                  SimpleXmlDocument2 tmpgpx = buildDummyGpx(Filename);
                  if (tmpgpx.LoadData(inputstream)) {
                     tmpgpx.AddNamespace("x");
                     tmp = tmpgpx.XReadOuterXml("/x:gpx");
                     if (tmp != null)
                        poorgpx = new GpxAll(tmp[0], true);
                  }
                  break;
            }

         } catch (Exception ex) {

            throw;
         }
      }

      /// <summary>
      /// speichert die Datei
      /// </summary>
      /// <param name="filename">neuer Dateiname oder null</param>
      /// <param name="outputstream">Falls das Objekt nicht mit einem Dateiname erzeugt wurde, ist hier ein Stream nötig.</param>
      /// <param name="formatted">Ausgabe formatiert</param>
      /// <param name="kml">KML oder GPX; nur bei Stream verwendet, sonst intern aus dem Dateinamen gesetzt</param>
      /// <param name="kmz">gezippt oder nicht; nur bei Stream verwendet und nur wenn kml=true</param>
      /// <param name="kmlcola">A-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolr">R-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolg">G-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolb">B-Komponente der Farben (je Track)</param>
      /// <param name="kmlwidth">Breite je Track</param>
      public void Save(string filename = null,
                       Stream outputstream = null,
                       bool formatted = true,
                       bool kml = false,
                       bool kmz = false,
                       IList<uint> kmlcola = null,
                       IList<uint> kmlcolr = null,
                       IList<uint> kmlcolg = null,
                       IList<uint> kmlcolb = null,
                       IList<uint> kmlwidth = null) {
         save(filename,
              outputstream,
              formatted,
              kml,
              kmz,
              int.MaxValue,
              kmlcola,
              kmlcolr,
              kmlcolg,
              kmlcolb,
              kmlwidth);
      }

      /// <summary>
      /// speichert (wenn möglich) die Poor-Datei
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="outputstream"></param>
      /// <param name="formatted"></param>
      /// <param name="kml">KML oder GPX; nur bei Stream verwendet, sonst intern aus dem Dateinamen gesetzt</param>
      /// <param name="kmz">gezippt oder nicht; nur bei Stream verwendet und nur wenn kml=true</param>
      /// <param name="scale"></param>
      /// <param name="kmlcola">A-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolr">R-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolg">G-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolb">B-Komponente der Farben (je Track)</param>
      /// <param name="kmlwidth">Breite je Track</param>
      public void SavePoorGpx(string filename = null,
                              Stream outputstream = null,
                              bool formatted = true,
                              bool kml = false,
                              bool kmz = false,
                              int scale = int.MaxValue,
                              IList<uint> kmlcola = null,
                              IList<uint> kmlcolr = null,
                              IList<uint> kmlcolg = null,
                              IList<uint> kmlcolb = null,
                              IList<uint> kmlwidth = null) {
         if (poorgpx != null)
            save(filename,
                 outputstream,
                 formatted,
                 kml,
                 kmz,
                 scale,
                 kmlcola,
                 kmlcolr,
                 kmlcolg,
                 kmlcolb,
                 kmlwidth);
      }

      /// <summary>
      /// speichert die Poor-Datei
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="outputstream"></param>
      /// <param name="formatted"></param>
      /// <param name="kml">KML oder GPX; nur bei Stream verwendet, sonst intern aus dem Dateinamen gesetzt</param>
      /// <param name="kmz">gezippt oder nicht; nur bei Stream verwendet und nur wenn kml=true</param>
      /// <param name="scale"></param>
      /// <param name="kmlcola">A-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolr">R-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolg">G-Komponente der Farben (je Track)</param>
      /// <param name="kmlcolb">B-Komponente der Farben (je Track)</param>
      /// <param name="kmlwidth">Breite je Track</param>
      void save(string filename = null,
                Stream outputstream = null,
                bool formatted = true,
                bool kml = false,
                bool kmz = false,
                int scale = int.MaxValue,
                IList<uint> kmlcola = null,
                IList<uint> kmlcolr = null,
                IList<uint> kmlcolg = null,
                IList<uint> kmlcolb = null,
                IList<uint> kmlwidth = null) {
         bool isFile = !string.IsNullOrEmpty(filename);

         if (isFile) {
            kmz = Path.GetExtension(filename).ToLower() == ".kmz";
            kml = Path.GetExtension(filename).ToLower() == ".kml" ||
                  kmz;
         }

         if (kml)
            new GpxFile2KmlWriter().Write_gdal(filename,
                                               outputstream,
                                               this,                    // FALSCH ?
                                               formatted,
                                               kmz,
                                               kmlcola,
                                               kmlcolr,
                                               kmlcolg,
                                               kmlcolb,
                                               kmlwidth);
         else {

            if (scale == int.MaxValue) {

               stdgpx.SaveData(isFile ? filename : null,
                               formatted,
                               outputstream);

            } else if (poorgpx != null) {

               SimpleXmlDocument2 tmp = stdgpx;
               stdgpx = buildDummyGpx(string.IsNullOrEmpty(filename) ? Filename : filename);

               string xml = poorgpx.AsXml(scale);
               xml = xml.Substring(5, xml.Length - 11);
               stdgpx.InsertXmlText(XPaths.Master(),
                                     xml,
                                     SimpleXmlDocument2.InsertPosition.AppendChild);

               stdgpx.SaveData(isFile ? filename : null,
                               formatted,
                               outputstream);

               stdgpx = tmp;

            }
         }
      }

      #region Objektanzahl

      /// <summary>
      /// liefert die Anzahl der Waypoints
      /// </summary>
      /// <returns></returns>
      public int WaypointCount() {
         int count;
         try {
            count = poorgpx != null ?
                        poorgpx.Waypoints.Count :
                        stdgpx.NodeCount(XPaths.Count_Waypoint());
         } catch {
            count = 0;
         }
         return count;
      }

      /// <summary>
      /// liefert die Anzahl der Routen
      /// </summary>
      /// <returns></returns>
      public int RouteCount() {
         int count;
         try {
            count = poorgpx != null ?
                        poorgpx.Routes.Count :
                        stdgpx.NodeCount(XPaths.Count_Route());
         } catch {
            count = 0;
         }
         return count;
      }

      /// <summary>
      /// liefert die Anzahl der Tracks
      /// </summary>
      /// <returns></returns>
      public int TrackCount() {
         int count;
         try {
            count = poorgpx != null ?
                        poorgpx.Tracks.Count :
                        stdgpx.NodeCount(XPaths.Count_Track());
         } catch {
            count = 0;
         }
         return count;
      }

      /// <summary>
      /// liefert die Anzahl der Segmente im Track
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <returns></returns>
      public int TrackSegmentCount(int track) {
         int count = 0;
         try {
            if (poorgpx != null) {
               GpxTrack t = poorgpx.GetTrack(track);
               count = t != null ? t.Segments.Count : 0;
            } else
               count = stdgpx.NodeCount(XPaths.Count_TrackSegment(track));
         } catch {
            count = 0;
         }
         return count;
      }

      /// <summary>
      /// liefert die Anzahl der Punkte im Segment des Tracks
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      /// <returns></returns>
      public int TrackSegmentPointCount(int track, int segment) {
         int count = 0;
         try {
            if (poorgpx != null) {
               GpxTrackSegment seg = poorgpx.GetTrackSegment(track, segment);
               count = seg != null ? seg.Points.Count : 0;
            } else
               count = stdgpx.NodeCount(XPaths.Count_TrackSegmentPoint(track, segment));
         } catch {
            count = 0;
         }
         return count;
      }

      #endregion

      /// <summary>
      /// liefert einen einzelnen Punkt
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      /// <param name="point">Punktnummer (0...)</param>
      /// <returns></returns>
      public GpxTrackPoint GetTrackSegmentPoint(int track, int segment, int point) {
         if (poorgpx != null) {
            GpxTrackPoint p = GetPoorTrackSegmentPoint(track, segment, point, false);
            return p ?? new GpxTrackPoint();
         }

         GpxTrackPoint pt = new GpxTrackPoint {
            Lon = stdgpx.ReadValue(XPaths.TrackSegmentPointLongitude(track, segment, point), BaseElement.NOTVALID_DOUBLE),
            Lat = stdgpx.ReadValue(XPaths.TrackSegmentPointLatitude(track, segment, point), BaseElement.NOTVALID_DOUBLE),
            Elevation = stdgpx.ReadValue(XPaths.TrackSegmentPointElevation(track, segment, point), BaseElement.NOTVALID_DOUBLE)
         };
         string tmp = stdgpx.ReadValue(XPaths.TrackSegmentPointTime(track, segment, point), GpxTime1_0.DateTime2String(BaseElement.NOTVALID_TIME));
         pt.Time = GpxTime1_0.String2DateTime(tmp);
         return pt;
      }

      /// <summary>
      /// liefert alle Punkte eines Segmentes
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      /// <param name="ascopy">bei false direkter Zugriff auf die Originalliste</param>
      /// <returns></returns>
      public List<GpxTrackPoint> GetTrackSegmentPointList(int track, int segment, bool ascopy = true) {
         List<GpxTrackPoint> lst;
         if (poorgpx != null) {

            GpxTrackSegment seg = GetPoorTrackSegment(track, segment, ascopy);
            if (seg != null)
               return seg.Points;
            else
               return new List<GpxTrackPoint>();

         } else {

            lst = new List<GpxTrackPoint>();
            int pointcount = TrackSegmentPointCount(track, segment);
            for (int i = 0; i < pointcount; i++)
               lst.Add(GetTrackSegmentPoint(track, segment, i));

         }
         return lst;
      }

      /// <summary>
      /// liefert einen einzelnen Waypoint ein
      /// </summary>
      /// <param name="wp">Waypointnummer (0...)</param>
      /// <returns></returns>
      public GpxWaypoint GetWaypoint(int wp) {
         GpxWaypoint pt = null;
         if (poorgpx != null) {

            pt = poorgpx.GetWaypoint(wp);

         } else {

            pt = new GpxWaypoint {
               Lon = stdgpx.ReadValue(XPaths.WaypointLongitude(wp), BaseElement.NOTVALID_DOUBLE),
               Lat = stdgpx.ReadValue(XPaths.WaypointLatitude(wp), BaseElement.NOTVALID_DOUBLE),
               Elevation = stdgpx.ReadValue(XPaths.WaypointElevation(wp), BaseElement.NOTVALID_DOUBLE)
            };
            string tmp = stdgpx.ReadValue(XPaths.WaypointPointTime(wp), DateTime2String(BaseElement.NOTVALID_TIME));
            try {
               //< time > xsd:dateTime </ time > < !--Datum und Zeit(UTC/ Zulu) in ISO 8601 Format: yyyy - mm - ddThh:mm: ssZ-- >
               pt.Time = DateTime.Parse(tmp, null, DateTimeStyles.RoundtripKind);
            } catch { }

         }
         return pt;
      }


      #region liefert Poor-Daten (wenn vorhanden) als Verweis oder Kopie

      /// <summary>
      /// liefert den Poor-Waypoint (oder null)
      /// </summary>
      /// <param name="track"></param>
      /// <param name="ascopy"></param>
      /// <returns></returns>
      public GpxWaypoint GetPoorWaypoint(int w, bool ascopy = false) {
         if (poorgpx != null) {
            GpxWaypoint pt = poorgpx.GetWaypoint(w);
            if (pt != null)
               return ascopy ?
                        new GpxWaypoint(pt) :
                        pt;
         }
         return null;
      }

      /// <summary>
      /// liefert den Poor-Trackpunkt (oder null)
      /// </summary>
      /// <param name="track"></param>
      /// <param name="segment"></param>
      /// <param name="point"></param>
      /// <param name="ascopy"></param>
      /// <returns></returns>
      public GpxTrackPoint GetPoorTrackSegmentPoint(int track, int segment, int point, bool ascopy = false) {
         if (poorgpx != null) {
            GpxTrackPoint pt = poorgpx.GetTrackSegmentPoint(track, segment, point);
            if (pt != null)
               return ascopy ?
                           new GpxTrackPoint(pt) :
                           pt;
         }
         return null;
      }

      /// <summary>
      /// liefert das Poor-Segment (oder null)
      /// </summary>
      /// <param name="track"></param>
      /// <param name="segment"></param>
      /// <returns></returns>
      public GpxTrackSegment GetPoorTrackSegment(int track, int segment, bool ascopy = false) {
         if (poorgpx != null) {
            GpxTrackSegment seg = poorgpx.GetTrackSegment(track, segment);
            if (seg != null)
               return ascopy ?
                           new GpxTrackSegment(seg) :
                           seg;
         }
         return null;
      }

      /// <summary>
      /// liefert den Poor-Track (oder null)
      /// </summary>
      /// <param name="track"></param>
      /// <param name="ascopy"></param>
      /// <returns></returns>
      public GpxTrack GetPoorTrack(int track, bool ascopy = false) {
         if (poorgpx != null) {
            GpxTrack tr = poorgpx.GetTrack(track);
            if (tr != null)
               return ascopy ?
                           new GpxTrack(tr) :
                           tr;
         }
         return null;
      }

      /// <summary>
      /// liefert die Poor-Route (oder null)
      /// </summary>
      /// <param name="route"></param>
      /// <param name="ascopy"></param>
      /// <returns></returns>
      public GpxRoute GetPoorRoute(int route, bool ascopy = false) {
         if (poorgpx != null) {
            GpxRoute r = poorgpx.GetRoute(route);
            if (r != null)
               return ascopy ?
                           new GpxRoute(r) :
                           r;
         }
         return null;
      }

      #endregion


      #region Track-Segment-Punkt-Bearbeitung

      /// <summary>
      /// ändert die Zeit und/oder die Höhe des Punktes
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      /// <param name="point">Punktnummer (0...)</param>
      /// <param name="time"></param>
      /// <param name="ele"></param>
      public void ChangeTrackSegmentPointData(int track, int segment, int point, DateTime time, double ele = BaseElement.NOTVALID_DOUBLE) {
         if (poorgpx != null) {
            GpxTrackPoint p = GetPoorTrackSegmentPoint(track, segment, point, false);
            if (p != null) {
               if (ele != BaseElement.NOTUSE_DOUBLE)
                  p.Elevation = ele;
               if (time != BaseElement.NOTUSE_TIME)
                  p.Time = time;
            }
         }

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx) {
            if (ele != BaseElement.NOTUSE_DOUBLE)
               if (ele != BaseElement.NOTVALID_DOUBLE) {
                  if (stdgpx.ExistXPath(XPaths.TrackSegmentPointElevation(track, segment, point)))
                     stdgpx.Change(XPaths.TrackSegmentPointElevation(track, segment, point), ele.ToString(CultureInfo.InvariantCulture));
                  else
                     stdgpx.InsertXmlText(XPaths.TrackSegmentPoint(track, segment, point), "<ele>" + ele.ToString(CultureInfo.InvariantCulture) + "</ele>", SimpleXmlDocument2.InsertPosition.PrependChild);
               } else {
                  if (stdgpx.ExistXPath(XPaths.TrackSegmentPointElevation(track, segment, point)))
                     stdgpx.Remove(XPaths.TrackSegmentPointElevation(track, segment, point));
               }

            if (time != BaseElement.NOTUSE_TIME)
               if (time != BaseElement.NOTVALID_TIME) {
                  string timetxt = GpxTime1_0.DateTime2String(time);
                  if (stdgpx.ExistXPath(XPaths.TrackSegmentPointTime(track, segment, point)))
                     stdgpx.Change(XPaths.TrackSegmentPointTime(track, segment, point), timetxt);
                  else
                     if (stdgpx.ExistXPath(XPaths.TrackSegmentPointElevation(track, segment, point)))
                     stdgpx.InsertXmlText(XPaths.TrackSegmentPointElevation(track, segment, point), "<time>" + timetxt + "</time>", SimpleXmlDocument2.InsertPosition.After);
                  else
                     stdgpx.InsertXmlText(XPaths.TrackSegmentPoint(track, segment, point), "<time>" + timetxt + "</time>", SimpleXmlDocument2.InsertPosition.PrependChild);
               } else {
                  if (stdgpx.ExistXPath(XPaths.TrackSegmentPointTime(track, segment, point)))
                     stdgpx.Remove(XPaths.TrackSegmentPointTime(track, segment, point));
               }
         }
      }

      /// <summary>
      /// entfernt den Punkt
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      /// <param name="point">Punktnummer (0...)</param>
      public void DeleteTrackSegmentPoint(int track, int segment, int point) {
         poorgpx?.RemoveTrackSegmentPoint(track, segment, point);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.Remove(XPaths.TrackSegmentPoint(track, segment, point));
      }

      #endregion

      #region Track-Segment-Bearbeitung

      /// <summary>
      /// löscht das Segment
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="segment">Segmentnummer (0...)</param>
      public void DeleteSegment(int track, int segment) {
         poorgpx?.RemoveTrackSegment(track, segment);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.Remove(XPaths.TrackSegment(track, segment));
      }

      #endregion

      #region Track-Bearbeitung

      /// <summary>
      /// fügt einen Track mit den Punkten an der Position ein
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="ptlist">Punktliste je Segment</param>
      public void InsertTrack(int track, GpxTrack tr) {
         poorgpx?.Tracks.Insert(track, tr);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            insertTrackXml(track, tr.AsXml());
      }

      /// <summary>
      /// fügt einen Track als Kopie eines Tracks aus einer anderen GPX-Datei ein
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="fromgpx"></param>
      /// <param name="fromtrack">Quell-Tracknummer (0...)</param>
      public void InsertTrack(int track, GpxFile fromgpx, int fromtrack) {
         string fromxml = null;
         GpxTrack fromtr = null;

         if (fromgpx.InternalGpx != InternalGpxForm.OnlyPoorGpx)
            fromxml = BaseElement.RemoveNamespace(fromgpx.stdgpx.GetXmlText(XPaths.Track(fromtrack)));
         else
            fromtr = fromgpx.GetPoorTrack(fromtrack, true);

         if (InternalGpx != InternalGpxForm.OnlyNormalGpx)
            poorgpx.InsertTrack(fromtr ?? new GpxTrack(fromxml),
                                track);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            insertTrackXml(track,
                           fromxml ?? fromtr.AsXml(int.MaxValue));
      }

      /// <summary>
      /// fügt den Track als XML-Text ein
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="trackxml"></param>
      void insertTrackXml(int track, string trackxml) {
         int trackcount = stdgpx.NodeCount(XPaths.Count_Track());    // NICHT TrackCount(), da bevorzugt nur poorgpx abgefragt wird
         track = Math.Max(0, Math.Min(track, trackcount));
         if (trackcount == 0) {
            string neighborpath = "";
            int count = RouteCount();
            if (count > 0) {
               neighborpath = XPaths.Route(count - 1);
            } else {
               count = WaypointCount();
               if (count > 0) {
                  neighborpath = XPaths.Waypoint(count - 1);
               } else {
                  if (stdgpx.ExistXPath(XPaths.Metadata()))
                     neighborpath = XPaths.Metadata();
               }
            }
            if (neighborpath.Length > 0) // nach rte, wpt oder metadata
               stdgpx.InsertXmlText(neighborpath,
                                 trackxml,
                                 SimpleXmlDocument2.InsertPosition.After);
            else // ohne rte, wpt oder metadata
               stdgpx.InsertXmlText(XPaths.Master(),
                                 trackxml,
                                 SimpleXmlDocument2.InsertPosition.AppendChild);
         } else { // Objektliste dieser Art ex. schon
            stdgpx.InsertXmlText(XPaths.Track(track - (track >= trackcount ? 1 : 0)),
                              trackxml,
                              track >= trackcount ?
                                 SimpleXmlDocument2.InsertPosition.After :  // nach diesem Child (als letztes der Liste)
                                 SimpleXmlDocument2.InsertPosition.Before); // vor diesem Child
         }
      }

      /// <summary>
      /// löscht den Track
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      public void DeleteTrack(int track) {
         poorgpx?.RemoveTrack(track);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.Remove(XPaths.Track(track));
      }

      #endregion

      #region Routen-Bearbeitung

      /// <summary>
      /// fügt eine Route als Kopie einer Route aus einer anderen GPX-Datei ein
      /// </summary>
      /// <param name="route">Routennummer (0...)</param>
      /// <param name="fromgpx"></param>
      /// <param name="fromroute">Quell-Routennummer (0...)</param>
      public void InsertRoute(int route, GpxFile fromgpx, int fromroute) {
         string fromxml = null;
         GpxRoute fromr = null;

         if (fromgpx.InternalGpx != InternalGpxForm.OnlyPoorGpx)
            fromxml = BaseElement.RemoveNamespace(fromgpx.stdgpx.GetXmlText(XPaths.Route(fromroute)));
         else
            fromr = fromgpx.GetPoorRoute(fromroute, true);

         if (InternalGpx != InternalGpxForm.OnlyNormalGpx)
            poorgpx.InsertRoute(fromr ?? new GpxRoute(fromxml),
                                route);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            insertRouteXml(route,
                           fromxml ?? fromr.AsXml(int.MaxValue));
      }

      /// <summary>
      /// fügt die Route als XML-Text ein
      /// </summary>
      /// <param name="route">Routennummer (0...)</param>
      /// <param name="routexml"></param>
      void insertRouteXml(int route, string routexml) {
         int routecount = RouteCount();
         route = Math.Max(0, Math.Min(route, routecount));
         if (routecount == 0) {
            string neighborpath = "";
            int count = WaypointCount();
            if (count > 0) {
               neighborpath = XPaths.Waypoint(count - 1);
            } else {
               if (stdgpx.ExistXPath(XPaths.Metadata()))
                  neighborpath = XPaths.Metadata();
            }
            if (neighborpath.Length > 0) // nach  wpt oder metadata
               stdgpx.InsertXmlText(neighborpath,
                                 routexml,
                                 SimpleXmlDocument2.InsertPosition.After);
            else // ohne wpt oder metadata
               stdgpx.InsertXmlText(XPaths.Master(),
                                 routexml,
                                 SimpleXmlDocument2.InsertPosition.AppendChild);
         } else { // Objektliste dieser Art ex. schon
            stdgpx.InsertXmlText(XPaths.Route(route - (route >= routecount ? 1 : 0)),
                              routexml,
                              route >= routecount ?
                                 SimpleXmlDocument2.InsertPosition.After :  // nach diesem Child (als letztes der Liste)
                                 SimpleXmlDocument2.InsertPosition.Before); // vor diesem Child
         }
      }

      /// <summary>
      /// löscht die Route
      /// </summary>
      /// <param name="route">Routennummer (0...)</param>
      public void DeleteRoute(int route) {
         poorgpx?.RemoveRoute(route);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.Remove(XPaths.Route(route));
      }

      #endregion

      #region Waypoint-Bearbeitung

      /// <summary>
      /// fügt einen Waypoint als Kopie eines Waypoint aus einer anderen GPX-Datei ein
      /// </summary>
      /// <param name="waypt">Punktnummer (0...)</param>
      /// <param name="fromgpx"></param>
      /// <param name="fromwaypt">Quell-Punktnummer (0...)</param>
      public void InsertWaypoint(int waypt, GpxFile fromgpx, int fromwaypt) {
         string fromxml = null;
         GpxWaypoint fromwp = null;

         if (fromgpx.InternalGpx != InternalGpxForm.OnlyPoorGpx)
            fromxml = BaseElement.RemoveNamespace(fromgpx.stdgpx.GetXmlText(XPaths.Waypoint(fromwaypt)));
         else
            fromwp = fromgpx.GetPoorWaypoint(fromwaypt, true);

         if (InternalGpx != InternalGpxForm.OnlyNormalGpx)
            poorgpx.InsertWaypoint(fromwp ?? new GpxWaypoint(fromxml),
                                   waypt);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            insertWaypointXml(waypt,
                              fromxml ?? fromwp.AsXml(int.MaxValue));
      }

      /// <summary>
      /// fügt den Waypoint als XML-Text ein
      /// </summary>
      /// <param name="waypt">Waypointnummer (0...)</param>
      /// <param name="wayptxml"></param>
      void insertWaypointXml(int waypt, string wayptxml) {
         int wayptcount = WaypointCount();
         waypt = Math.Max(0, Math.Min(waypt, wayptcount));

         // leeren Waypoint erzeugen
         if (wayptcount == 0) { // 1. Objekt dieser Art
            // Sequenz unter <gpx>: <metadata>, <wpt> 0..
            if (stdgpx.ExistXPath(XPaths.Metadata()))
               stdgpx.InsertXmlText(XPaths.Metadata(),
                                 wayptxml,
                                 SimpleXmlDocument2.InsertPosition.After);
            else
               stdgpx.InsertXmlText(XPaths.Master(),
                                 wayptxml,
                                 SimpleXmlDocument2.InsertPosition.PrependChild); // als 1. Child
         } else { // Objektliste dieser Art ex. schon
            stdgpx.InsertXmlText(XPaths.Waypoint(waypt - (waypt >= wayptcount ? 1 : 0)),
                              wayptxml,
                              waypt >= wayptcount ?
                                 SimpleXmlDocument2.InsertPosition.After :  // nach diesem Child (als letztes der Liste)
                                 SimpleXmlDocument2.InsertPosition.Before); // vor diesem Child
         }
      }


      /// <summary>
      /// löscht den Waypoint
      /// </summary>
      /// <param name="waypt">Waypointnummer (0...)</param>
      public void DeleteWaypoint(int waypt) {
         poorgpx?.RemoveWaypoint(waypt);

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.Remove(XPaths.Waypoint(waypt));
      }

      #endregion

      #region Namen liefern/setzen

      /// <summary>
      /// liefert den Namen des Waypoints
      /// </summary>
      /// <param name="wp">Waypointnummer (0...)</param>
      /// <returns></returns>
      public string GetWaypointname(int wp) {
         GpxWaypoint w = GetPoorWaypoint(wp);
         return w != null ?
                     w.Name :
                     stdgpx.ReadValue(XPaths.WaypointName(wp), "");
      }

      /// <summary>
      /// setzt den Tracknamen
      /// </summary>
      /// <param name="wp">Waypointnummer (0...)</param>
      /// <param name="name"></param>
      public void SetWaypointname(int wp, string name) {
         GpxWaypoint waypt = GetPoorWaypoint(wp);
         if (waypt != null)
            waypt.Name = name;

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx) {
            string path = XPaths.WaypointName(wp);
            if (stdgpx.ExistXPath(path)) {
               if (!string.IsNullOrEmpty(name))
                  stdgpx.Change(path, name);
               else
                  stdgpx.Remove(path);
            } else {
               if (!string.IsNullOrEmpty(name)) {
                  stdgpx.InsertXmlText(XPaths.Waypoint(wp),
                                    string.Format("<name>{0}</name>", name),
                                    SimpleXmlDocument2.InsertPosition.PrependChild);
               }
            }
         }
      }

      /// <summary>
      /// liefert den Namen der Route
      /// </summary>
      /// <param name="route">Routenummer (0...)</param>
      /// <returns></returns>
      public string GetRoutename(int route) {
         GpxRoute r = GetPoorRoute(route);
         return r != null ?
                     r.Name :
                     stdgpx.ReadValue(XPaths.RouteName(route), "");
      }

      /// <summary>
      /// setzt den Tracknamen
      /// </summary>
      /// <param name="r">Routenummer (0...)</param>
      /// <param name="name"></param>
      public void SetRoutename(int r, string name) {
         GpxRoute route = GetPoorRoute(r);
         if (route != null)
            route.Name = name;

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx) {
            string path = XPaths.RouteName(r);
            if (stdgpx.ExistXPath(path)) {
               if (!string.IsNullOrEmpty(name))
                  stdgpx.Change(path, name);
               else
                  stdgpx.Remove(path);
            } else {
               if (!string.IsNullOrEmpty(name)) {
                  stdgpx.InsertXmlText(XPaths.Route(r),
                                    string.Format("<name>{0}</name>", name),
                                    SimpleXmlDocument2.InsertPosition.PrependChild);
               }
            }
         }
      }

      /// <summary>
      /// liefert den Namen des Tracks
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <returns></returns>
      public string GetTrackname(int track) {
         GpxTrack t = GetPoorTrack(track);
         return t != null ?
                     t.Name :
                     stdgpx.ReadValue(XPaths.TrackName(track), "");
      }

      /// <summary>
      /// setzt den Tracknamen
      /// </summary>
      /// <param name="track">Tracknummer (0...)</param>
      /// <param name="name"></param>
      public void SetTrackname(int track, string name) {
         GpxTrack tr = GetPoorTrack(track);
         if (tr != null)
            tr.Name = name;

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx) {
            string path = XPaths.TrackName(track);
            if (stdgpx.ExistXPath(path)) {
               if (!string.IsNullOrEmpty(name))
                  stdgpx.Change(path, name);
               else
                  stdgpx.Remove(path);
            } else {
               if (!string.IsNullOrEmpty(name)) {
                  // Laut Definition GPX 1.1: <name> ist (wenn vorhanden) immer das 1 Element unterhalb von <trk>
                  stdgpx.InsertXmlText(XPaths.Track(track),
                                     string.Format("<name>{0}</name>", name),
                                     SimpleXmlDocument2.InsertPosition.PrependChild);
               }
            }
         }
      }

      #endregion

      /// <summary>
      /// hängt die Daten der GPX-Datei an die vorhandene Datei an (Waypoints, Routen und Tracks)
      /// </summary>
      /// <param name="fromgpx"></param>
      public void Concat(GpxFile fromgpx) {
         for (int i = 0; i < fromgpx.WaypointCount(); i++)
            InsertWaypoint(int.MaxValue, fromgpx, i);
         for (int i = 0; i < fromgpx.RouteCount(); i++)
            InsertRoute(int.MaxValue, fromgpx, i);
         for (int i = 0; i < fromgpx.TrackCount(); i++)
            InsertTrack(int.MaxValue, fromgpx, i);

         // Metadaten
         if (InternalGpx != InternalGpxForm.OnlyNormalGpx &&
             fromgpx.InternalGpx != InternalGpxForm.OnlyNormalGpx) {
            poorgpx.Metadata.SetMaxDateTime(fromgpx.poorgpx.Metadata.Time);
            poorgpx.Metadata.Bounds.Union(fromgpx.poorgpx.Metadata.Bounds);
         }
      }

      /// <summary>
      /// nur bei Version 1.1 korrekt
      /// </summary>
      /// <param name="minlat"></param>
      /// <param name="maxlat"></param>
      /// <param name="minlon"></param>
      /// <param name="maxlon"></param>
      /// <param name="time"></param>
      public void InsertSimpleMetadata(double minlat, double maxlat, double minlon, double maxlon,
                                       DateTime time) {
         stdgpx.Remove(XPaths.Metadata()); // falls schon vorhanden

         GpxMetadata1_1 metadata = new GpxMetadata1_1 {
            Time = time,
            Bounds = new GpxBounds(minlat, maxlat, minlon, maxlon)
         };

         if (poorgpx != null)
            poorgpx.Metadata = metadata;

         if (InternalGpx != InternalGpxForm.OnlyPoorGpx)
            stdgpx.InsertXmlText("/x:gpx",
                              metadata.AsXml(),
                              SimpleXmlDocument2.InsertPosition.PrependChild);
      }

      /// <summary>
      /// liefert Datum und Zeit im Format für die GPX-Datei
      /// </summary>
      /// <param name="dt"></param>
      /// <returns></returns>
      public static string DateTime2String(DateTime dt) {
         return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
      }

      /// <summary>
      /// liefert einen XML-codierten String
      /// </summary>
      /// <param name="txt"></param>
      /// <returns></returns>
      public static string Text2String(string txt) {
         return HttpUtility.HtmlEncode(txt);
      }

      /// <summary>
      /// liefert eine double-Zahl als Text
      /// </summary>
      /// <param name="v"></param>
      /// <returns></returns>
      public static string Double2String(double v) {
         return v.ToString(CultureInfo.InvariantCulture);
      }

      public override string ToString() {
         return string.Format("{0}, {1} Tracks", Filename, TrackCount());
      }
   }
}
