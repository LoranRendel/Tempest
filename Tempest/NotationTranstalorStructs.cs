using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tempest
{
    static partial class NotationTranstalor
    {
        public struct Note
        {
            double _frequncy;
            double _duration;

            public double Frequncy
            {
                get
                {
                    return _frequncy;
                }
            }
            public double Duration
            {
                get
                {
                    return _duration;
                }
            }

            public Note(double frequncy, double duration)
            {
                _frequncy = frequncy;
                _duration = duration;
            }
        }

        public struct Song
        {
            private string _name;
            private string _author;
            private string _text;
            private Note[] _notes;
            private int _msLength;

            public string Name { get { return _name; } }
            public string Author { get { return _author; } }
            public string Text { get { return _text; } }
            public Note[] Notes { get { return _notes; } }
            public int Length { get { return _msLength; } }

            public Song(string name, string author, string text)
                : this()
            {
                this._name = name;
                this._author = author;
                this._text = text;
                this._notes = NotationTranstalor.TranslateNotation(text);
                _msLength = GetSongLength(_notes);
            }

            private int GetSongLength(NotationTranstalor.Note[] notes)
            {
                double result = 0;
                foreach (NotationTranstalor.Note n in notes)
                    result += n.Duration;
                return (int)result;
            }
        }
    }
}
