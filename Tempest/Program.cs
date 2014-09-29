using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Media;
using System.Threading;
using WaveGenerator;

namespace Tempest
{

    partial class Program
    {
        static ConsoleColor defaultForeground = Console.ForegroundColor;
        static ConsoleColor defaultBackground = Console.BackgroundColor;
        static bool saveToFile = false;
        static string mainPromptText = "> ";
        static string subPromptText = ">>> ";
        static int promptLeft = 4;
        static int notificationLeft = 6;
        static ushort sampleRate = 16000;
        static string windowTitle = "Tempest";
        static bool playing = false;
        static bool playPrompt = false;
        static bool running = true;
        static bool systemBeeper = false;
        static bool simple = false;        
        static char[] playingIndicatorFrames = new char[] { '|', '/', '-', '\\' };
        static string defaultPlaylistPath = "songs.txt";
        static NotationTranstalor.Song[] pieces = null;
        static FileInfo playListFile = null;
        static bool cancelPressed = false;

        static int Main()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            ShowWelcomeScreen();
            OpenPlayList(defaultPlaylistPath);
            PrintPlayList();
            while (running)
                PlayerPrompt();
            return 0;
        }

        static void ShowWelcomeScreen()
        {

            Console.Title = windowTitle;
            string welcomeText = string.Format("Подобие музыкального проигрывателя v {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            int screenCenter = Console.WindowWidth / 2;
            Console.CursorLeft = screenCenter - (welcomeText.Length / 2);
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(welcomeText);
            Console.ForegroundColor = defaultForeground;
            Console.BackgroundColor = defaultBackground;
            Console.Write("\n\n");
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
        private static bool OpenPlayList(string path)
        {
            bool result = false;
            string answer = path;
            if (path == null)
            {
                PrintNotification("Укажите путь к плейлисту:", promptLeft);
                Console.CursorLeft = promptLeft;
                Console.Write(subPromptText);
                answer = Console.ReadLine();
            }
            FileInfo playlistFile = null;
            try
            {
                playlistFile = new FileInfo(answer);
            }
            catch (ArgumentException e)
            {
                return result;
            }
            if (playlistFile != null && playlistFile.Exists)
            {
                Program.playListFile = playlistFile;
                using (StreamReader reader = new StreamReader(Program.playListFile.FullName))
                {
                    pieces = ReadPlayList(reader.ReadToEnd());
                }
                result = true;
            }
            return result;
        }

        static NotationTranstalor.Song[] ReadPlayList(string fileText)
        {
            if (fileText == null)
                throw new ArgumentNullException();
            string[] splitted = fileText.Split('\n');
            List<NotationTranstalor.Song> pieces = new List<NotationTranstalor.Song>();
            // NotationTranstalor.Song[] pieces = new NotationTranstalor.Song[splitted.Length];
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
                        try
                        {
                            NotationTranstalor.Song s = new NotationTranstalor.Song(name, string.Empty, text);
                            pieces.Add(s);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            return pieces.ToArray();
        }        

        static void PrintPlayList()
        {
            if (pieces == null)
            {
                Console.CursorLeft = notificationLeft;
                PrintNotification("Плейлист отсутствует или имеет неправильный формат", notificationLeft);
                return;
            }
            Console.CursorLeft = 4;
            Console.WriteLine("Для прослушивания доступны следующие мелодии:\n");
            for (int i = 0; i < pieces.Length; i++)
            {
                int lm = pieces[i].Length;
                TimeSpan length = new TimeSpan(0, 0, 0, lm / 1000, lm - lm / 1000 * 1000);
                Console.CursorLeft = 6;
                Console.WriteLine("{0}. {1} {2}{3}", i + 1, pieces[i].Name, string.Format("({0:d2}:{1:d2})", length.Minutes, length.Seconds), (i != pieces.Length - 1) ? ";" : ".\n");
            }
        }

        static void PlayerPrompt()
        {
            string answer = string.Empty;
            int pieceNumber = 0;
            Console.CursorLeft = promptLeft;
            Console.Write(mainPromptText);
            answer = Console.ReadLine();
            if (int.TryParse(answer, out pieceNumber) && pieces != null && pieceNumber <= pieces.Length && pieceNumber > 0)
            {
                PlayPiece(pieces[pieceNumber - 1], Program.systemBeeper);
            }
            else
            {
                switch (answer)
                {
                    case "s":
                    case "sysb":
                        systemBeeper = true;
                        PrintNotification("Теперь звуки будут воспроизводиться через системный бипер", notificationLeft);
                        break;
                    case "w":
                    case "wav":
                        systemBeeper = false;
                        PrintNotification("Теперь звуки будут воспроизводиться при помощи wav-файла", notificationLeft);
                        break;
                    case "o":
                    case "open":
                        if (OpenPlayList(null))
                            PrintPlayList();
                        break;
                    case "reload":
                        if (Program.pieces == null)
                            PrintNotification("Плейлист не был загружен, поэтому его невозможно перезагрузить", notificationLeft);
                        else
                        {
                            NotationTranstalor.Song[] temp = pieces;
                            bool result = OpenPlayList(Program.playListFile.FullName);
                            if (pieces != null && result)
                            {
                                PrintNotification("Плейлист перезагружен", notificationLeft);
                                PrintPlayList();
                            }
                            else
                            {
                                PrintNotification("Не удалось перезагрузить плейлист, так как файл содержит ошибки или не существует.", notificationLeft);
                                pieces = temp;
                            }
                        }
                        break;
                    case "sp":
                    case "show playlist":
                        PrintPlayList();
                        break;
                    case "play":
                        playPrompt = true;
                        while (playPrompt)
                            PlayCommandPrompt();
                        break;
                    case "save":
                        saveToFile = true;
                        PrintNotification("Результаты генерации будут сохраняться в файл", notificationLeft);
                        break;
                    case "dnsave":
                        saveToFile = false;
                        PrintNotification("Сохранение результатов в файл отключено", notificationLeft);
                        break;
                    case "simple":
                        simple = !simple;
                        PrintNotification(simple.ToString(), notificationLeft);
                        break;
                    case "help":
                    case "h":
                        Console.WriteLine(new string(' ', notificationLeft)+manual.Replace("\r\n", "\r\n"+new string(' ', notificationLeft)));
                        break;
                    case "q":
                    case "exit":
                    case "quit":
                        running = false;
                        return;
                }
            }
        }

        static void PlayCommandPrompt()
        {
            Console.CursorLeft = promptLeft;
            Console.Write(subPromptText);
            string[] possibleCommands = { "q", "qq" };
            string answer = Console.ReadLine();
            if (Array.IndexOf(possibleCommands, answer) != -1)
            {
                switch (answer.ToLower())
                {
                    case "q":
                        playPrompt = false;
                        break;
                    case "qq":
                        playPrompt = false;
                        running = false;
                        break;
                }
            }
            else
            {
                try
                {
                    NotationTranstalor.Song enteredSong = new NotationTranstalor.Song("Untitled", string.Empty, answer);
                    PlayPiece(enteredSong, Program.systemBeeper);
                }
                catch
                {
                    PrintNotification("Не удалось воспроизвести мелодию из-за ошибки в записи.", notificationLeft);
                }
            }
        }

        static void PlayPiece(NotationTranstalor.Song piece, bool systemBeeper)
        {            
            playing = true;            
            Thread playThread = new Thread(new ParameterizedThreadStart(StartPlay));
            playThread.Start(piece);
            Console.CursorLeft = promptLeft;
            Console.Write(piece.Name);
            while (playing)
            {
                for (int i = 0; i < playingIndicatorFrames.Length; i++)
                {
                    if (playing == false)
                        break;
                    Console.CursorLeft = promptLeft + piece.Name.Length;
                    Console.Write(" " + playingIndicatorFrames[i].ToString());
                    Thread.Sleep(50);
                }
            }
            Console.CursorLeft = promptLeft + piece.Name.Length;
            Console.WriteLine("  ");
            cancelPressed = false;
        }

        /// <summary>
        /// Starts the player in a separate thread and signals about its termination unsetting Program.playing
        /// </summary>
        /// <param name="parameters">Song to play</param>
        static void StartPlay(object parameters)
        {
            if (parameters == null)
            {
                playing = false;
                return;
            }
            NotationTranstalor.Song song = (NotationTranstalor.Song)parameters;       
            using (StreamWriter m = new StreamWriter("melody.txt"))
            {
                foreach (NotationTranstalor.Note n in song.Notes)
                    m.WriteLine("{0} {1}", (int)n.Frequncy, (int)n.Duration);
                m.Close();
            }
            if (systemBeeper)
            {
                foreach (NotationTranstalor.Note note in song.Notes)
                {
                    if (cancelPressed == true)
                        break;
                    if (note.Frequncy > 37)
                        Console.Beep((int)note.Frequncy, (int)note.Duration);
                    else
                        System.Threading.Thread.Sleep((int)note.Duration);
                }
            }
            else
            {
                MemoryStream audioFileStream = new MemoryStream();
                WaveFile wavFile = new WaveFile(Program.sampleRate, BitDepth.Bit16, 1, audioFileStream);
                SoundGenerator sg = new SoundGenerator(wavFile);
               
                double[] startPhase = new double[] { 0, 0, 0 };
                for (int i = 0; i <  song.Notes.Length; i++)
                {
                    if (song.Notes[i].Frequncy == 0)
                        startPhase = new double[] { 0, 0, 0 };
                    if (simple)
                        startPhase[0] = sg.AddSimpleTone(song.Notes[i].Frequncy, song.Notes[i].Duration, startPhase[0], 1, true);
                    else
                        startPhase = sg.AddComplexTone(song.Notes[i].Duration, startPhase, 1, true, song.Notes[i].Frequncy, song.Notes[i].Frequncy * 2, song.Notes[i].Frequncy * 3);
                }
                sg.Save();
                if (saveToFile)
                {
                    string fileHash = string.Empty;
                    using (var cp = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                    {
                        fileHash = BitConverter.ToString(cp.ComputeHash(audioFileStream.ToArray()), 14);
                    }
                    audioFileStream.Position = 0;
                    string fileName = string.Format("generated_{0}_{1}_{2}.wav", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString().Replace(':', '-'), fileHash);
                    FileStream file = new FileStream(fileName, FileMode.Create);
                    audioFileStream.WriteTo(file);
                    file.Close();
                }
                audioFileStream.Position = 0;
                SoundPlayer player = new SoundPlayer(audioFileStream);
                player.Play();
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                int sl = song.Length + 20;
                while (sl > sw.ElapsedMilliseconds)
                {
                    if (cancelPressed)
                    {
                        player.Stop();
                        sw.Stop();
                        break;
                    }
                }
                audioFileStream.Close();
                audioFileStream.Dispose();
            }
            playing = false;
        }

        static void PrintNotification(string text, int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.WriteLine(text);
        }         

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancelPressed = true;
        }
    }
}