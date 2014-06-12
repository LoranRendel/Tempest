using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Media;
using System.Threading;

namespace Tempest
{

    class Program
    {
        static string defaultPlayListPath = "songs.txt";
        static bool running = true;
        static bool systemBeeper = false;
        static Song[] pieces;
        static FileInfo playListFile = null;

        [STAThread]
        static int Main()
        {            
            ShowWelcomeScreen();
            OpenPlayList(defaultPlayListPath);           
            PrintPlayList();            
            while (running)
            {
                PlayerPrompt();
            }
            return 0;
        }
        /// <summary>
        /// Opens and loads a playlist
        /// </summary>
        /// <param name="path">A path to a playlist file, if it's null, an OpenFileDialogue is shown.</param>
        /// <returns>Result of operation: 
        /// [0] the playlist wasn't loaded and user didn't press OK button; 
        /// [1] the playlist was loaded and user didn't press OK button;
        /// [2] the playlist wasn't loaded and user pressed OK button;
        /// [3] the playlist was loaded and user pressed OK buttonon.
        /// </returns>
        private static byte OpenPlayList(string path)
        {
            StreamReader reader = null;
            bool okayPressed = false;
            bool loaded = false;
            if (path == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Выберите файл со списком мелодий";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    playListFile = new FileInfo(ofd.FileName);
                    reader = playListFile.OpenText();
                    pieces = ReadPlayList(reader.ReadToEnd());
                    reader.Close();
                    okayPressed = true;
                }
            }
            else
            {
                playListFile = new FileInfo(path);
                if (playListFile.Exists)
                {
                    reader = playListFile.OpenText();
                    pieces = ReadPlayList(reader.ReadToEnd());
                    reader.Close();
                }
                else
                    return OpenPlayList(null);
            }
            loaded = pieces != null;
            byte result = 0;
            if (loaded == false && okayPressed == false)
                result = 0;
            if (loaded == true && okayPressed == false)
                result = 1;
            if(loaded == false && okayPressed == true)
                result = 2;
            if(loaded == true && okayPressed == true)
                result = 3;           
            return result;
        }        

        static void ShowWelcomeScreen()
        {
            string welcomeText = " Подобие музыкального проигрывателя v 0.0004";
            int screenCenter = Console.WindowWidth / 2;
            Console.CursorLeft = screenCenter - (welcomeText.Length / 2);
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(welcomeText);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("\n\n");
        }

        static void PrintPlayList()
        {
            if (pieces == null)
            {
                Console.CursorLeft = 6;
                Console.WriteLine("Плейлист отсутствует или имеет неправильный формат");
                return;
            }
            Console.CursorLeft = 4;
            Console.WriteLine("Для прослушивания доступны следующие мелодии:\n");
            for (int i = 0; i < pieces.Length; i++)
            {
                int lm = GetSongLength(pieces[i]);
                TimeSpan length = new TimeSpan(0, 0, 0, lm / 1000, lm - lm / 1000 * 1000);
                Console.CursorLeft = 6;
                if (i != pieces.Length - 1)
                    Console.WriteLine("{0}. {1} {2};", i + 1, pieces[i].name, string.Format("({0}:{1})", length.Minutes, length.Seconds));
                else
                    Console.WriteLine("{0}. {1} {2}.\n", i + 1, pieces[i].name, string.Format("({0}:{1})", length.Minutes, length.Seconds));
            }
        }

        static void PlayerPrompt()
        {
            string answer = string.Empty;

            string promptText = "Проиграть мелодию: ";
            int left = 4;
            int leftForNotifications = 6;

            int pieceNumber = 0;
            Console.CursorLeft = left;
            Console.Write(promptText);
            answer = Console.ReadLine();
            if (int.TryParse(answer, out pieceNumber) && pieces != null && pieceNumber <= pieces.Length && pieceNumber > 0)
            {
                Thread indicator = PrintIndicatorAsync(left + promptText.Length + answer.Length + 1, 100);
                PlayPiece(pieces[pieceNumber - 1], Program.systemBeeper);
                indicator.Abort();
                //interface bug
                while (indicator.IsAlive) ;
            }
            else
            {
                Console.CursorLeft = leftForNotifications;
                switch (answer)
                {
                    case "s":
                    case "sysb":
                        systemBeeper = true;
                        Console.WriteLine("Теперь звуки будут воспроизводиться через системный бипер");
                        break;
                    case "w":
                    case "wav":
                        systemBeeper = false;
                        Console.WriteLine("Теперь звуки будут воспроизводиться при помощи wav-файла");
                        break;
                    case "o":
                    case "open":
                        byte result = OpenPlayList(null);
                        if (result == 3 || result == 2)
                            PrintPlayList();
                        break;
                    case "reload":
                        if (Program.pieces == null)
                            Console.WriteLine("Плейлист не был загружен, поэтому его невозможно перезагрузить");
                        else
                        {
                            OpenPlayList(Program.playListFile.FullName);
                            Console.WriteLine("Плейлист перезагружен");
                            PrintPlayList();
                        }
                        break;
                    case "play":
                        Console.Write(">>> ");
                        Song enteredSong = new Song() { text = Console.ReadLine() };
                        try
                        {
                            PlayPiece(enteredSong, Program.systemBeeper);
                        }
                        catch
                        {
                            Console.CursorLeft = 4;
                            Console.WriteLine("Не удалось воспроизвести мелодию из-за ошибки в записи");
                        }
                        break;
                    case "q":
                    case "exit":
                    case "quit":
                        running = false;
                        return;
                }
            }

        }

        static Thread PrintIndicatorAsync(int left, int frameGap)
        {
            Thread indicatorThread = new Thread(new ParameterizedThreadStart(PrintWorkingIndicator));
            int[] parametrs = new int[] { left, frameGap };
            indicatorThread.Start(parametrs);
            return indicatorThread;
        }
        static void PrintWorkingIndicator(object parameters)
        {
            bool working = true;
            int[] pp = (int[])parameters;
            int top = Console.CursorTop - 1;
            int left = pp[0];
            int frameGap = pp[1];
            char[] frames = new char[] { '|', '/', '-', '\\' };
            while (working)
            {
                try
                {
                    for (int i = 0; i < frames.Length; i++)
                    {

                        Console.CursorLeft = left;
                        Console.CursorTop = top;
                        Console.Write(frames[i].ToString());
                        Thread.Sleep(frameGap);
                    }
                }
                catch
                {
                    Console.CursorLeft = left;
                    Console.CursorTop = top;
                    Console.WriteLine(" ");
                    working = false;
                    continue;
                }
            }
        }

        static void PlayPiece(Song piece, bool systemBeeper)
        {
            //Console.CursorVisible = false;
            NotationTranstalor.Note[] notes = NotationTranstalor.TranslateNotation(piece.text);
            if (systemBeeper)
            {
                foreach (NotationTranstalor.Note note in notes)
                {
                    if (note.Frequncy > 37)
                        Console.Beep((int)note.Frequncy, note.Duration);
                    else
                        System.Threading.Thread.Sleep(note.Duration);
                }
            }
            else
            {
                Wave audioFileGenerator = new Wave(22050);
                foreach (NotationTranstalor.Note note in notes)
                {
                    audioFileGenerator.addWave((int)note.Frequncy, note.Duration);
                }
                MemoryStream audioFileStream = new MemoryStream();
                audioFileGenerator.saveFile(audioFileStream);
                audioFileStream.Position = 0;
                SoundPlayer player = new SoundPlayer(audioFileStream);
                player.PlaySync();
                audioFileStream.Close();
            }
            Console.CursorVisible = true;
        }

        static int GetSongLength(Song s)
        {
            int result = 0;
            NotationTranstalor.Note[] notes = NotationTranstalor.TranslateNotation(s.text);
            foreach (NotationTranstalor.Note n in notes)
                result += n.Duration;
            return result;
        }

        static Song[] ReadPlayList(string fileText)
        {
            string[] splitted = fileText.Split('\n');
            Song[] pieces = new Song[splitted.Length];
            for (int i = 0; i < splitted.Length; i++)
            {
                //Is it a proper song?
                splitted[i] = splitted[i].Replace('\r', '\0');
                if (splitted[i].Contains("(") && splitted[i].Contains(")"))
                {
                    int open = 0, close = 0, openCount = 0, closeCount = 0;
                    for (int j = 0; j < splitted[i].Length; j++)
                    {
                        if (splitted[i][j] == '(')
                        {
                            openCount++;
                            open = j;
                        }
                        if (splitted[i][j] == ')')
                        {
                            closeCount++;
                            close = j;
                        }
                    }
                    string text, name;
                    if (open < close && openCount == 1 && closeCount == 1)
                    {
                        text = splitted[i].Substring(open + 1, close - open - 1);
                        name = splitted[i].Substring(0, open);
                        pieces[i].text = text;
                        pieces[i].name = name;
                    }
                    else
                        return null;

                }
                else
                    return null;

            }
            return pieces;
        }

        struct Song
        {
            public string name;
            public string author;
            public string text;
        }
    }
}