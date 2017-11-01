using System;
using System.Collections.Generic;
using FSoftUtils;
using System.IO;

namespace GpxTool {

   /// <summary>
   /// Optionen und Argumente werden zweckmäßigerweise in eine (programmabhängige) Klasse gekapselt.
   /// Erzeugen des Objektes und Evaluate() sollten in einem try-catch-Block erfolgen.
   /// </summary>
   public class Options {

      /// <summary>
      /// Typ der horizontalen Track-Vereinfachung
      /// </summary>
      public enum HSimplification {
         Nothing,
         Douglas_Peucker,
         Reumann_Witkam
      }

      /// <summary>
      /// Typ der vertikalen Track-Vereinfachung
      /// </summary>
      public enum VSimplification {
         Nothing,
         /// <summary>
         /// gleitenden Mittelwert mit Wichtung der zugehörigen Teil-Streckenlänge
         /// </summary>
         SlidingMean,
         /// <summary>
         /// Integration für variable Streifenbreite um den jeweiligen Punkt
         /// </summary>
         SlidingIntegral
      }

      // alle Optionen sind 'read-only'

      /// <summary>
      /// Infos über die Tracks und Segmente ausgeben
      /// </summary>
      public bool ShowInfo { get; private set; }
      /// <summary>
      /// Array der neuen Tracknamen
      /// </summary>
      public string[] NewTrackName { get; private set; }
      /// <summary>
      /// Tracks mit dem Dateinamen bezeichnen
      /// </summary>
      public bool Filename2TrackName { get; private set; }
      /// <summary>
      /// Name der Ausgabedatei
      /// </summary>
      public string Outputfile { get; private set; }
      /// <summary>
      /// Name der Datei für die Höhenprofildaten
      /// </summary>
      public string HeightOutputfile { get; private set; }
      /// <summary>
      /// Liste der auszugebenden Tracks (wenn null dann alle, wenn Listenlänge 0 dann keiner)
      /// </summary>
      public int[] Output4Tracks { get; private set; }
      /// <summary>
      /// alle Zeitstempel entfernen
      /// </summary>
      public bool DeleteTimestamp { get; private set; }
      /// <summary>
      /// alle Höhenangaben entfernen
      /// </summary>
      public bool DeleteHeight { get; private set; }
      /// <summary>
      /// alle Punkte auf diese Höhe setzen
      /// </summary>
      public double ConstantHeight { get; private set; }
      /// <summary>
      /// fehlende Höhenwerte und Zeitstempel linear interpolieren
      /// </summary>
      public bool GapFill { get; private set; }
      /// <summary>
      /// Art der horizontalen Vereinfachung
      /// </summary>
      public HSimplification HorizontalSimplification { get; private set; }
      /// <summary>
      /// Breite des Toleranzbereiches für die Vereinfachung (in m, Standard 5)
      /// </summary>
      public double HorizontalWidth { get; private set; }
      /// <summary>
      /// maximal erlaubte Geschwindigkeit
      /// </summary>
      public double HorizontalMaxSpeed { get; private set; }
      /// <summary>
      /// Parameter für die Pausenplatzberechnung
      /// </summary>
      public int[] HorizontalRestArea { get; private set; }
      /// <summary>
      /// Ausgabedatei für die Pausenplatzberechnung
      /// </summary>
      public string HorizontalRestAreaProt { get; private set; }
      /// <summary>
      /// Art der vertikalen Vereinfachung
      /// </summary>
      public VSimplification VerticalSimplification { get; private set; }
      /// <summary>
      /// min. erlaubte Höhe
      /// </summary>
      public double MinHeight { get; private set; }
      /// <summary>
      /// max. erlaubte Höhe
      /// </summary>
      public double MaxHeight { get; private set; }
      /// <summary>
      /// Breite des Höhen-Integrationsbereiches in Metern (Standard 250m)
      /// </summary>
      public double VerticalWidth { get; private set; }
      /// <summary>
      /// Breite des Bereiches für die "Ausreißer"-Korrektur von Höhen (Standard 100m)
      /// </summary>
      public double VerticalOutlierWidth { get; private set; }
      /// <summary>
      /// max. gültiger An-/Abstieg in Prozent (Standard 25%)
      /// </summary>
      public double MaxAscent { get; private set; }
      /// <summary>
      /// Liste der auszugebenden Routen (wenn null dann alle, wenn Listenlänge 0 dann keiner)
      /// </summary>
      public int[] Output4Routes { get; private set; }
      /// <summary>
      /// Liste der auszugebenden Waypoints (wenn null dann alle, wenn Listenlänge 0 dann keiner)
      /// </summary>
      public int[] Output4Waypoints { get; private set; }
      /// <summary>
      /// Ausgabe formatiert oder "1-zeilig"
      /// </summary>
      public bool FormatedOutput { get; private set; }

      /// <summary>
      /// Programm-Parameter
      /// </summary>
      public List<string> Inputfile { get; private set; }

      CmdlineOptions cmd;

      /// <summary>
      /// Liste der Optionen (Reihenfolge ist für die Hilfeanzeige wichtig)
      /// </summary>
      enum MyOptions {
         ShowInfo,
         NewTrackName,
         Outputfile,
         FormatedOutput,

         Output4Tracks,
         Output4Routes,
         Output4Waypoints,

         DeleteTimestamp,
         DeleteHeight,
         GapFill,

         ConstantHeight,
         HorizontalSimplification,
         HorizontalWidth,
         HorizontalMaxSpeed,
         HorizontalRestArea,
         HorizontalRestAreaProt,
         HeightOutputfile,

         MinHeight,
         MaxHeight,

         VerticalSimplification,
         VerticalWidth,
         VerticalOutlierWidth,
         MaxAscent,

         Filename2TrackName,
         Help
      }

      public Options() {
         try {

            cmd = new CmdlineOptions();

            // Definition der Optionen
            ShowInfo = true;
            cmd.DefineOption((int)MyOptions.ShowInfo, "info", "i", "Ausgabe von Waypoint-, Routen-, Track- und Segment-Infos auf STDOUT (Name, Länge usw.)" + System.Environment.NewLine +
                                                                   "(Standard: true)", CmdlineOptions.OptionArgumentType.BooleanOrNothing);
            NewTrackName = new string[0];
            cmd.DefineOption((int)MyOptions.NewTrackName, "name", "n", "neuer Trackname (mehrfach verwendbar für Track 1 usw.)", CmdlineOptions.OptionArgumentType.String, int.MaxValue);
            Outputfile = "";
            cmd.DefineOption((int)MyOptions.Outputfile, "output", "o", "Name der Ausgabedatei für die (ev. veränderten) GPX-Daten", CmdlineOptions.OptionArgumentType.String);
            FormatedOutput = true;
            cmd.DefineOption((int)MyOptions.FormatedOutput, "formatted", "f", "Ausgabe formatiert oder '1-zeilig' (Standard: true)", CmdlineOptions.OptionArgumentType.BooleanOrNothing);

            HeightOutputfile = "";
            cmd.DefineOption((int)MyOptions.HeightOutputfile, "heightoutput", "O", "Name der Ausgabedatei für die (ev. veränderten) Höhen-Daten in Abhängigkeit" + System.Environment.NewLine +
                                                                                   "der jeweiligen Tracklänge", CmdlineOptions.OptionArgumentType.String);
            Output4Tracks = null;
            cmd.DefineOption((int)MyOptions.Output4Tracks, "tracks", "t", "Liste (mit Komma) der zu verwendenden Tracknummern (1, ...) (Standard: alle)", CmdlineOptions.OptionArgumentType.String);
            Output4Routes = null;
            cmd.DefineOption((int)MyOptions.Output4Routes, "routes", "r", "Liste (mit Komma) der zu verwendenden Routennummern (1, ...) (Standard: alle)", CmdlineOptions.OptionArgumentType.String);
            Output4Waypoints = null;
            cmd.DefineOption((int)MyOptions.Output4Waypoints, "waypoints", "p", "Liste (mit Komma) der zu verwendenden Waypoints (1, ...) (Standard: alle)", CmdlineOptions.OptionArgumentType.String);

            DeleteTimestamp = false;
            cmd.DefineOption((int)MyOptions.DeleteTimestamp, "deletetime", "", "alle Zeitstempel werden aus den Trackpunkten entfernt", CmdlineOptions.OptionArgumentType.BooleanOrNothing);
            DeleteHeight = false;
            cmd.DefineOption((int)MyOptions.DeleteHeight, "deleteheight", "", "alle Höhenangaben werden aus den Trackpunkten entfernt", CmdlineOptions.OptionArgumentType.BooleanOrNothing);
            ConstantHeight = double.MinValue;
            cmd.DefineOption((int)MyOptions.ConstantHeight, "newheigth", "N", "alle Höhen werden in den Trackpunkten auf einen konstanten Wert gesetzt", CmdlineOptions.OptionArgumentType.Double);
            GapFill = false;
            cmd.DefineOption((int)MyOptions.GapFill, "gapfill", "G", "fehlende Höhenwerte und Zeitstempel linear interpolieren (Standard: false)", CmdlineOptions.OptionArgumentType.BooleanOrNothing);

            HorizontalSimplification = HSimplification.Nothing;
            cmd.DefineOption((int)MyOptions.HorizontalSimplification, "simplify", "s", "Vereinfachung der Tracks [mit Algorithmus Reumann-Witkam (RW) oder Douglas-Peucker (DP)]" + System.Environment.NewLine +
                                                                                       "(Standard: keine)", CmdlineOptions.OptionArgumentType.StringOrNothing);
            HorizontalWidth = 0.05;
            cmd.DefineOption((int)MyOptions.HorizontalWidth, "width", "w", "Breite des Toleranzbereiches für die Vereinfachung (Standard 0.05)", CmdlineOptions.OptionArgumentType.PositivDouble);
            HorizontalMaxSpeed = 0;
            cmd.DefineOption((int)MyOptions.HorizontalMaxSpeed, "maxspeed", "m", "Punkte entfernen, die mit einer höheren Geschwindigkeit in km/h erreicht werden" + System.Environment.NewLine +
                                                                                 "(Standard: inaktiv)", CmdlineOptions.OptionArgumentType.PositivDouble);
            HorizontalRestArea = null;
            cmd.DefineOption((int)MyOptions.HorizontalRestArea, "restarea", "a", "Werteliste (mit Komma) für die Pausenplatzeliminierung, z.B. 10,1,20,60,2,25,50" + System.Environment.NewLine +
                                                                                 "(Standard: inaktiv)", CmdlineOptions.OptionArgumentType.String);
            HorizontalRestAreaProt = null;
            cmd.DefineOption((int)MyOptions.HorizontalRestAreaProt, "restareaprot", "", "Name der Protokolldatei für die Pausenplatzeliminierung", CmdlineOptions.OptionArgumentType.String);

            MinHeight = double.MinValue;
            cmd.DefineOption((int)MyOptions.MinHeight, "minheight", "", "Minimalhöhe; alle kleineren Höhen werden damit ersetzt", CmdlineOptions.OptionArgumentType.Double);
            MaxHeight = double.MaxValue;
            cmd.DefineOption((int)MyOptions.MaxHeight, "maxheight", "", "Maximalhöhe; alle größeren Höhen werden damit ersetzt", CmdlineOptions.OptionArgumentType.Double);

            VerticalSimplification = VSimplification.Nothing;
            cmd.DefineOption((int)MyOptions.VerticalSimplification, "heightsimplify", "S", "Höhenprofil vereinfachen [mit Algorithmus SlidingIntegral (SI) oder SlidingMean (SM)]" + System.Environment.NewLine +
                                                                                           "(Standard: keine)", CmdlineOptions.OptionArgumentType.StringOrNothing);
            VerticalWidth = 100;
            cmd.DefineOption((int)MyOptions.VerticalWidth, "heightwidth", "W", "Breite des Höhen-Integrationsbereiches in Metern (Standard 100m)", CmdlineOptions.OptionArgumentType.PositivDouble);
            VerticalOutlierWidth = 50;
            cmd.DefineOption((int)MyOptions.VerticalOutlierWidth, "heightoutlierwidth", "U", "Länge des Bereiches für die 'Ausreißer'-Korrektur von Höhen (Standard 50m)", CmdlineOptions.OptionArgumentType.UnsignedDouble);
            MaxAscent = 25;
            cmd.DefineOption((int)MyOptions.MaxAscent, "maxascent", "A", "max. gültiger An-/Abstieg in Prozent (Standard 25%)", CmdlineOptions.OptionArgumentType.PositivDouble);
            Filename2TrackName = false;
            cmd.DefineOption((int)MyOptions.Filename2TrackName, "filenametotrackname", "", "Tracknamen auf den Dateinamen setzen (Standard: false)", CmdlineOptions.OptionArgumentType.BooleanOrNothing);


            cmd.DefineOption((int)MyOptions.Help, "help", "?", "diese Hilfe", CmdlineOptions.OptionArgumentType.Nothing);

         } catch (Exception ex) {
            Console.Error.WriteLine(ex.Message);
         }
      }


      /// <summary>
      /// Auswertung der Optionen
      /// </summary>
      /// <param name="args"></param>
      public void Evaluate(string[] args) {
         if (args == null) return;
         List<string> NewTrackName_Tmp = new List<string>();
         try {
            cmd.Parse(args);

            foreach (MyOptions opt in Enum.GetValues(typeof(MyOptions))) {    // jede denkbare Option testen
               int optcount = cmd.OptionAssignment((int)opt);                 // Wie oft wurde diese Option verwendet?
               if (optcount > 0)
                  switch (opt) {
                     case MyOptions.ShowInfo:
                        if (cmd.ArgIsUsed((int)opt))
                           ShowInfo = cmd.BooleanValue((int)opt);
                        else
                           ShowInfo = true;
                        break;

                     case MyOptions.NewTrackName:
                        for (int i = 0; i < optcount; i++)
                           NewTrackName_Tmp.Add(cmd.StringValue((int)opt, i));
                        break;

                     case MyOptions.FormatedOutput:
                        if (cmd.ArgIsUsed((int)opt))
                           FormatedOutput = cmd.BooleanValue((int)opt);
                        else
                           FormatedOutput = true;
                        break;

                     case MyOptions.Outputfile:
                        Outputfile = cmd.StringValue((int)opt);
                        break;

                     case MyOptions.HeightOutputfile:
                        HeightOutputfile = cmd.StringValue((int)opt);
                        break;

                     case MyOptions.Output4Tracks:
                        Output4Tracks = GetIndexArray(cmd.StringValue((int)opt));
                        break;

                     case MyOptions.DeleteTimestamp:
                        if (cmd.ArgIsUsed((int)opt))
                           DeleteTimestamp = cmd.BooleanValue((int)opt);
                        else
                           DeleteTimestamp = true;
                        break;

                     case MyOptions.DeleteHeight:
                        if (cmd.ArgIsUsed((int)opt))
                           DeleteHeight = cmd.BooleanValue((int)opt);
                        else
                           DeleteHeight = true;
                        break;

                     case MyOptions.ConstantHeight:
                        ConstantHeight = cmd.DoubleValue((int)opt);
                        break;

                     case MyOptions.GapFill:
                        if (cmd.ArgIsUsed((int)opt))
                           GapFill = cmd.BooleanValue((int)opt);
                        else
                           GapFill = true;
                        break;

                     case MyOptions.HorizontalSimplification: {
                           string tmp = null;
                           if (cmd.ArgIsUsed((int)opt))
                              tmp = cmd.StringValue((int)opt).ToUpper();
                           if (string.IsNullOrEmpty(tmp) ||
                               tmp == "DP" ||
                               tmp == "Douglas-Peucker")
                              HorizontalSimplification = HSimplification.Douglas_Peucker;
                           else if (tmp == "RW" ||
                                    tmp == "Reumann-Witkam")
                              HorizontalSimplification = HSimplification.Reumann_Witkam;
                           else
                              throw new Exception("Unbekannter Typ: " + tmp);
                        }
                        break;

                     case MyOptions.HorizontalWidth:
                        HorizontalWidth = cmd.PositivDoubleValue((int)opt);
                        break;

                     case MyOptions.HorizontalMaxSpeed:
                        HorizontalMaxSpeed = cmd.PositivDoubleValue((int)opt);
                        break;

                     case MyOptions.HorizontalRestArea:
                        HorizontalRestArea = GetParaArray(cmd.StringValue((int)opt));
                        break;

                     case MyOptions.HorizontalRestAreaProt:
                        HorizontalRestAreaProt = cmd.StringValue((int)opt);
                        break;

                     case MyOptions.MinHeight:
                        MinHeight=cmd.DoubleValue((int)opt);
                        break;

                     case MyOptions.MaxHeight:
                        MaxHeight = cmd.DoubleValue((int)opt);
                        break;

                     case MyOptions.VerticalSimplification: {
                           string tmp = null;
                           if (cmd.ArgIsUsed((int)opt))
                              tmp = cmd.StringValue((int)opt).ToUpper();
                           if (string.IsNullOrEmpty(tmp) ||
                               tmp == "SI" ||
                               tmp == "SlidingIntegral")
                              VerticalSimplification = VSimplification.SlidingIntegral;
                           else if (tmp == "SM" ||
                                    tmp == "SlidingMean")
                              VerticalSimplification = VSimplification.SlidingMean;
                           else
                              throw new Exception("Unbekannter Typ: " + tmp);
                        }
                        break;

                     case MyOptions.VerticalWidth:
                        VerticalWidth = cmd.PositivDoubleValue((int)opt);
                        break;

                     case MyOptions.VerticalOutlierWidth:
                        VerticalOutlierWidth = cmd.UnsignedDoubleValue((int)opt);
                        break;

                     case MyOptions.MaxAscent:
                        MaxAscent = cmd.PositivDoubleValue((int)opt);
                        break;

                     case MyOptions.Output4Routes:
                        Output4Routes = GetIndexArray(cmd.StringValue((int)opt));
                        break;

                     case MyOptions.Output4Waypoints:
                        Output4Waypoints = GetIndexArray(cmd.StringValue((int)opt));
                        break;

                     case MyOptions.Filename2TrackName:
                        if (cmd.ArgIsUsed((int)opt))
                           Filename2TrackName = cmd.BooleanValue((int)opt);
                        else
                           Filename2TrackName = true;
                        break;


                     case MyOptions.Help:
                        ShowHelp();
                        break;
                  }
            }

            Console.Error.WriteLine(cmd);

            Inputfile = new List<string>();
            foreach (string item in cmd.Parameters)
               Inputfile.AddRange(CmdlineOptions.WildcardExpansion4Files(item));

            Console.Error.WriteLine(Inputfile.Count);



            //if (cmd.Parameters.Count > 0)
            //   throw new Exception("Es sind keine Argumente sondern nur Optionen erlaubt.");

            NewTrackName = new string[NewTrackName_Tmp.Count];
            NewTrackName_Tmp.CopyTo(NewTrackName);

            if (VerticalSimplification != VSimplification.Nothing) {
               GapFill = true;
            } else
               VerticalOutlierWidth = 0;

            if (GapFill) {
               DeleteHeight = false;
               DeleteTimestamp = false;
            }

            if (MinHeight > MaxHeight) {
               MaxHeight = double.MaxValue;
            }

            if (ConstantHeight != double.MinValue)
               DeleteHeight = false;

         } catch (Exception ex) {
            Console.Error.WriteLine(ex.Message);
            ShowHelp();
            throw new Exception("Fehler beim Ermitteln oder Anwenden der Programmoptionen.");
         }
      }

      /// <summary>
      /// erzeugt aus der Liste (Text) eine Indexliste
      /// </summary>
      /// <param name="arg"></param>
      /// <returns></returns>
      int[] GetIndexArray(string arg) {
         int[] idx;
         if (!string.IsNullOrEmpty(arg)) {
            string[] tmpa = arg.Split(new char[] { ',' });
            int[] tmpi = new int[tmpa.Length];
            for (int i = 0; i < tmpa.Length; i++)
               try {
                  tmpi[i] = Convert.ToInt32(tmpa[i]) - 1;         // 1... -> 0...
                  if (tmpi[i] < 0)
                     throw new Exception();
               } catch (Exception) {
                  throw new Exception(string.Format("Das Listenelement '{0}' ist keine positive Zahl.", tmpa[i]));
               }
            idx = new int[tmpi.Length];
            tmpi.CopyTo(idx, 0);
            Array.Sort(idx);
            return idx;
         } else
            return new int[0];
      }

      /// <summary>
      /// erzeugt aus der Liste (Text) eine int-Parameterliste
      /// </summary>
      /// <param name="arg"></param>
      /// <returns></returns>
      int[] GetParaArray(string arg) {
         if (!string.IsNullOrEmpty(arg)) {
            string[] tmpa = arg.Split(new char[] { ',' });
            int[] tmpi = new int[tmpa.Length];
            for (int i = 0; i < tmpa.Length; i++)
               try {
                  tmpi[i] = Convert.ToInt32(tmpa[i]);         // 1... -> 0...
               } catch (Exception) {
                  throw new Exception(string.Format("Das Listenelement '{0}' ist keine Zahl.", tmpa[i]));
               }
            return tmpi;
         } else
            return new int[0];
      }


      /// <summary>
      /// Hilfetext für Optionen ausgeben
      /// </summary>
      /// <param name="cmd"></param>
      public void ShowHelp() {
         List<string> help = cmd.GetHelpText();
         for (int i = 0; i < help.Count; i++)
            Console.Error.WriteLine(help[i]);
         Console.Error.WriteLine();
         Console.Error.WriteLine("Zusatzinfos:");


         Console.Error.WriteLine("Für '--' darf auch '/' stehen und für '=' Leerzeichen oder ':'.");

         // ...

         if (File.Exists("gpxtool.html"))
            CmdlineOptions.ShowHtmlfile("gpxtool.html");
      }

   }
}
