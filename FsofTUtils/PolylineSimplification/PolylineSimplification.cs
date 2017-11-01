﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FSoftUtils.PolylineSimplification {

   /// <summary>
   /// 2D-Punkt
   /// </summary>
   public class SimplificationPoint {

      public double X { get; set; }
      public double Y { get; set; }

      /// <summary>
      /// false, wenn der Punkt entfallen soll
      /// </summary>
      public bool IsValid { get; set; }
      /// <summary>
      /// true, wenn der Punkt unbedingt erhalten bleiben soll
      /// </summary>
      public bool IsLocked { get; set; }

      public SimplificationPoint() {
         X = 0;
         Y = 0;
         IsValid = true;
         IsLocked = false;
      }

      public SimplificationPoint(SimplificationPoint p) {
         Set(p);
      }

      public SimplificationPoint(double x, double y, bool bIsLocked = false) {
         X = x;
         Y = y;
         IsValid = true;
         IsLocked = bIsLocked;
      }

      public void Set(SimplificationPoint p) {
         X = p.X;
         Y = p.Y;
         IsValid = p.IsValid;
         IsLocked = p.IsLocked;
      }

      /// <summary>
      /// Quadrat der Länge zum Nullpunkt
      /// </summary>
      /// <returns></returns>
      public double SquareAbsolute() {
         return X * X + Y * Y;
      }

      /// <summary>
      /// Länge zum Nullpunkt
      /// </summary>
      /// <returns></returns>
      public double Absolute() {
         return Math.Sqrt(SquareAbsolute());
      }

      /// <summary>
      /// Skalarprodukt des Vektors vom Nullpunkt
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public double DotProduct(SimplificationPoint p) {
         return X * p.X + Y * p.Y;
      }

      /// <summary>
      /// Winkel zwischen 2 Vektoren (0..Math.PI)
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public double Arc(SimplificationPoint p) {
         return Math.Acos(DotProduct(p) / (Absolute() * p.Absolute()));
      }

      /// <summary>
      /// Quadrat des Abstandes
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public double SquareDistance(SimplificationPoint p) {
         return (X - p.X) * (X - p.X) + (Y - p.Y) * (Y - p.Y);
      }

      /// <summary>
      /// Abstand
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public double Distance(SimplificationPoint p) {
         return Math.Sqrt(SquareDistance(p));
      }


      /// <summary>
      /// Vektor-Subtraktion
      /// </summary>
      /// <param name="p1"></param>
      /// <param name="p2"></param>
      /// <returns></returns>
      public static SimplificationPoint operator -(SimplificationPoint p1, SimplificationPoint p2) {
         return new SimplificationPoint(p1.X - p2.X, p1.Y - p2.Y);
      }

      public bool Equals(SimplificationPoint p) {
         return p != null && X == p.X && Y == p.Y;
      }

      public override string ToString() {
         return string.Format("({0}, {1}), IsValid={2}, IsLocked={3}", X, Y, IsValid, IsLocked);
      }
   }

   public class SimplificationPointList {

      /// <summary>
      /// Punktliste
      /// </summary>
      public List<SimplificationPoint> pt;

      /// <summary>
      /// Liste mit fester Länge erzeugen
      /// </summary>
      /// <param name="iLength"></param>
      public SimplificationPointList(int iLength) {
         if (iLength < 3)
            throw new ArgumentException("Es sind min. 3 Punkte für die Liste nötig.");
         pt = new List<SimplificationPoint>(new SimplificationPoint[iLength]);
      }

      /// <summary>
      /// Kopie einer Liste erzeugen
      /// </summary>
      /// <param name="pl"></param>
      public SimplificationPointList(SimplificationPointList pl) {
         SimplificationPoint[] tmp = new SimplificationPoint[pl.Count];
         pl.pt.CopyTo(tmp);
         pt = new List<SimplificationPoint>(tmp);
      }

      public void Set(int no, double x, double y, bool islocked = false) {
         if (no < 0 || pt.Count <= no)
            throw new ArgumentException("Der Punktindex liegt außerhalb des gültigen Bereichs.");
         if (pt[no] == null)
            pt[no] = new SimplificationPoint(x, y);
         else {
            pt[no].X = x;
            pt[no].Y = y;
            pt[no].IsLocked = islocked;
            pt[no].IsValid = true;
         }
      }

      /// <summary>
      /// Länge der Liste
      /// </summary>
      public int Count { get { return pt.Count; } }

      #region horizontale Glättung (durch Ungültigsetzen von Punkten)

      /// <summary>
      /// Reumann-Witkam-Algorithmus;
      /// i.A. sollte der letzte Punkt vorher 'gelocked' werden
      /// </summary>
      /// <param name="dWidth">halbe Korridorbreite</param>
      public void ReumannWitkam(double dWidth) {
         int iStart = 0;
         int iNext = 1;
         while (pt[iStart].Equals(pt[iNext]) && iNext < Count) iNext++;      // ungleichen Punkt suchen
         int iTest = iNext + 1;
         dWidth *= dWidth;

         for (int i = 0; i < Count; i++)
            pt[i].IsValid = true;

         while (iTest < Count) {
            bool bPointIsValid = pt[iTest].IsLocked;
            if (!bPointIsValid) {            // Test ist nötig (Normalfall)
               SimplificationPoint p0 = pt[iStart];
               SimplificationPoint p1 = pt[iNext];
               SimplificationPoint p2 = pt[iTest];
               // teste, ob p2 innerhalb der durch p0, p1 und dWidth vorgegebenen Bandbreite liegt

               // Das fkt. sehr einfach mit Hilfe des Skalarproduktes. Es wird nicht der Abstand, sondern das Quadrat des Abstandes
               // berechnet. Man spart sich das Ziehen der Wurzel und es gibt keine Probleme mit ev. negativen Werten.

               SimplificationPoint p0p1 = p0 - p1;
               SimplificationPoint p1p2 = p1 - p2;
               double dDotProduct = p0p1.DotProduct(p1p2);
               double dSquare_AbsP0P1 = p0p1.SquareAbsolute();
               double dSquare_WidthTest = (dSquare_AbsP0P1 * p1p2.SquareAbsolute() - dDotProduct * dDotProduct) / dSquare_AbsP0P1;

               bPointIsValid = dSquare_WidthTest > dWidth;
            }

            if (bPointIsValid) {      // p2 bleibt erhalten
               pt[iNext].IsValid = false;
               iStart = iTest;
               iNext = iStart + 1;
               while (iNext < Count && pt[iStart].Equals(pt[iNext])) iNext++;      // ungleichen Punkt suchen
               iTest = iNext + 1;
               while (iTest < Count && pt[iNext].Equals(pt[iTest])) iTest++;       // ungleichen Punkt suchen
            } else {
               pt[iTest].IsValid = false;
               do {
                  iTest++;
               } while (iTest < Count && pt[iNext].Equals(pt[iTest]));             // ungleichen Punkt suchen
            }
         }
         pt[Count - 1].IsValid = true;
      }

      /// <summary>
      /// Douglas-Peucker-Algorithmus
      /// </summary>
      /// <param name="dWidth"></param>
      /// <returns>Anzahl der gelöschten Punkte</returns>
      public int DouglasPeucker(double dWidth) {
         int iCount = pt.Count;
         if (pt[0] == pt[pt.Count - 1]) { // wenn geschlossen
            pt[iCount - 1].IsValid = true;
            iCount--;
         }

         if (iCount <= 2)
            return 0;

         for (int i = 0; i < iCount; i++) // zunächst alle Punkt ungültig
            pt[i].IsValid = false;
         pt[0].IsValid =                   // 1. und letzter Punkt sind immer gültig
         pt[iCount - 1].IsValid = true;

         DouglasPeuckerRecursive(0, iCount - 1, dWidth * dWidth);

         //return RemoveNotValidPoints();
         return 0;
      }

      /// <summary>
      /// Wenn ein Punkt der Polylinie von iStart bis iEnd seitlich zu weit von der Verbindung von iStart zu iEnd entfernt ist, wird er
      /// als gültig gesetzt und die Polylinie an dieser Stelle geteilt. Die Teil-Polylinien werden (rekursiv) genauso weiter untersucht.
      /// Zum Schluß sind alle notwendigen Punkte gültig gesetzt. Die anderen können entfernt werden.
      /// </summary>
      /// <param name="iStart">Index des 1. Punktes</param>
      /// <param name="iEnd">Index des letzten Punktes</param>
      /// <param name="dSquareWidth">Quadrat der min. Abweichung</param>
      void DouglasPeuckerRecursive(int iStart, int iEnd, double dSquareWidth) {
         int idx = GetFarPointIdx4DouglasPeucker(iStart, iEnd, dSquareWidth);
         if (idx > 0) {                // Aufteilung der Polylinie, weil der Trennpunkt seitlich zu weit weg von der Verbindung zwischen Anfangs- und Endpunkt liegt
            pt[idx].IsValid = true;    // Trennpunkt ist auf jeden Fall gültig
            if (idx - iStart > 1)      // rekursiv für die 1. Teil-Polylinie
               DouglasPeuckerRecursive(iStart, idx, dSquareWidth);
            if (iEnd - idx > 1)        // rekursiv für die 2. Teil-Polylinie
               DouglasPeuckerRecursive(idx, iEnd, dSquareWidth);
         }
      }

      /// <summary>
      /// Wenn ein Punkt der Polylinie von iStart bis iEnd weiter entfernt ist, wird die Polylinie an dieser Stelle geteilt und der
      /// Index des "Teilungspunktes" geliefert. Dieser Punkt bleibt in der Polylinie erhalten.
      /// </summary>
      /// <param name="iStart">Index des 1. Punktes der untersuchten (Teil-)Polylinie</param>
      /// <param name="iEnd">Index des letzten Punktes der untersuchten (Teil-)Polylinie</param>
      /// <param name="dMinSquareWidth">Quadrat des min. nötigen Abstandes für einen "Teilungspunkt"</param>
      /// <returns>Index des Teilungspunktes oder negativ</returns>
      int GetFarPointIdx4DouglasPeucker(int iStart, int iEnd, double dMinSquareWidth) {
         int idx = -1;
         SimplificationPoint pBaseLine = pt[iEnd] - pt[iStart];    // Verbindung von Anfangs- und Endpunkt (Richtungsvektor)
         double dSquare_AbsBaseLine = pBaseLine.SquareAbsolute();    // Quadrat der Länge der Verbindung von Anfangs- und Endpunkt

         for (int i = iStart + 1; i < iEnd; i++) {

            /* Für die Strecke AB mit dem Winkel alpha im Punkt A zum Punkt P und dem Fußpunkt F des Punktes P auf AB ergibt sich:
             * 
             *    cos(alpha) = |AF| / |AP|
             *    |AF| = |AP| * cos(alpha)
             * 
             * Außerdem gilt im rechtwinkligen Dreieck:
             * 
             *    |AP|² = |AF|² + |FP|²
             *    
             * Mit |FP| = d folgt:
             * 
             *    d² = |AP|² - |AF|²
             *    d² = |AP|² - (|AP| * cos(alpha))²
             *    d² = |AP|² - |AP|² * (cos(alpha))²
             *    
             * Gleichzeitig gilt für das Skalarprodukt von AP und AB:
             * 
             *    AP * AB = |AP| * |AB| * cos(alpha)
             *    cos(alpha) = (AP * AB) / (|AP| * |AB|)
             * 
             *    d² = |AP|² - |AP|² * ((AP * AB) / (|AP| * |AB|))²
             *    d² = |AP|² - (AP * AB)² / |AB|²
             * 
             * Man kann also den Abstand d mit der Länge von AP bzw. AB und dem Skalarprodukt der beiden Vektoren bestimmen.
             * 
             * Noch effektiver ist die Bestimmung des Quadrates des Abstandes.
             */
            SimplificationPoint pTestLine = pt[i] - pt[iStart];       // Testpunkt
            double dDotProduct = pBaseLine.DotProduct(pTestLine);       // Skalarprodukt des Testpunktes zur Linie
            double dSquare_WidthTest = (dSquare_AbsBaseLine * pTestLine.SquareAbsolute() - dDotProduct * dDotProduct) / dSquare_AbsBaseLine; // Quadrat des Abstandes des Testpunktes von der Linie
            if (dMinSquareWidth < dSquare_WidthTest) { // auf jeden Fall Teilung der Polylinie nötig, aber ev. erst bei einem späteren i
               dMinSquareWidth = dSquare_WidthTest;
               idx = i;
            }
         }

         //double dMaxSquareWidth = dMinSquareWidth;
         //int idx = -1;
         //for (int i = iStart + 1; i < iEnd - 1; i++) {
         //   SimplificationPoint pBaseLine = pt[iEnd] - pt[iStart];
         //   SimplificationPoint pTestLine = pt[i] - pt[iStart];
         //   double dDotProduct = pBaseLine.DotProduct(pTestLine);
         //   double dSquare_AbsBaseLine = pBaseLine.SquareAbsolute();
         //   double dSquare_WidthTest = (dSquare_AbsBaseLine * pTestLine.SquareAbsolute() - dDotProduct * dDotProduct) / dSquare_AbsBaseLine;
         //   if (dMaxSquareWidth < dSquare_WidthTest) {
         //      dMaxSquareWidth = dSquare_WidthTest;
         //      idx = i;
         //   }
         //}

         return idx;
      }

      /// <summary>
      /// ungültige Punkte entfernen
      /// </summary>
      /// <returns>Anzahl der gelöschten Punkte</returns>
      int RemoveNotValidPoints() {
         int removed = 0;
         for (int i = pt.Count - 1; i >= 0; i--)
            if (!pt[i].IsValid) {
               pt.RemoveAt(i);
               removed++;
            }
         return removed;
      }

      #endregion

      #region Höhenglättung (X steht für die kumulierte Entfernung vom Startpunkt, Y für die Höhe)

      /// <summary>
      /// führt eine Integration für 'dWidth' breite Streifen durch und erzeugt eine neue (!) Punktliste
      /// </summary>
      /// <param name="dWidth"></param>
      public void HeigthProfileWithIntegral(double dWidth) {
         List<SimplificationPoint> newpt = new List<SimplificationPoint>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         if (pt.Count >= 2) {
            double dStripeStart = 0.0;
            double dX0 = pt[0].X;
            double dY0 = pt[0].Y;
            double dTripLength = pt[pt.Count - 1].X;

            // Für jeden Streifen wird eine mit der Streckenlänge "gewichtete" durchschnittliche Höhe ermittelt.
            for (int i = 0, p = 1;
                 dStripeStart < dTripLength && p < pt.Count;
                 i++, dStripeStart += dWidth) {
               double dStripeEnd = Math.Min(dStripeStart + dWidth, dTripLength);
               // Integral zwischen dStripeStart und dStripeEnd bilden
               double dIntegral = 0;
               dX0 = dStripeStart;
               for (; p < pt.Count; p++) {
                  if (pt[p].X <= dStripeEnd) {
                     double dx = pt[p].X - dX0;
                     double dh = pt[p].Y - dY0;
                     dIntegral += dx * (dY0 + dh / 2);
                     dY0 = pt[p].Y;
                     dX0 += dx;
                  } else {
                     double dx = dStripeEnd - dX0;
                     double dh = dx * (pt[p].Y - pt[p - 1].Y) / (pt[p].X - pt[p - 1].X);
                     dIntegral += dx * (dY0 + dh / 2);
                     dY0 = pt[p - 1].Y + dh;
                     dX0 += dx;
                     break;
                  }
               }
               newpt.Add(new SimplificationPoint(dStripeEnd, dIntegral));
            }

            for (int i = 0; i < newpt.Count; i++) {
               double dx = i == 0 ? newpt[i].X : newpt[i].X - newpt[i - 1].X;
               newpt[i].Y /= dx;
            }
            pt = newpt;
         }
      }

      /// <summary>
      /// berechnet den gleitenden Mittelwert aus 'count' Punkten, wobei die Höhe jeweils noch mit der zugehörigen Teil-Streckenlänge gewichtet wird
      /// </summary>
      /// <param name="count"></param>
      public void HeigthProfileWithSlidingMean(int count) {
         List<SimplificationPoint> newpt = new List<SimplificationPoint>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         int iFirstIdx = -count / 2;
         int iLastIdx = iFirstIdx + count - 1;

         newpt.Add(new SimplificationPoint(pt[0].X, pt[0].Y));       // 1. Punkt einfach übernehmen
         for (int i = 1; i < pt.Count; i++) {
            int iStartIdx = Math.Max(1, i + iFirstIdx);
            int iEndIdx = Math.Min(pt.Count - 1, i + iLastIdx);
            double dSum = 0;
            for (int j = iStartIdx; j <= iEndIdx; j++)      // iStartIdx > 0 !
               dSum += pt[j].Y * (pt[j].X - pt[j - 1].X);   // Höhe * Teillänge
            dSum /= pt[iEndIdx].X - pt[iStartIdx - 1].X;    // / Gesamtlänge
            newpt.Add(new SimplificationPoint(pt[i].X, dSum));
         }
         pt = newpt;
      }

      /// <summary>
      /// führt eine Integration für max. 'dWidth' breite Streifen um den jeweiligen Punkt durch (+- dWidth/2)
      /// </summary>
      /// <param name="dWidth"></param>
      public void HeigthProfileWithSlidingIntegral(double dWidth) {
         List<SimplificationPoint> newpt = new List<SimplificationPoint>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         if (pt.Count >= 2) {
            newpt.Add(new SimplificationPoint(pt[0]));
            dWidth /= 2;
            SimplificationPoint pStart = new SimplificationPoint();
            SimplificationPoint pEnd = new SimplificationPoint();
            for (int i = 1; i < pt.Count; i++) {      // für jeden Punkt außer dem 1.
               double dStripeStart = Math.Max(0, pt[i].X - dWidth);
               double dStripeEnd = Math.Min(pt[pt.Count - 1].X, pt[i].X + dWidth);
               int iStart, iEnd;
               for (iStart = i; iStart >= 0; iStart--)
                  if (pt[iStart].X <= dStripeStart) break;
               // --> pt[iStart].X <= dStripeStart
               for (iEnd = i; iEnd < pt.Count; iEnd++)
                  if (pt[iEnd].X >= dStripeEnd) break;
               // --> pt[iEnd].X >= dStripeEnd

               if (pt[iStart].X < dStripeStart) {     // virtuellen Startpunkt berechnen
                  pStart.X = dStripeStart;
                  pStart.Y = pt[iStart].Y + (dStripeStart - pt[iStart].X) * (pt[iStart + 1].Y - pt[iStart].Y) / (pt[iStart + 1].X - pt[iStart].X);
               }
               if (pt[iEnd].X > dStripeEnd) {         // virtuellen Endpunkt berechnen
                  pEnd.X = dStripeEnd;
                  pEnd.Y = pt[iEnd].Y - (pt[iEnd].X - dStripeEnd) * (pt[iEnd].Y - pt[iEnd - 1].Y) / (pt[iEnd].X - pt[iEnd - 1].X);
               }

               // Integral zwischen dStripeStart und dStripeEnd bilden; die Punkte mit Index iStart ... iEnd sind daran beteiligt
               double dIntegral = 0;
               for (int p = iStart; p < iEnd; p++) {
                  SimplificationPoint p1 = pt[p];
                  if (p1.X < dStripeStart) p1 = pStart;
                  SimplificationPoint p2 = pt[p + 1];
                  if (p2.X > dStripeEnd) p2 = pEnd;

                  dIntegral += (p1.Y + (p2.Y - p1.Y) / 2) * (p2.X - p1.X);
               }

               newpt.Add(new SimplificationPoint(pt[i].X, dIntegral / (dStripeEnd - dStripeStart)));
            }

            pt = newpt;
         }
      }

      #endregion

      #region Pausenplatz-Erkennung

      /// <summary>
      /// versucht, Pausen (scheinbare Bewegungen) zu erkennen und zu beseitigen
      /// <para>
      /// Wenn die Punktfolge innerhalb eines Kreises mit vorgegebenen Radius liegt, dieses Polygon min. die vorgegebene Anzahl von 
      /// Kreuzungen mit sich selbst aufweist und der durchschnittliche Winkel zwischen 2 aufeinanderfolgenden Teilstrecken min.
      /// dem vorgegebenen Winkel entspricht, wird diese Punktfolge bis auf den Punkt gelöscht, der dem Umkreismittelpunkt am nächsten 
      /// liegt.
      /// </para>
      /// <para>
      /// Wenn die Anzahl der Kreuzungen zwischen <see cref="crossing1"/> und <see cref="crossing2"/> liegt, werden der 1. Radius und Winkel
      /// verwendet, wenn die Anzahl der Kreuzungen größer oder gleich <see cref="crossing2"/> ist, werden der 2. Radius und Winkel verwendet.
      /// </para>
      /// </summary>
      /// <param name="ptcount">Länge der Punktfolge</param>
      /// <param name="crossing1">1. Anzahl der notwendigen "Kreuzungen"</param>
      /// <param name="maxradius1">1. Maximalradius</param>
      /// <param name="minturnaround1">1. min. durchschnittliche Winkelabweichung</param>
      /// <param name="crossing2">2. Anzahl der notwendigen "Kreuzungen"</param>
      /// <param name="maxradius2">2. Maximalradius</param>
      /// <param name="minturnaround2">2. min. durchschnittliche Winkelabweichung</param>
      /// <param name="protfile">Dateiname für eine Protokolldatei</param>
      public void RemoveRestingplace(int ptcount,
                                     int crossing1, double maxradius1, double minturnaround1,
                                     int crossing2, double maxradius2, double minturnaround2,
                                     string protfile = null) {
         StreamWriter file = null;
         if (!string.IsNullOrEmpty(protfile))
            try {
               file = new StreamWriter(protfile);       // überschreibt vorhandene Datei
            } catch (Exception ex) {
               Console.Error.WriteLine("Fehler beim Erzeugen der Protokolldatei: " + ex.Message);
            }
         if (file != null) {
            file.WriteLine("RemoveRestingplace() für {0} Punkte", ptcount);
            file.WriteLine("   1) {0} Kreuzungen, Radius {1}, Richtungsänderung {2}", crossing1, maxradius1, minturnaround1);
            file.WriteLine("   2) {0} Kreuzungen, Radius {1}, Richtungsänderung {2}", crossing2, maxradius2, minturnaround2);
            file.WriteLine("Punktindex\tKreuzungen\tRadius\tWinkel\tgelöscht");
         }

         minturnaround1 *= Math.PI / 180;
         minturnaround2 *= Math.PI / 180;

         double rx = 0, ry = 0, radius = 0;
         for (int i = 0; i < pt.Count - ptcount; i++) {
            if (pt[i].IsValid) {
               bool delete = false;

               int crossing = GetCrossingCount(i, i + ptcount - 1);
               if (crossing1 <= crossing && crossing < crossing2) {
                  if (minturnaround1 <= GetMeanTurnaround(i, i + ptcount - 1))
                     if ((radius = GetSmallestCircle(i, i + ptcount - 1, out rx, out ry)) <= maxradius1)
                        delete = true;
               } else
                  if (crossing2 < crossing) {
                  if (minturnaround2 <= GetMeanTurnaround(i, i + ptcount - 1))
                     if ((radius = GetSmallestCircle(i, i + ptcount - 1, out rx, out ry)) <= maxradius2)
                        delete = true;
               }

               if (file != null)
                  file.Write(string.Format("{0}\t{1}\t{2:F1}\t{3:F1}",
                     i,
                     crossing,
                     GetSmallestCircle(i, i + ptcount - 1, out rx, out ry),
                     GetMeanTurnaround(i, i + ptcount - 1) * 180 / Math.PI));

               if (delete) {
                  // Wenn Kriterien zutreffen:
                  //    * gültiger Punkt aus dem Intervall mit geringstem Abstand zum Mittelpunkt des Kreises suchen -> bleibt als einziger erhalten
                  //    * alle anderen Punkte des Intervalls ungültig
                  //    * alle folgenden Punkte, die auch in diesem Umkreis sind, sind ungültig
                  //    * mit erstem gültigen Punkt nach dem Intervall weitermachen

                  int valididx = -1;
                  double squaredistance = double.MaxValue;
                  SimplificationPoint m = new SimplificationPoint(rx, ry);
                  for (int j = i; j < i + ptcount - 1; j++) {
                     double dist = m.SquareDistance(pt[j]);
                     if (dist < squaredistance) {
                        squaredistance = dist;
                        valididx = j;
                     }
                  }
                  for (int j = i; j < i + ptcount - 1; j++)
                     if (j != valididx)
                        pt[j].IsValid = false;
                     else
                        pt[j].IsValid = true;

                  radius *= radius;
                  for (int j = i + ptcount; j < pt.Count - ptcount; j++)
                     if (m.SquareDistance(pt[j]) < radius)
                        pt[j].IsValid = false;
                     else
                        break;
               }
               if (file != null)
                  file.WriteLine("\t{0}", pt[i].IsValid ? 0 : 1);

            } else
               if (file != null)
               file.WriteLine("{0}\t{1}\t{2:F1}\t{3:F1}\t{4}",
                  i,
                  GetCrossingCount(i, i + ptcount - 1),
                  GetSmallestCircle(i, i + ptcount - 1, out rx, out ry),
                  GetMeanTurnaround(i, i + ptcount - 1) * 180 / Math.PI,
                  pt[i].IsValid ? 0 : 1);
         }

         // 1. und letzter Punkt sind immer gültig
         if (pt.Count > 0)
            pt[0].IsValid = true;
         if (pt.Count > 1)
            pt[pt.Count - 1].IsValid = true;

         if (file != null) {
            for (int i = pt.Count - ptcount; i < pt.Count; i++)
               file.WriteLine("{0}\t\t\t\t{1}", i, pt[i].IsValid ? 0 : 1);
            file.Close();
         }
      }

      /// <summary>
      /// ermittelt den kleinsten Kreis, der die ausgewählten Punkte umschließt
      /// </summary>
      /// <param name="firstidx">Index des 1. zu berücksichtigenden Punktes</param>
      /// <param name="lastidx">Index des letzten zu berücksichtigenden Punktes</param>
      /// <param name="rx">x-Koordinate des Mittepunktes</param>
      /// <param name="ry">y-Koordinate des Mittepunktes</param>
      /// <returns>Radius</returns>
      double GetSmallestCircle(int firstidx, int lastidx, out double rx, out double ry) {
         double radius = 0;
         rx = ry = 0;
         List<SimplificationPoint> pts = new List<SimplificationPoint>();
         for (int i = firstidx; i <= lastidx && i < pt.Count; i++)
            pts.Add(pt[i]);
         if (pts.Count >= 3) {
            Circle c = Circle.SmallestCircle(pts);
            radius = c.Radius;
            rx = c.Center.X;
            ry = c.Center.Y;
         } else if (pts.Count == 2) {
            radius = pts[0].Distance(pts[1]) / 2;
            rx = pts[0].X + (pts[1].X - pts[0].X) / 2;
            ry = pts[0].Y + (pts[1].Y - pts[0].Y) / 2;
         } else if (pts.Count == 1) {
            rx = pts[0].X;
            ry = pts[0].Y;
         }
         return radius;
      }

      //static public void Test_SmallestCircle() {
      //   for (int i = 0; i < 20; i++) {
      //      List<SimplificationPoint> pts = new List<SimplificationPoint>();
      //      pts.Add(new SimplificationPoint(0, 0));
      //      pts.Add(new SimplificationPoint(17.8007315345031, 28.6832299716601));
      //      pts.Add(new SimplificationPoint(17.7111520136521, 38.4772763330034));
      //      pts.Add(new SimplificationPoint(17.7303475344727, 60.7119048945307));
      //      pts.Add(new SimplificationPoint(17.7815356473597, 63.6286855901544));
      //      pts.Add(new SimplificationPoint(17.2440604466446, 64.4860141013281));
      //      pts.Add(new SimplificationPoint(19.0996258439946, 72.8729234497667));
      //      pts.Add(new SimplificationPoint(19.3939569624157, 74.9137380578868));
      //      pts.Add(new SimplificationPoint(19.6371000206965, 76.246324765472));
      //      pts.Add(new SimplificationPoint(20.340934295968, 81.7816849354415));
      //      pts.Add(new SimplificationPoint(24.5191373006249, 96.2910381082404));
      //      pts.Add(new SimplificationPoint(28.1982545310626, 106.429879631686));
      //      pts.Add(new SimplificationPoint(32.1716946188981, 116.391664179998));
      //      pts.Add(new SimplificationPoint(33.5857526966127, 120.594437642383));
      //      pts.Add(new SimplificationPoint(36.2667029811656, 124.172852297716));

      //      System.Threading.Thread.Sleep(200);
      //      Circle c = Circle.SmallestCircle(pts);

      //      Console.WriteLine(c);
      //   }
      //}

      /// <summary>
      /// Hilfsklasse zur Berechnung des kleinsten Umkreises
      /// </summary>
      protected class Circle {

         /// <summary>
         /// Radius des Kreises
         /// </summary>
         public readonly double Radius;
         /// <summary>
         /// Quadrat des Radius des Kreises (bei einigen Berechnungen nützlich)
         /// </summary>
         public readonly double SquareRadius;
         /// <summary>
         /// Mittelpunkt des Kreises
         /// </summary>
         public readonly SimplificationPoint Center;

         public Circle()
            : this(0, 0, 0) {
         }

         public Circle(double mx, double my, double r) {
            Radius = r;
            SquareRadius = r * r;
            Center = new SimplificationPoint(mx, my);
         }

         public Circle(SimplificationPoint Center, double Radius)
            : this(Center.X, Center.Y, Radius) {
         }

         /// <summary>
         /// Kreis durch 3 Punkte auf dem Rand definiert (Umkreis)
         /// <para>Der Radius 0 ist ein Zeichen dafür, dass kein Kreis gefunden werden konnte.</para>
         /// </summary>
         /// <param name="pa"></param>
         /// <param name="pb"></param>
         /// <param name="pc"></param>
         public Circle(SimplificationPoint pa, SimplificationPoint pb, SimplificationPoint pc)
            : this() {
            /* Der Mittelpunkt des Umkreises ist der Schnittpunkt der 3 Mittelsenkrechten der Seiten des durch A, B, und C
             * gebildeten Dreiecks.
             * Zur einfacheren Berechnung wird zunächst das gesamte Dreieck um -A verschoben, so dass A im Koordinatensprung
             * liegt. Dann werden die beiden Mittelsenkrechten der Strecken AB und AC betrachtet. D sei der Mittelpunkt von AB,
             * E der Mittelpunkt von AC. D ist nun einfach (xb/2, yb/2) und E (xc/2, yc/2). Ein zu AB orthogonaler Vektor (yb, -xb) 
             * beschreibt die Richtung der Mittelsenkrechten in D, der Vektor (yc, -xc) gilt entsprechend für E.
             * Mit M=D+gamma*(yb, -xb) und M=E+delta*(yc, -xc) können 4 Gleichungen formuliert werden, die xm, ym, gamma und delta
             * als Unbekannte enthalten. Die entsprechende Lösung für xm und ym wird für die Berechnung verwendet (s.u.).
             */

            // A temp. als Koordinatenursprung verwenden, um die Berechnung zu vereinfachen -> neue Koordinaten für B und C:
            double xb = pb.X - pa.X;
            double yb = pb.Y - pa.Y;
            double xc = pc.X - pa.X;
            double yc = pc.Y - pa.Y;

            if ((xb == 0 && yb == 0) ||      // A == B
                (xc == 0 && yc == 0) ||      // A == C
                (xb == xc && yb == yc))      // B == C
               return;              // Für (teilweise) identische Punkte wird keine Lösung gesucht.

            /* Wenn der Anstieg mc=yc/xc und mb=yb/xb gleich ist müssen A, B, und auf einer Geraden liegen -> keine Lösung möglich */
            if (xb * yc == xc * yb)
               return;

            double d = 2 * (xb * yc - yb * xc);
            double xm = (yb * (xc * xc + yc * yc) - yc * (xb * xb + yb * yb)) / d;
            double ym = (xb * (xc * xc + yc * yc) - xc * (xb * xb + yb * yb)) / d;

            // Der Radius ist der Abstand von diesem M zum Koordinatenursprung (A).
            SquareRadius = xm * xm + ym * ym;
            Radius = Math.Sqrt(SquareRadius);

            // M noch verschieben, da A i.A. NICHT der Koordinatenursprung ist.
            Center.X = pa.X + xm;
            Center.Y = pa.Y + ym;

            // Da der Mittelpunkt i.A. nur näherungsweise durch double-Zahlen dargestellt werden kann, kann es passieren, dass pa, pb oder pc
            // nicht die Contains()-Funktion erfüllen!
            // Da diese Funktion sehr wichtig ist, wird der Radius notfalls lieber etwas größer gesetzt.
            SquareRadius = Math.Max(Math.Max(SquareDistance(pa, Center), SquareDistance(pb, Center)), SquareDistance(pc, Center));
            Radius = Math.Sqrt(SquareRadius);
         }

         public Circle(SimplificationPoint A, SimplificationPoint B)
            : this(A.X + (B.X - A.X) / 2,
                   A.Y + (B.Y - A.Y) / 2,
                   Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y)) / 2) {
            // Da der Mittelpunkt i.A. nur näherungsweise durch double-Zahlen dargestellt werden kann, kann es passieren, dass A oder B
            // nicht die Contains()-Funktion erfüllen!
            // Da diese Funktion sehr wichtig ist, wird der Radius notfalls lieber etwas größer gesetzt.
            SquareRadius = Math.Max(SquareDistance(A, Center), SquareDistance(B, Center));
            Radius = Math.Sqrt(SquareRadius);
         }

         /// <summary>
         /// Ist der Punkt innerhalb oder auf dem Kreis?
         /// </summary>
         /// <param name="p"></param>
         /// <returns></returns>
         bool Contains(SimplificationPoint p) {
            return SquareDistance(p, Center) <= SquareRadius;
         }

         /// <summary>
         /// Quadrat des Abstandes der 2 Punkte
         /// </summary>
         /// <param name="a"></param>
         /// <param name="b"></param>
         /// <returns></returns>
         double SquareDistance(SimplificationPoint a, SimplificationPoint b) {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
         }

         /// <summary>
         /// Naiver Brute Force-Algorithmus zum Finden des Bounding Balls - O(n^4), d.h. nur für kleine Punktmengen geeignet
         /// </summary>
         /// <param name="pts"></param>
         /// <returns></returns>
         static Circle SmallestCircleNaive(IList<SimplificationPoint> pts) {
            Circle best = new Circle(0, 0, double.MaxValue);     // Start-Kreis (enthält mit Sicherheit alle Punkte)

            // Alle sinnvollen 2-Punkte-Kombinationen ausprobieren und die Beste ermitteln.
            for (int i = 0; i < pts.Count; i++) {
               for (int j = i + 1; j < pts.Count; j++) {
                  Circle test = new Circle(pts[i], pts[j]);

                  bool bContainsAll = true;
                  for (int k = 0; k < pts.Count; k++)
                     if (k != i &&
                         k != j &&
                         !test.Contains(pts[k])) {
                        bContainsAll = false;
                        break;
                     }

                  if (bContainsAll &&
                      test.Radius < best.Radius)
                     best = test;
               }
            }

            if (best.Radius == double.MaxValue)       // keine 2-Punkte-Kombination gefunden, die alle anderen Punkte enthält
               // Alle sinnvollen 3-Punkte-Kombinationen ausprobieren und die Beste ermitteln.
               for (int i = 0; i < pts.Count; i++) {
                  for (int j = i + 1; j < pts.Count; j++) {
                     for (int k = j + 1; k < pts.Count; k++) {
                        bool bContainsAll = true;
                        Circle test = new Circle(pts[i], pts[j], pts[k]);

                        for (int l = 0; l < pts.Count; l++)
                           if (l != i &&
                               l != j &&
                               l != k &&
                               !test.Contains(pts[l])) {
                              bContainsAll = false;
                              break;
                           }

                        if (bContainsAll &&
                            test.Radius < best.Radius)
                           best = test;
                     }
                  }
               }
            return best;
         }

         /// <summary>
         /// ermittelt den kleinsten Kreis, der alle gegebenen Punkte einschließt
         /// </summary>
         /// <param name="pts">gegebenen Punkte</param>
         /// <param name="max_generation">damit könnte die Iteration abgebrochen werden</param>
         /// <returns></returns>
         static public Circle SmallestCircle(IList<SimplificationPoint> pts, int max_generation = int.MaxValue) {
            Random rand = new Random();
            Circle circle = new Circle();

            if (pts.Count > 13) {
               List<int> voices = new List<int>();       // "Stimmen" je Punkt
               for (int i = 0; i < pts.Count; i++)
                  voices.Add(1);                         // am Anfang alle gleichberechtigt: je 1 Stimme

               int iGeneration = 0;
               int iMistakes = 0;

               HashSet<int> voiceindices = new HashSet<int>();                            // Indizes der ausgewählten Stimmzettel speichern
               List<SimplificationPoint> randpts = new List<SimplificationPoint>();       // ausgewählte Punkte speichern
               do {
                  iGeneration++;
                  voiceindices.Clear();
                  randpts.Clear();

                  // Gesamtzahl der Stimmen im "Lostopf"
                  int voicessum = 0;
                  for (int i = 0; i < voices.Count; i++)
                     voicessum += voices[i];

                  // 13 zufällige, per Stimmzahl gewichtete Punkte wählen
                  while (randpts.Count < 13) {
                     int idx = rand.Next(0, voicessum);        // "Stimmzettel auslosen"

                     if (!voiceindices.Contains(idx)) {        // dieser Stimmzettel wurde bisher noch nicht gezogen
                        voiceindices.Add(idx);

                        int p = 0;
                        int lastvoiceidx = voices[0] - 1;      // Index des letzten Stimmzettels für den Punkt p

                        while (lastvoiceidx < idx)
                           lastvoiceidx += voices[++p];

                        if (!randpts.Contains(pts[p]))
                           randpts.Add(pts[p]);
                     }
                  }

                  //randpts.Clear();
                  //randpts.Add(pts[4]);
                  //randpts.Add(pts[9]);
                  //randpts.Add(pts[7]);
                  //randpts.Add(pts[6]);
                  //randpts.Add(pts[3]);
                  //randpts.Add(pts[5]);
                  //randpts.Add(pts[0]);
                  //randpts.Add(pts[10]);
                  //randpts.Add(pts[2]);
                  //randpts.Add(pts[14]);
                  //randpts.Add(pts[13]);
                  //randpts.Add(pts[11]);
                  //randpts.Add(pts[8]);

                  circle = SmallestCircleNaive(randpts);

                  // Fehler zählen und Stimmen angleichen
                  iMistakes = 0;
                  for (int i = 0; i < pts.Count; i++) {
                     if (!circle.Contains(pts[i])) {           // Punkt nicht enthalten
                        voices[i] *= 2;                        // --> seine "Stimmen" verdoppeln
                        iMistakes++;
                     }
                  }
                  //Debug.WriteLine(string.Format("In Generation {0} noch {1} Fehler", m_Generation, iNumMistakes));
               } while (iMistakes > 0 &&
                        max_generation > iGeneration);

            } else
               circle = SmallestCircleNaive(pts);

            return circle;
         }

         public override string ToString() {
            return string.Format("Radius {0}, Mittelpunkt {1}", Radius, Center); ;
         }

      }

      /// <summary>
      /// liefert die durchschnittliche Winkelabweichung von Punkt zu Punkt (0..Math.PI)
      /// </summary>
      /// <param name="firstidx"></param>
      /// <param name="lastidx"></param>
      /// <returns></returns>
      double GetMeanTurnaround(int firstidx, int lastidx) {
         double ta = 0;
         int count = 0;
         for (int i = firstidx + 2; i <= lastidx && i < pt.Count; i++) {
            count++;
            SimplificationPoint p1 = pt[i - 2];
            SimplificationPoint p2 = pt[i - 1];
            SimplificationPoint p3 = pt[i];
            p3 -= p2;
            p2 -= p1;
            double arc = p2.Arc(p3);
            ta += arc;
            //Debug.WriteLine("Winkel {0}°, Punk1 {1}, Punk2 {2}", arc * 180 / Math.PI, p2, p3);
         }
         return count > 1 ? ta / count : ta;
      }


      /// <summary>
      /// liefert die durchschnittliche Winkelabweichung von Punkt zu Punkt (0..Math.PI) und die zugehörige Standardabweichung
      /// (die Standardabweichung hat leider keine Zusammenhang zur "Pause")
      /// </summary>
      /// <param name="firstidx"></param>
      /// <param name="lastidx"></param>
      /// <param name="sa">Standardabweichung</param>
      /// <returns></returns>
      double GetMeanTurnaround_V2(int firstidx, int lastidx, out double sa) {
         List<double> v = new List<double>();
         for (int i = firstidx + 2; i <= lastidx && i < pt.Count; i++) {
            SimplificationPoint p1 = pt[i - 2];
            SimplificationPoint p2 = pt[i - 1];
            SimplificationPoint p3 = pt[i];
            p3 -= p2;
            p2 -= p1;
            v.Add(p2.Arc(p3));
         }

         double ta = 0;
         for (int i = 0; i < v.Count; i++)
            ta += v[i];
         if (v.Count > 0)
            ta /= v.Count;          // Durchschnitt

         double var = 0;
         for (int i = 0; i < v.Count; i++)
            var += (ta - v[i]) * (ta - v[i]);
         sa = Math.Sqrt(var);

         return ta;
      }

      /// <summary>
      /// liefert die Anzahl der Schnittpunkte für diesen Polygonzug
      /// </summary>
      /// <param name="firstidx"></param>
      /// <param name="lastidx"></param>
      /// <returns></returns>
      int GetCrossingCount(int firstidx, int lastidx) {
         int crossing = 0;

         for (int i = firstidx; i <= lastidx - 1 && i < pt.Count - 1; i++) {
            SimplificationPoint p1 = pt[i];
            SimplificationPoint p2 = pt[i + 1];
            for (int j = i + 1; j <= lastidx - 1 && j < pt.Count - 1; j++) {
               SimplificationPoint p3 = pt[j];
               SimplificationPoint p4 = pt[j + 1];

               if (InRightHalfplane(p1, p2, p3) * InRightHalfplane(p1, p2, p4) < 0 &&    // p3 und p4 in unterschiedlichen Halbebenen bzgl. p1p2
                   InRightHalfplane(p3, p4, p1) * InRightHalfplane(p3, p4, p2) < 0)      // p1 und p2 in unterschiedlichen Halbebenen bzgl. p3p4
                  crossing++;
            }
         }

         return crossing;
      }

      /// <summary>
      /// ermittelt die Lage von p2 bzgl. des Vektors p0p1
      /// </summary>
      /// <param name="p0"></param>
      /// <param name="p1"></param>
      /// <param name="p2"></param>
      /// <returns>1 wenn p2 rechts liegt, -1 wenn p2 links liegt</returns>
      int InRightHalfplane(SimplificationPoint p0, SimplificationPoint p1, SimplificationPoint p2) {
         //dx1:=p1.x-p0.x; dy1:=p1.y-p0.y;
         //dx2:=p2.x-p0.x; dy2:=p2.y-p0.y;
         //if dx1*dy2>dy1*dx2 then ccw:=1;
         //if dx1*dy2<dy1*dx2 then ccw:=-1;
         //if dx1*dy2=dy1*dx2 then 
         //  begin
         //  if (dx1*dx2<0) or (dy1*dy2<0) then ccw:=-1 else
         //  if (dx1*dx1+dy1*dy1)>=(dx2*dx2+dy2*dy2) then ccw:=0 else ccw:=1;
         //  end;
         double dx1 = p1.X - p0.X;
         double dy1 = p1.Y - p0.Y;
         double dx2 = p2.X - p0.X;
         double dy2 = p2.Y - p0.Y;
         if (dx1 * dy2 > dy1 * dx2)
            return 1;
         if (dx1 * dy2 < dy1 * dx2)
            return -1;
         // dx1 * dy2 == dy1 * dx2  --> Anstieg identisch
         if (dx1 * dx2 < 0 || dy1 * dy2 < 0)
            return -1;
         if ((dx1 * dx1 + dy1 * dy1) >= (dx2 * dx2 + dy2 * dy2))
            return 0;
         return 1;
      }

      #endregion


   }

}