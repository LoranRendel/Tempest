using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Media;
using System.Threading;
using WaveGenerator;
using System.Text.RegularExpressions;
using Simue;

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
        static ushort sampleRate = 44000;
        static string windowTitle = "Tempest";
        static bool playing = false;
        static bool playPrompt = false;
        static bool running = true;
        static bool systemBeeper = false;
        static bool simple = true;
        static char[] playingIndicatorFrames = new char[] { '|', '/', '-', '\\' };
        static string defaultPlaylistPath = "songs.txt";
        static Song[] pieces = null;
        static FileInfo playListFile = null;
        static bool cancelPressed = false;
        static SimueCompiler compiler = new SimueCompiler();

        static int Main()
        {
            var originalEncoding = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.GetEncoding(1251);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            ShowWelcomeScreen();
            OpenPlayList(defaultPlaylistPath);
            PrintPlayList();
            while (running)
                PlayerPrompt();
            Console.OutputEncoding = originalEncoding;
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

        static Song[] ReadPlayList(string fileText)
        {
            SimueCompiler compiler = new SimueCompiler();
            if (fileText == null)
                throw new ArgumentNullException();
            string[] splitted = fileText.Split('\n');
            Regex playListEntryChecker = new Regex("^(?<name>.+)(?<open>\\()[^\\(\\)]+(?<close-open>\\))$");
            List<Song> pieces = new List<Song>();
            for (int i = 0; i < splitted.Length; i++)
            {
                splitted[i] = splitted[i].Replace("\r", "");
                splitted[i] = splitted[i].Trim();
                Match entry = playListEntryChecker.Match(splitted[i]);
                if (entry.Success)
                    try
                    {
                        var result = compiler.Parse(compiler.Tokenize(entry.Groups["close"].Value));
                        if (result.Errors.Count == 0)
                            pieces.Add(new Song
                            {
                                Name = entry.Groups["name"].Value,
                                Text = entry.Groups["close"].Value,
                                Author = string.Empty,
                                Notes = result.Song.Notes
                            });
                    }
                    catch
                    {
                        continue;
                    }
            }
            if (pieces.Count == 0)
                return null;
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
                double lm = pieces[i].Length;
                TimeSpan length = new TimeSpan(0, 0, 0, (int)lm / 1000, (int)(lm - lm / 1000 * 1000));
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
                            Song[] temp = pieces;
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
                        Console.WriteLine(new string(' ', notificationLeft) + manual.Replace("\r\n", "\r\n" + new string(' ', notificationLeft)));
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
                    var compRes = compiler.Parse(compiler.Tokenize(answer));
                    if (compRes.Errors.Count == 0)
                    {
                        Song enteredSong = new Song
                        {
                            Name = "Untitled",
                            Author = string.Empty,
                            Notes = compRes.Song.Notes
                        };
                        PlayPiece(enteredSong, Program.systemBeeper);
                    }
                    else
                    {
                        PrintNotification("Не удалось воспроизвести мелодию из-за ошибки в записи.", notificationLeft);
                    }
                }
                catch
                {
                    PrintNotification("Не удалось воспроизвести мелодию из-за ошибки в записи.", notificationLeft);
                }
            }
        }

        static void PlayPiece(Song piece, bool systemBeeper)
        {
            WaveFile audioFile = null;
            if (!systemBeeper)
            {
                string text = "Генерация мелодии";
                PrintNotification(text, notificationLeft);
                audioFile = GenerateSongFile(piece);
                Console.CursorTop = Console.CursorTop - 1;
                PrintNotification(new string(' ', text.Length), notificationLeft);
                Console.CursorTop = Console.CursorTop - 1;
            }
            playing = true;
            Thread playThread = new Thread
                (delegate ()
                    {
                        if (systemBeeper)
                            PlaySystemBeeper(piece);
                        else
                        {
                            audioFile = GenerateSongFile(piece);
                            if (saveToFile)
                                SaveGeneratedSongToFile(piece.Name, (MemoryStream)audioFile.File);
                            PlayWaveFile(audioFile);
                        }
                        playing = false;
                    }
                );
            playThread.Start();
            Console.CursorLeft = promptLeft;
            Console.Write(piece.Name);
            while (playing)
                for (int i = 0; i < playingIndicatorFrames.Length; i++)
                {
                    if (playing == false)
                        break;
                    Console.CursorLeft = promptLeft + piece.Name.Length;
                    Console.Write(" " + playingIndicatorFrames[i].ToString());
                    Thread.Sleep(50);
                }
            //Free memory after song generation
            if (audioFile != null)
            {
                audioFile.File.Dispose();
                audioFile = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.CursorLeft = promptLeft + piece.Name.Length;
            Console.WriteLine("  ");
            cancelPressed = false;
        }

        private static void PlayWaveFile(WaveFile audioFile)
        {
            audioFile.File.Position = 0;
            SoundPlayer player = new SoundPlayer(audioFile.File);
            player.Play();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            uint sl = audioFile.SampleCount / audioFile.SampleRate * 1000 + 1000;
            while (sl > sw.ElapsedMilliseconds)
                if (cancelPressed)
                {
                    player.Stop();
                    sw.Stop();
                    break;
                }
            player.Stop();
            player.Dispose();
        }

        private static void PlaySystemBeeper(Song song)
        {
            foreach (Note note in song.Notes)
            {
                if (cancelPressed == true)
                    break;
                if (note.Frequency > 37)
                    Console.Beep((int)note.Frequency, (int)note.Duration);
                else
                    System.Threading.Thread.Sleep((int)note.Duration);
            }
        }

        private static WaveFile GenerateSongFile(Song song)
        {
            MemoryStream audioFileStream = new MemoryStream();
            WaveFile wavFile = new WaveFile(Program.sampleRate, BitDepth.Bit16, 1, audioFileStream);
            SoundGenerator sg = new SoundGenerator(wavFile);

            for (int i = 0; i < song.Notes.Length; i++)
            {
                if (simple)
                    sg.AddSimpleTone(song.Notes[i].Frequency, song.Notes[i].Duration);
                else
                    sg.AddComplexTone(song.Notes[i].Duration, true, song.Notes[i].Frequency, song.Notes[i].Frequency * 2, song.Notes[i].Frequency * 3);
            }
            sg.Save();
            return wavFile;
        }

        private static void SaveGeneratedSongToFile(string fileName, MemoryStream audioFileStream)
        {
            audioFileStream.Position = 0;
            Regex regex = new Regex("[\\\\/\":\\*\\?<>\\|]");
            fileName = regex.Replace(fileName, " ");
            string baseName = fileName;
            fileName = string.Format("{0}.wav", fileName);
            int count = 1;
            while (File.Exists(fileName))
            {
                fileName = string.Format("{0} ({1}).wav", baseName, count);
                count++;
            }
            using (FileStream file = new FileStream(fileName, FileMode.Create))
            {
                audioFileStream.WriteTo(file);
                file.Close();
            }
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