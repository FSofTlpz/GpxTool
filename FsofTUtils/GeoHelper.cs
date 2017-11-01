using System;

namespace FSoftUtils {

   public class GeoHelper {

      //Console.WriteLine(GeoHelper.Wgs84Distance(8.41321, 8.42182, 49.9917, 50.0049));       // etwa 1591
      //Console.WriteLine(GeoHelper.Wgs84Distance(8.41321, 8.42182, 49.9917, 50.0049, 1));    // etwa 1591
      //Console.WriteLine(GeoHelper.Wgs84Distance(8.41321, 8.42182, 49.9917, 50.0049, 2));    // etwa 1592

      /// <summary>
      /// Berechnungsmethoden für Entfernungen zwischen WGS84-Koordinaten
      /// </summary>
      public enum Wgs84DistanceCompute {
         /// <summary>
         /// für kurze Entfernungen; die Erdoberfläche wird näherungsweise als Fläche angesehen
         /// </summary>
         simple,
         /// <summary>
         /// Grosskreisberechnung auf einer Kugel
         /// </summary>
         sphere,
         /// <summary>
         /// Erde als Ellipsoid
         /// </summary>
         ellipsoid
      }

      /// <summary>
      /// näherungsweise Entfernungsberechnung zwischen WGS84-Koordinaten
      /// </summary>
      /// <param name="lon1"></param>
      /// <param name="lon2"></param>
      /// <param name="lat1"></param>
      /// <param name="lat2"></param>
      /// <param name="model">0 für kurze Entfernungen, 1 für Grosskreis auf Kugel, sonst für WGS84-Ellipsoid</param>
      public static double Wgs84Distance(double lon1, double lon2, double lat1, double lat2, Wgs84DistanceCompute model = Wgs84DistanceCompute.simple) {
         if (lon1 == lon2 &&
             lat1 == lat2)
            return 0;

         double radius = 6370000;         // durchschnittlicher Erdradius

         switch (model) {
            case Wgs84DistanceCompute.simple:
               // Annahmen: 
               //    * Die Entfernung ist so kurz, das sich die Erdoberfläche näherungsweise als Fläche ansehen läßt
               //    * Die Erde ist eine Kugel (konstanter Radius).
               double dist4degree = radius * Math.PI / 180;   // 111177,5
               double deltay = dist4degree * (lat1 - lat2);
               dist4degree *= Math.Cos((lat1 + (lat1 - lat2) / 2) / 180 * Math.PI);
               double deltax = dist4degree * (lon1 - lon2);
               return Math.Sqrt(deltax * deltax + deltay * deltay);

            case Wgs84DistanceCompute.sphere:
               // Annahmen: 
               //    * Die Erde ist eine Kugel (konstanter Radius) -> Grosskreisberechnung
               lat1 *= Math.PI / 180;
               lat2 *= Math.PI / 180;
               lon1 *= Math.PI / 180;
               lon2 *= Math.PI / 180;
               return radius * Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1));

            default:
               // Annahmen: 
               //    * WGS84-Ellipsoid
               // vgl. http://de.wikipedia.org/wiki/Entfernungsberechnung#Genauere_Formel_zur_Abstandsberechnung_auf_der_Erde
               double f = 1 / 298.257223563;    // Abplattung der Erde
               double a = 6378137;              // Äquatorradius der Erde

               double F = (lat1 + lat2) / 2 * Math.PI / 180;
               double G = (lat1 - lat2) / 2 * Math.PI / 180;
               double l = (lon1 - lon2) / 2 * Math.PI / 180;
               double S = Math.Pow(Math.Sin(G), 2) * Math.Pow(Math.Cos(l), 2) + Math.Pow(Math.Cos(F), 2) * Math.Pow(Math.Sin(l), 2);
               double C = Math.Pow(Math.Cos(G), 2) * Math.Pow(Math.Cos(l), 2) + Math.Pow(Math.Sin(F), 2) * Math.Pow(Math.Sin(l), 2);
               double w = Math.Atan(Math.Sqrt(S / C));
               double D = 2 * w * a;
               // Der Abstand D muss nun durch die Faktoren H_1 und H_2 korrigiert werden:
               double R = Math.Sqrt(S * C) / w;
               double H1 = (3 * R - 1) / (2 * C);
               double H2 = (3 * R + 1) / (2 * S);
               return D * (1 + f * H1 * Math.Pow(Math.Sin(F), 2) * Math.Pow(Math.Cos(G), 2) - f * H2 * Math.Pow(Math.Cos(F), 2) * Math.Pow(Math.Sin(G), 2));
         }
      }

      /// <summary>
      /// berechnet näherungsweise die Veränderung der x- und der y-Koordinate (für sehr kleine Winkeldifferenzen)
      /// </summary>
      /// <param name="lon1">alte Länge</param>
      /// <param name="lon2">neue Länge</param>
      /// <param name="lat1">alte Breite</param>
      /// <param name="lat2">neue Breite</param>
      /// <param name="deltax"></param>
      /// <param name="deltay"></param>
      public static void Wgs84ShortXYDelta(double lon1, double lon2, double lat1, double lat2, out double deltax, out double deltay) {
         double radius = 6370000;         // durchschnittlicher Erdradius
         double dist4degree = radius * Math.PI / 180;   // 111177,5
         deltay = dist4degree * (lat2 - lat1);
         dist4degree *= Math.Cos((lat2 + (lat2 - lat1) / 2) / 180 * Math.PI);
         deltax = dist4degree * (lon2 - lon1);
      }

   }
}
