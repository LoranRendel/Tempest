using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Tempest
{
    static partial class NotationTranstalor
    {
        private static Dictionary<string, int> generalKeyNumbers;

        static NotationTranstalor()
        {
            Initialize();
        }

        private static void Initialize()
        {
            generalKeyNumbers = new Dictionary<string, int>();
            generalKeyNumbers.Add("C", 0);
            generalKeyNumbers.Add("C#", 1);
            generalKeyNumbers.Add("DB", 1);
            generalKeyNumbers.Add("D", 2);
            generalKeyNumbers.Add("D#", 3);
            generalKeyNumbers.Add("EB", 3);
            generalKeyNumbers.Add("E", 4);
            generalKeyNumbers.Add("F", 5);
            generalKeyNumbers.Add("F#", 6);
            generalKeyNumbers.Add("GB", 6);
            generalKeyNumbers.Add("G", 7);
            generalKeyNumbers.Add("G#", 8);
            generalKeyNumbers.Add("AB", 8);
            generalKeyNumbers.Add("A", 9);
            generalKeyNumbers.Add("A#", 10);
            generalKeyNumbers.Add("BB", 10);
            generalKeyNumbers.Add("B", 11);
        }

        private static int GetKeyNumber(string key, int octaveNumber)
        {
            int keyNumber;
            try
            {
                keyNumber = generalKeyNumbers[key.ToUpper()];
            }
            catch
            {
                throw new ArgumentException("Wrong key name!");
            }
            return 12 * octaveNumber + keyNumber;
        }

        public static Note[] TranslateNotation(string notation)
        {
            if (notation == null)
                throw new ArgumentNullException();
            //NOTATION FORMAT:
            //[TEMP] [NOTE1-DURATION] [NOTE2-DURATION] [NOTEN-DURATION]
            //TEMP: TEMP + BPM. Example: TEMP120 - tempo is 120 beats per minute
            //NOTE: Note name + octave number. Example: C4, A5 etc.
            //DURATION[.T|xNumber]: Denominator of 1/2, 1/4 etc. Example: 2, 4, 8, 16.
            //Example: TEMP98 A4-16 F5-16 E5-16 D5-8
            notation = notation.ToUpper();
            notation = Regex.Replace(notation, "\\s+", " ");
            notation = notation.Trim(' ');
            Regex cnRegEx = new Regex(@"(?<=\s)"+
                                 @"([CDEFGABcdefgab][Bb#]?\d+|[Pp])"+
                                 @"-"+
                                 @"\d+((([tT]|[.]+)|[xX×]\d+))?"+
                                 @"((?=\s)|$)");
            Regex tempRegex = new Regex(@"^TEMP(?<tempo>\d+)\s");
            Match rightTempo = tempRegex.Match(notation);
            MatchCollection correctNotes = cnRegEx.Matches(notation);
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

        private static Note ParseNote(string noteText, int tempo)
        {
            double frequency = 0;
            double duration = 0;
            Regex re = new Regex(@"((?<noteName>[CDEFGABcdefgab][Bb#]?)(?<octave>\d+)|(?<pause>[Pp]))-(?<duration>\d+(([tT]|[.]+)|[xX×]\d+)?)$");
            Match parsedNoteText = re.Match(noteText);
            if (parsedNoteText.Success)
            {
                if (!parsedNoteText.Groups["pause"].Success)
                    frequency = 16.352 * Math.Pow(2, GetKeyNumber(parsedNoteText.Groups["noteName"].Value, int.Parse(parsedNoteText.Groups["octave"].Value)) / 12d);
                duration = ReadDuration(parsedNoteText.Groups["duration"].Value, 60000d / tempo * 4);
            }
            return new Note(frequency, duration);
        }

        private static double ReadDuration(string durationText, double whole)
        {
            double result = 0;
            Regex durationRegex = new Regex(@"(?<denominator>\d+)(?<modifier>(?<simpleModifier>([tT]|[.]+))|(?<multiply>[xX×](?<factor>\d+)))?");
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
                                result += result * (1-(1/Math.Pow(2, parsedDuration.Groups["simpleModifier"].Value.Length)));
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

        private static void RemoveMultipleSpaces(ref string text)
        {
            if (text == null)
                return;
            string result = string.Empty;
            bool prevIsSpace = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (prevIsSpace == true & text[i] == ' ')
                    continue;
                result += text[i];
                if (text[i] == ' ')
                    prevIsSpace = true;
                else
                    prevIsSpace = false;
            }
            text = result;
        }

        private static bool CheckBrackets(string[] tokens)
        {
            bool result = false;
            int balance = 0;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "(")
                {
                    if (balance < 0)
                        return false;
                    else
                        balance++;
                }
                if (tokens[i] == ")")
                {
                    balance--;
                }
            }
            if (balance == 0)
                result = true;
            return result;
        }
    }
}
