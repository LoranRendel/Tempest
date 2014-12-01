using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Tempest
{
    static partial class NotationTranstalor
    {
        private static Dictionary<string, string> variables;
        private static Dictionary<string, int> degreeSemitones;
        private static string[] steps = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        static NotationTranstalor()
        {
            Initialize();
        }

        private static void Initialize()
        {
            variables = new Dictionary<string, string>();
            degreeSemitones = new Dictionary<string, int>();
            degreeSemitones.Add("C", 0);
            degreeSemitones.Add("D", 2);
            degreeSemitones.Add("E", 4);
            degreeSemitones.Add("F", 5);
            degreeSemitones.Add("G", 7);
            degreeSemitones.Add("A", 9);
            degreeSemitones.Add("B", 11);
        }

        public static int GetSemitones(string note)
        {
            if (note.Length < 2 || note.Length > 3)
                return -1;
            Regex keyNameExtractor = new Regex(@"^(?i)" +
                                               @"(?<degreeName>[CDEFGAB])" +
                                               @"(?<modifier>[B#])?" +
                                               @"(?<octave>\d+)" +
                                               @"$");
            Match noteMatch = keyNameExtractor.Match(note);
            int semitones;
            int octave;
            int modifier = 0;
            if (noteMatch.Success)
            {
                semitones = degreeSemitones[noteMatch.Groups["degreeName"].Value.ToUpper()];
                octave = int.Parse(noteMatch.Groups["octave"].Value);
                if (noteMatch.Groups["modifier"].Success)
                    modifier = noteMatch.Groups["modifier"].Value == "#" ? 1 : -1;
                return octave * 12 + semitones + modifier;
            }
            else
                return -1;
        }

        private static string GetNote(ushort semitones, bool check)
        {
            ushort octave = (ushort)(semitones / 12);
            if (check && octave > 8)
                return string.Empty;
            string degreeName = steps[semitones % 12] + octave;
            return degreeName;
        }

        public static Note[] TranslateNotation(string notation)
        {
            if (notation == null)
                throw new ArgumentNullException();
            //NOTATION FORMAT:
            //TEMP NOTE1-DURATION^[NUMBER] NOTE2-DURATION NOTEN-DURATION NAME[contents] NAME
            //TEMP: TEMP + BPM. Example: TEMP120 - tempo is 120 beats per minute
            //NOTE: Note name + octave number. Example: C4, A5 etc.
            //DURATION[.[..]|T|xNumber]: Denominator of 1/2, 1/4 etc. Example: 2, 4, 8, 16., 4T, 8..
            //. means dotted note. You can add arbitrary number of dots not just one
            //T means triplet note
            //x[number] means that duration is multipled by the specified number. For example D4-2x2 = D4-1 (1/2*2 = 1)
            //Example: TEMP98 A4-16 F5-16 E5-16 D5-8 F6-8. G#5-4T B#4-4..
            //^[NUMBER] means that the note should be repeade NUMBER times.
            //Example: D5-8^3 = D5-8 D5-8 D5-8
            //NAME is a constant mention
            //NAME[CONTENTS] is a constant declaration. A constant must not contain another constant or a constant menthion, i. e. NAME[NAME2[CONTENTS]CONTENTS] or NAME[NAME CONTENTS] will result in an error.
            //Example: TEMP100 triplet[C4-4T^3] triplet = TEMP100 C4-4T C4-4T C4-4T C4-4T C4-4T C4-4T
            int semitones = 0;
            notation = notation.Trim(' ');
            Regex tempRegex = new Regex(@"(?i)^TEMP(?<tempo>\d+)(?<transposeMarker>T(?<semitones>-?\d+))?\s");
            Match rightTempo = tempRegex.Match(notation);
            if (rightTempo.Groups["transposeMarker"].Success)
                semitones = int.Parse(rightTempo.Groups["semitones"].Value);
            notation = Preprocess(notation);
            notation = Transpose(notation, semitones);
            notation = Regex.Replace(notation, "\\s+", " ");
            Regex noteExtractor = new Regex(@"(?i)(?<=\s)" +
                                            @"([CDEFGAB][B#]?\d+|P)" +
                                            @"-" +
                                            @"\d+(((T|\.+)|[X×]\d+))?" +
                                            @"(?=(\s|$))");
            MatchCollection correctNotes = noteExtractor.Matches(notation);
            notation = notation.Trim(' ');
            string[] tokens = notation.Split(' ');          
            if (correctNotes.Count != tokens.Length - 1)
                return null;
            int tempo = 0;
            if (!rightTempo.Success)
                throw new FormatException("Tempo was not specified correctly");
            else
                tempo = int.Parse(rightTempo.Groups["tempo"].Value);
            if (correctNotes.Count == 0)
                return null;
            Note[] song = new Note[correctNotes.Count];
            for (int i = 0; i < song.Length; i++)
                song[i] = ParseNote(correctNotes[i].Value, tempo);
            return song;
        }

        private static string Preprocess(string notation)
        {
            variables.Clear();
            Regex variableDeclaration = new Regex(@"(?<name>\w+)" +
                                                  @"(?<open>\[)" +
                                                  @"[^\[\]]+" +
                                                  @"(?<close-open>\])");
            MatchCollection variableMatches = variableDeclaration.Matches(notation);
            foreach (Match m in variableMatches)
                variables.Add(m.Groups["name"].Value, m.Groups["close"].Value);
            notation = variableDeclaration.Replace(notation, "${close}");

            Regex varMentions = new Regex(@"(?<=\s)" +
                                          @"\w+" +
                                          @"((?=\s)|$)");
            MatchCollection varMent = varMentions.Matches(notation);
            foreach (Match vm in varMent)
            {
                string varValue;
                if (variables.TryGetValue(vm.Value, out varValue))
                    notation = Regex.Replace(notation, string.Format(@"(?<=\s){0}((?=\s)|$)", vm.Value), varValue);
            }
            Regex complextNote = new Regex(@"(?i)((?<=\s)|^)" +
                                 @"(?<note>([CDEFGAB][B#]?\d+|P)" +
                                 @"-" +
                                 @"\d+(((T|\.+)|[X×]\d+))?)" +
                                 @"(?<repeat>\^(?<repeatcount>\d+))" +
                                 @"((?=\s)|$)");
            MatchCollection mc = complextNote.Matches(notation);
            foreach (Match m in mc)
            {
                string replace = string.Empty;
                for (int i = 0; i < int.Parse(m.Groups["repeatcount"].Value); i++)
                    replace += m.Groups["note"].Value + " ";
                notation = notation.Replace(m.Value, replace);
            }
            return notation;
        }

        public static string Transpose(string notation, int halftones)
        {
            if (halftones == 0)
                return notation;
            StringBuilder result = new StringBuilder(Regex.Match(notation, @"(?i)^TEMP(?<tempo>\d+)(?<transposeMarker>T(?<semitones>-?\d+))?\s").Value);
            Regex noteExtractor = new Regex(@"(?i)(?<=\s)" +
                                            @"(" +
                                               @"(?<note>[CDEFGAB][B#]?\d+)" +
                                               @"|" +
                                               @"P" +
                                            @")" +
                                            @"-" +
                                            @"(?<sp>\d+(((T|\.+)|[X×]\d+))?)" +
                                            @"((?=\s)|$)");
            MatchCollection notes = noteExtractor.Matches(notation);
            int semiTones;
            string transposedNote;
            foreach (Match note in notes)
                if (note.Groups["note"].Success)
                {
                    semiTones = GetSemitones(note.Groups["note"].Value) + halftones;
                    if (semiTones < 0)
                        return notation;
                    transposedNote = GetNote((ushort)semiTones, true);
                    if (transposedNote == string.Empty)
                        return notation;
                    result.AppendFormat(" {0}", transposedNote + "-" + note.Groups["sp"]);
                }
                else
                    result.AppendFormat(" {0}", note.Value);
            return result.ToString();
        }

        private static Note ParseNote(string noteText, int tempo)
        {
            double frequency = 0;
            double duration = 0;
            Regex re = new Regex(@"(?i)^((?<note>[CDEFGAB][B#]?\d+)|(?<pause>P))-(?<duration>\d+((T|\.+)|[X×]\d+)?)$");
            Match parsedNoteText = re.Match(noteText);
            if (parsedNoteText.Success)
            {
                if (parsedNoteText.Groups["note"].Success)
                    frequency = 16.352 * Math.Pow(2, GetSemitones(parsedNoteText.Groups["note"].Value) / 12d);
                duration = ParseDuration(parsedNoteText.Groups["duration"].Value, 60000d / tempo * 4);
            }
            return new Note(frequency, duration);
        }

        private static double ParseDuration(string durationText, double whole)
        {
            double result = 0;
            Regex durationRegex = new Regex(@"(?i)^(?<denominator>\d+)(?<modifier>(?<simpleModifier>([tT]|[.]+))|(?<multiply>[X×](?<factor>\d+)))?$");
            Match parsedDuration = durationRegex.Match(durationText);
            if (parsedDuration.Success)
            {
                result = whole / double.Parse(parsedDuration.Groups["denominator"].Value);
                if (parsedDuration.Groups["modifier"].Success)
                {
                    if (parsedDuration.Groups["multiply"].Success)
                        result *= double.Parse(parsedDuration.Groups["factor"].Value);

                    if (parsedDuration.Groups["simpleModifier"].Success)
                        switch (parsedDuration.Groups["simpleModifier"].Value[0].ToString().ToLower())
                        {
                            case ".":
                                result += result * (1 - (1 / Math.Pow(2, parsedDuration.Groups["simpleModifier"].Value.Length)));
                                break;
                            case "t":
                                double q = 2 / double.Parse(parsedDuration.Groups["denominator"].Value) / 3;
                                result = whole * q;
                                break;
                        }
                }
            }
            return result;
        }
    }
}