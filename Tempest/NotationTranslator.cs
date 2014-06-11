using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Tempest
{
    class NotationTranstalor
    {
        private Dictionary<string, int> generalKeyNumbers;   

        public NotationTranstalor()
        {
            Initialize();            
        }

        private void Initialize()
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

        private int GetKeyNumber(string key, int octaveNumber)
        {            
            int keyNumber;
            try
            {
                keyNumber = generalKeyNumbers[key];
            }
            catch
            {
                throw new ArgumentException("Wrong key name!");
            }           
            return 12 * octaveNumber + keyNumber;
        }

        public Note[] TranslateNotation(string notation)
        {
            //NOTATION FORMAT:
            //[TEMP] [NOTE1-DURATION] [NOTE2-DURATION] [NOTEN-DURATION]
            //TEMP: TEMP + BPM. Example: TEMP120 - tempo is 120 beats per minute
            //NOTE: Note name + octave number. Example: C4, A5 etc.
            //DURATION: Denominator of 1/2, 1/4 etc. Example: 2, 4, 8, 16
            //Example: TEMP98 A4-16 F5-16 E5-16 D5-8
            notation = notation.ToUpper();           
            string[] tokens = notation.Split(' ');
            string[] note = new string[2];           
            int errorCount = 0;
            int tempo;
            if (!int.TryParse(tokens[0].Replace("TEMP", ""), out tempo))
                throw new FormatException("Tempo was not specified correctly");
            double quarter = 60000d / tempo;
            double Ndur = 0;
            Note[] song = new Note[tokens.Length - 1];            
            if (tokens.Length < 2)
                return null;
            for (int i = 1; i < tokens.Length; i++)
            {
                note = tokens[i].Split('-');
                //If a note is empty, it is an error                       
                if (note[0] == string.Empty)
                {
                    errorCount++;
                    continue;
                }
                //Wrong note duration
                if (!double.TryParse(note[1], out Ndur))
                    errorCount++;              
                double duration = 4d / Ndur * quarter;
                //Is it a pause?
                if (note[0][0].ToString() == "P")                
                    song[i - 1] = new Note(0, (int)duration);               
                else
                {
                    string kn = note[0].Remove(note[0].Length - 1, 1);
                    int on = 0;
                    //Wrong octave number
                    if (!int.TryParse(note[0][note[0].Length - 1].ToString(), out on))
                    {
                        errorCount++;
                        continue;
                    }
                    double noteFreq = 16.352 * Math.Pow(2, GetKeyNumber(kn, on) / 12d);                   
                    song[i - 1] = new Note(noteFreq, (int)duration);
                }              
            }
            return song;
        }       

        public struct Note
        {
            double _frequncy;
            int _duration;

            public double Frequncy
            {
                get
                {
                    return _frequncy;
                }
            }
            public int Duration
            {
                get
                {
                    return _duration;
                }
            }

            public Note(double frequncy, int duration)
            {
                _frequncy = frequncy;
                _duration = duration;
            }
        }        
    }
}
