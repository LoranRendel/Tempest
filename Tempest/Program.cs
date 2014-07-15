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

    class Program
    {
        static ConsoleColor defaultForeground = Console.ForegroundColor;
        static ConsoleColor defaultBackground = Console.BackgroundColor;
        static bool saveToFile = false;
        static string mainPromptText = "> ";
        static string subPromptText = ">>> ";
        static int promptLeft = 4;
        static int notificationLeft = 6;   
        static string windowTitle = "Tempest";    
        static bool playing = false;
        static bool running = true;
        static bool systemBeeper = false;
        static char[] playingIndicatorFrames = new char[] { '|', '/', '-', '\\' };
        static string defaultPlaylistPath = "songs.txt";
        static NotationTranstalor.Song[] pieces = null;
        static FileInfo playListFile = null;
        static bool cancelPressed = false;      
      
        [STAThread]
        static int Main()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            ShowWelcomeScreen();
            OpenPlayList(defaultPlaylistPath);
            PrintPlayList();
            while (running)
            {
                GC.Collect();       
                PlayerPrompt();
            }
            return 0;
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancelPressed = true;
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
            Console.ForegroundColor = defaultForeground;
            Console.BackgroundColor = defaultBackground;
            Console.Write("\n\n");
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
                int lm = GetSongLength(pieces[i]);
                TimeSpan length = new TimeSpan(0, 0, 0, lm / 1000, lm - lm / 1000 * 1000);
                Console.CursorLeft = 6;
                if (i != pieces.Length - 1)
                    Console.WriteLine("{0}. {1} {2};", i + 1, pieces[i].Name, string.Format("({0:d2}:{1:d2})", length.Minutes, length.Seconds));
                else
                    Console.WriteLine("{0}. {1} {2}.\n", i + 1, pieces[i].Name, string.Format("({0:d2}:{1:d2})", length.Minutes, length.Seconds));
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
                        byte result = OpenPlayList(null);
                        if (result == 3 || result == 2)
                        {
                            PrintPlayList();                           
                        }
                        break;
                    case "reload":
                        if (Program.pieces == null)
                            PrintNotification("Плейлист не был загружен, поэтому его невозможно перезагрузить", notificationLeft);
                        else
                        {
                            OpenPlayList(Program.playListFile.FullName);
                            PrintNotification("Плейлист перезагружен", notificationLeft);
                            PrintPlayList();                           
                        }
                        break;
                    case "sp":
                    case "show playlist":
                        PrintPlayList();
                        break;
                    case "play":
                        Console.CursorLeft = promptLeft;                       
                        Console.Write(subPromptText);
                        int cl = Console.CursorLeft;
                        NotationTranstalor.Song enteredSong = new NotationTranstalor.Song("Untitled", string.Empty, Console.ReadLine()) ;                       
                        PlayPiece(enteredSong, Program.systemBeeper);
                        break;
                    case "save":
                        saveToFile = true;                       
                        PrintNotification("Результаты генерации будут сохраняться в файл", notificationLeft);
                        break;
                    case "dnsave":
                        saveToFile = false;           
                        PrintNotification("Сохранение результатов в файл отключено", notificationLeft);
                        break;
                    case "q":
                    case "exit":
                    case "quit":
                        running = false;
                        return;
                }                 
            }
        }

        static void PlayPiece(NotationTranstalor.Song piece, bool systemBeeper)
        {         
            NotationTranstalor.Note[] notes = null;
            try
            {
                notes = NotationTranstalor.TranslateNotation(piece.Text);
            }
            catch
            {
                PrintNotification("Не удалось воспроизвести мелодию из-за ошибки в записи", 6);
                return;
            }
            playing = true;
            Thread playThread = new Thread(new ParameterizedThreadStart(StartPlay));
            object[] songWithName = new object[] { piece.Name, notes, GetSongLength(piece)};

            playThread.Start(songWithName);        
            Console.CursorLeft = promptLeft;
            Console.Write(piece.Name);
            while (playing)
            {
                for (int i = 0; i < playingIndicatorFrames.Length; i++)
                {
                    if (playing == false)
                        break;
                    Console.CursorLeft = promptLeft+piece.Name.Length;
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
            object[] songWithName = (object[])parameters;
            NotationTranstalor.Note[] notes = (NotationTranstalor.Note[])songWithName[1];
            StreamWriter m = new StreamWriter("melody.txt");
            foreach (NotationTranstalor.Note n in notes)
                m.WriteLine("{0} {1}", (int)n.Frequncy, (int)n.Duration);
            m.Close();
            if (systemBeeper)
            {
                foreach (NotationTranstalor.Note note in notes)
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
                SoundGenerator sg = new SoundGenerator(22050, BitDepth.Bit16, 1, audioFileStream);
                double[] startPhase = new double[] { 0, 0, 0};
                for (int i = 0; i < notes.Length; i++)
                {
                    if(notes[i].Frequncy == 0)
                        startPhase = new double[] { 0, 0, 0 };
                    startPhase = sg.AddComplexTone(notes[i].Duration, startPhase, 1, true, notes[i].Frequncy, notes[i].Frequncy * 2, notes[i].Frequncy * 3);                    
                }
                sg.Save();
                if (saveToFile)
                {
                    string fileHash = string.Empty;
                    using (var cp = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                    {
                      fileHash =  BitConverter.ToString(cp.ComputeHash(audioFileStream.ToArray()), 14);
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
                int sl = (int)songWithName[2]+20;
                while (sl > sw.ElapsedMilliseconds)
                {
                    if (cancelPressed)
                    {                        
                        player.Stop();                       
                        break;
                    }
                }     
                audioFileStream.Close();
            }
            playing = false;            
        }            

        static void PrintNotification(string text, int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.WriteLine(text);          
        }

        static int GetSongLength(NotationTranstalor.Song s)
        {
            double result = 0;
            NotationTranstalor.Note[] notes = null;
            try
            {
                notes = NotationTranstalor.TranslateNotation(s.Text);
            }
            catch
            {
                return 0;
            }
            foreach (NotationTranstalor.Note n in notes)
                result += n.Duration;
            return (int)result;
        }

        static NotationTranstalor.Song[] ReadPlayList(string fileText)
        {
            if (fileText == null)
                throw new ArgumentNullException();
            string[] splitted = fileText.Split('\n');
            NotationTranstalor.Song[] pieces = new NotationTranstalor.Song[splitted.Length];
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
                        pieces[i] = new NotationTranstalor.Song(name, string.Empty, text);                       
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            return pieces;
        }        
    }
}