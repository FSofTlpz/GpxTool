using System;
using System.Collections.Generic;

namespace FSofTUtils.Geometry.PolylineSimplification {

   public class PointList {
      protected List<Point> pt;

      public PointList(int iLength) {
         if (iLength < 3)
            throw new ArgumentException("Es sind min. 3 Punkte für die Liste nötig.");
         pt = new List<Point>(new Point[iLength]);
      }

      public PointList(PointList pl) {
         Point[] tmp = new Point[pl.Length];
         pl.pt.CopyTo(tmp);
         pt = new List<Point>(tmp);
      }

      public void Set(int no, Point p) {
         IndexTest(no);
         if (pt[no] == null)
            pt[no] = new Point(p);
         else {
            pt[no].X = p.X;
            pt[no].Y = p.Y;
            pt[no].IsLocked = p.IsLocked;
            pt[no].IsValid = p.IsValid;
         }
      }

      public void Set(int no, double x, double y) {
         IndexTest(no);
         if (pt[no] == null)
            pt[no] = new Point(x, y);
         else {
            pt[no].X = x;
            pt[no].Y = y;
            pt[no].IsLocked = false;
            pt[no].IsValid = true;
         }
      }

      public Point Get(int no) {
         IndexTest(no);
         return new Point(pt[no]);
      }

      public void Get(int no, ref Point p) {
         IndexTest(no);
         p.X = pt[no].X;
         p.Y = pt[no].Y;
         p.IsLocked = pt[no].IsLocked;
         p.IsValid = pt[no].IsValid;
      }

      public void Remove(int idx) {
         if (0 <= idx && idx < pt.Count)
            pt.RemoveAt(idx);
      }

      public int Length { get { return pt.Count; } }

      private void IndexTest(int no) {
         if (no < 0 || pt.Count <= no)
            throw new ArgumentException("Der Punktindex liegt außerhalb des gültigen Bereichs.");
      }

      /// <summary>
      /// Reumann-Witkam-Algorithmus;
      /// i.A. sollte der letzte Punkt vorher 'gelocked' werden
      /// </summary>
      /// <param name="dWidth">halbe Korridorbreite</param>
      public void ReumannWitkam(double dWidth) {
         int iStart = 0;
         int iNext = 1;
         while (pt[iStart].Equals(pt[iNext]) && iNext < Length) iNext++;      // ungleichen Punkt suchen
         int iTest = iNext + 1;
         dWidth *= dWidth;

         for (int i = 0; i < Length; i++)
            pt[i].IsValid = true;

         while (iTest < Length) {
            bool bPointIsValid = pt[iTest].IsLocked;
            if (!bPointIsValid) {            // Test ist nötig (Normalfall)
               Point p0 = pt[iStart];
               Point p1 = pt[iNext];
               Point p2 = pt[iTest];
               // teste, ob p2 innerhalb der durch p0, p1 und dWidth vorgegebenen Bandbreite liegt

               // Das fkt. sehr einfach mit Hilfe des Skalarproduktes. Es wird nicht der Abstand, sondern das Quadrat des Abstandes
               // berechnet. Man spart sich das Ziehen der Wurzel und es gibt keine Probleme mit ev. negativen Werten.

               Point p0p1 = p0 - p1;
               Point p1p2 = p1 - p2;
               double dDotProduct = p0p1.DotProduct(p1p2);
               double dSquare_AbsP0P1 = p0p1.SquareAbsolute();
               double dSquare_WidthTest = (dSquare_AbsP0P1 * p1p2.SquareAbsolute() - dDotProduct * dDotProduct) / dSquare_AbsP0P1;

               bPointIsValid = dSquare_WidthTest > dWidth;
            }

            if (bPointIsValid) {      // p2 bleibt erhalten
               pt[iNext].IsValid = false;
               iStart = iTest;
               iNext = iStart + 1;
               while (iNext < Length && pt[iStart].Equals(pt[iNext])) iNext++;      // ungleichen Punkt suchen
               iTest = iNext + 1;
               while (iTest < Length && pt[iNext].Equals(pt[iTest])) iTest++;       // ungleichen Punkt suchen
            } else {
               pt[iTest].IsValid = false;
               do {
                  iTest++;
               } while (iTest < Length && pt[iNext].Equals(pt[iTest]));             // ungleichen Punkt suchen
            }
         }
         pt[Length - 1].IsValid = true;
      }

      /// <summary>
      /// Douglas-Peucker-Algorithmus
      /// </summary>
      /// <param name="dWidth"></param>
      public void DouglasPeucker(double dWidth) {
         for (int i = 0; i < Length; i++)
            pt[i].IsValid = false;
         pt[0].IsValid =
         pt[Length - 1].IsValid = true;
         DouglasPeuckerRecursive(0, Length - 1, dWidth * dWidth);
         for (int i = 0; i < Length; i++)
            if (pt[i].IsLocked)
               pt[i].IsValid = true;
      }

      private void DouglasPeuckerRecursive(int iStart, int iEnd, double dSquareWidth) {
         int idx = GetFarPointIdx4DouglasPeucker(iStart, iEnd, dSquareWidth);
         if (idx > 0) {
            pt[idx].IsValid = true;
            if (idx - iStart > 1)
               DouglasPeuckerRecursive(iStart, idx, dSquareWidth);
            if (iEnd - idx > 1)
               DouglasPeuckerRecursive(idx, iEnd, dSquareWidth);
         }
      }

      private int GetFarPointIdx4DouglasPeucker(int iStart, int iEnd, double dSquareWidth) {
         double dMaxSquareWidth = dSquareWidth;
         int idx = -1;
         for (int i = iStart + 1; i < iEnd - 1; i++) {
            Point pBaseLine = pt[iEnd] - pt[iStart];
            Point pTestLine = pt[i] - pt[iStart];
            double dDotProduct = pBaseLine.DotProduct(pTestLine);
            double dSquare_AbsBaseLine = pBaseLine.SquareAbsolute();
            double dSquare_WidthTest = (dSquare_AbsBaseLine * pTestLine.SquareAbsolute() - dDotProduct * dDotProduct) / dSquare_AbsBaseLine;
            if (dMaxSquareWidth < dSquare_WidthTest) {
               dMaxSquareWidth = dSquare_WidthTest;
               idx = i;
            }
         }
         return idx;
      }

      /// <summary>
      /// führt eine Integration für 'dWidth' breite Streifen durch und liefert nur noch je Streifen 1 Punkt
      /// </summary>
      /// <param name="dWidth"></param>
      public void HeigthProfileWithIntegral(double dWidth) {
         List<Point> newpt = new List<Point>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         if (pt.Count >= 2) {
            double dStripeStart = 0.0;
            double dX0 = pt[0].X;
            double dY0 = pt[0].Y;
            double dTripLength = pt[pt.Count - 1].X;
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
               newpt.Add(new Point(dStripeEnd, dIntegral));
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
         List<Point> newpt = new List<Point>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         int iFirstIdx = -count / 2;
         int iLastIdx = iFirstIdx + count - 1;
         newpt.Add(new Point(pt[0].X, pt[0].Y));
         for (int i = 1; i < pt.Count; i++) {
            int iStartIdx = Math.Max(1, i + iFirstIdx);
            int iEndIdx = Math.Min(pt.Count - 1, i + iLastIdx);
            double dSum = 0;
            for (int j = iStartIdx; j <= iEndIdx; j++)      // iStartIdx > 0 !
               dSum += pt[j].Y * (pt[j].X - pt[j - 1].X);
            dSum /= pt[iEndIdx].X - pt[iStartIdx - 1].X;
            newpt.Add(new Point(pt[i].X, dSum));
         }
         pt = newpt;
      }

      /// <summary>
      /// führt eine Integration für max. 'dWidth' breite Streifen um den jeweiligen Punkt durch (+- dWidth/2)
      /// </summary>
      /// <param name="dWidth"></param>
      public void HeigthProfileWithSlidingIntegral(double dWidth) {
         List<Point> newpt = new List<Point>();
         // Punktliste: X = Entfernung vom Start, Y = Höhe in dieser Entfernung
         if (pt.Count >= 2) {
            newpt.Add(new Point(pt[0]));
            dWidth /= 2;
            Point pStart = new Point();
            Point pEnd = new Point();
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
                  Point p1 = pt[p];
                  if (p1.X < dStripeStart) p1 = pStart;
                  Point p2 = pt[p + 1];
                  if (p2.X > dStripeEnd) p2 = pEnd;

                  dIntegral += (p1.Y + (p2.Y - p1.Y) / 2) * (p2.X - p1.X);
               }

               newpt.Add(new Point(pt[i].X, dIntegral / (dStripeEnd - dStripeStart)));
            }

            pt = newpt;
         }
      }

   }

}