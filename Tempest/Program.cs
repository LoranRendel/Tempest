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
        static string windowTitle = "Tempest";
        static int plFirstRowTop = 0;
        static bool playing = false;
        static bool running = true;
        static bool systemBeeper = false;
        static char[] playingIndicatorFrames = new char[] { '|', '/', '-', '\\' };
        static string defaultPlaylistPath = "songs.txt";
        static Song[] pieces = null;
        static FileInfo playListFile = null;
        static SoundPlayer currPlayer = null;

        [STAThread]
        static int Main()
        {
            ShowWelcomeScreen();
            OpenPlayList(defaultPlaylistPath);
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
        /// [3] the playlist was loaded and user pressed OK button.
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
            if (loaded == false && okayPressed == true)
                result = 2;
            if (loaded == true && okayPressed == true)
                result = 3;
            if (loaded)
                Console.Title = playListFile.FullName;
            else
                Console.Title = windowTitle;
            return result;
        }

        static void ShowWelcomeScreen()
        {
            Console.Title = windowTitle;
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
            plFirstRowTop = Console.CursorTop;
            for (int i = 0; i < pieces.Length; i++)
            {
                int lm = GetSongLength(pieces[i]);
                TimeSpan length = new TimeSpan(0, 0, 0, lm / 1000, lm - lm / 1000 * 1000);
                Console.CursorLeft = 6;
                if (i != pieces.Length - 1)
                    Console.WriteLine("{0}. {1} {2};", i + 1, pieces[i].name, string.Format("({0:d2}:{1:d2})", length.Minutes, length.Seconds));
                else
                    Console.WriteLine("{0}. {1} {2}.\n", i + 1, pieces[i].name, string.Format("({0:d2}:{1:d2})", length.Minutes, length.Seconds));
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

                PlayPiece(pieces[pieceNumber - 1], Program.systemBeeper, left + promptText.Length + answer.Length + 1, pieceNumber);
            }
            else
            {
                switch (answer)
                {
                    case "s":
                    case "sysb":
                        systemBeeper = true;
                        PrintNotification("Теперь звуки будут воспроизводиться через системный бипер", leftForNotifications);
                        break;
                    case "w":
                    case "wav":
                        systemBeeper = false;
                        PrintNotification("Теперь звуки будут воспроизводиться при помощи wav-файла", leftForNotifications);
                        break;
                    case "o":
                    case "open":
                        byte result = OpenPlayList(null);
                        if (result == 3 || result == 2)
                            PrintPlayList();
                        break;
                    case "reload":
                        if (Program.pieces == null)
                            PrintNotification("Плейлист не был загружен, поэтому его невозможно перезагрузить", leftForNotifications);
                        else
                        {
                            OpenPlayList(Program.playListFile.FullName);
                            PrintNotification("Плейлист перезагружен", leftForNotifications);
                            PrintPlayList();
                        }
                        break;
                    case "play":
                        Console.CursorLeft = left;
                        promptText = ">>> ";
                        Console.Write(promptText);
                        int cl = Console.CursorLeft;
                        Song enteredSong = new Song() { text = Console.ReadLine() };
                        PlayPiece(enteredSong, Program.systemBeeper, left + promptText.Length + enteredSong.text.Length + 1, pieceNumber);
                        break;
                    case "q":
                    case "exit":
                    case "quit":
                        running = false;
                        return;
                }
            }

        }

        static void PlayPiece(Song piece, bool systemBeeper, int indicatorLeft, int songNumber)
        {
            NotationTranstalor.Note[] notes = null;
            try
            {
                notes = NotationTranstalor.TranslateNotation(piece.text);
            }
            catch
            {
                PrintNotification("Не удалось воспроизвести мелодию из-за ошибки в записи", 6);
                return;
            }
            playing = true;
            Thread playThread = new Thread(new ParameterizedThreadStart(StartPlay));
            playThread.Start(notes);
            int top = Console.CursorTop;
            //Highlight a song that is being played
            if (songNumber != -1)
                HighLightSong(piece, songNumber, ConsoleColor.Magenta);
            while (playing)
            {
                for (int i = 0; i < playingIndicatorFrames.Length; i++)
                {
                    if (playing == false)
                        break;
                    Console.CursorTop = top - 1;
                    Console.CursorLeft = indicatorLeft;
                    Console.WriteLine(playingIndicatorFrames[i].ToString());
                    Thread.Sleep(50);

                }
            }
            if (songNumber != -1)
            HighLightSong(piece, songNumber, ConsoleColor.Gray);
            Console.CursorTop = top - 1;
            Console.CursorLeft = indicatorLeft;
            Console.WriteLine(" ");
        }

        private static void HighLightSong(Song piece, int songNumber, ConsoleColor color)
        {
            Console.CursorTop = plFirstRowTop - 1 + songNumber;
            Console.CursorLeft = (songNumber.ToString() + ". ").Length + 6;
            Console.ForegroundColor = color;
            Console.WriteLine(piece.name);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        /// <summary>
        /// Starts the player in a separate thread and signals about its termination unsetting Program.playing
        /// </summary>
        /// <param name="piece">Song to play</param>
        static void StartPlay(object piece)
        {
            if (piece == null)
            {
                playing = false;
                return;
            }
            NotationTranstalor.Note[] notes = (NotationTranstalor.Note[])piece;
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
                //currPlayer = player;            
                player.PlaySync();
                audioFileStream.Close();
            }
            playing = false;
        }

        static void PrintNotification(string text, int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.WriteLine(text);
        }

        static int GetSongLength(Song s)
        {
            int result = 0;
            NotationTranstalor.Note[] notes = null;
            try
            {
                notes = NotationTranstalor.TranslateNotation(s.text);
            }
            catch
            {
                return 0;
            }
            foreach (NotationTranstalor.Note n in notes)
                result += n.Duration;
            return result;
        }

        static Song[] ReadPlayList(string fileText)
        {
            if (fileText == null)
                throw new ArgumentNullException();
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