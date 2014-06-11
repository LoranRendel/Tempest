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
        static FileInfo playList = new FileInfo(defaultPlayListPath);
        static NotationTranstalor translator = new NotationTranstalor();
        [STAThread]
        static int Main()
        {
            ShowWelcomeScreen();
            if (!playList.Exists)
                OpenPlayList();
            else
            {
                StreamReader reader = playList.OpenText();
                pieces = ReadPlayList(reader.ReadToEnd());
                reader.Close();
                PrintSongList();
            }
            while (running)
            {
                PlayerPrompt();
            }
            return 0;
        }

        private static void OpenPlayList()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выберите файл со списком мелодий";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                playList = new FileInfo(ofd.FileName);
                StreamReader reader = playList.OpenText();
                pieces = ReadPlayList(reader.ReadToEnd());
                reader.Close();
                PrintSongList();
            }
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

        static void PrintSongList()
        {
            if (pieces == null)
                return;
            Console.CursorLeft = 4;
            Console.WriteLine("Для прослушивания доступны следующие мелодии:\n");
            for (int i = 0; i < pieces.Length; i++)
            {
                Console.CursorLeft = 6;
                if (i != pieces.Length - 1)
                    Console.WriteLine("{0}. {1};", i + 1, pieces[i].name);
                else
                    Console.WriteLine("{0}. {1}.\n", i + 1, pieces[i].name);
            }
        }

        static void PlayerPrompt()
        {
            string answer = string.Empty;

            int pieceNumber = 0;
            Console.CursorLeft = 4;
            Console.Write("Проиграть мелодию: ");
            answer = Console.ReadLine();
            if (int.TryParse(answer, out pieceNumber) && pieceNumber <= pieces.Length && pieceNumber > 0)
            {
                Thread indicator = PrintIndicatorAsync(4 + "Проиграть мелодию: ".Length + answer.Length + 1, 100);
                PlayPiece(pieces[pieceNumber - 1]);
                indicator.Abort();
            }
            else
            {
                Console.CursorLeft = 6;
                switch (answer)
                {
                    case "sysb":
                        systemBeeper = true;
                        Console.WriteLine("Теперь звуки будут воспроизводиться через системный бипер");
                        break;
                    case "wav":
                        systemBeeper = false;
                        Console.WriteLine("Теперь звуки будут воспроизводиться при помощи wav-файла");
                        break;
                    case "open":
                        OpenPlayList();
                        break;
                    case "reload":
                        StreamReader r = playList.OpenText();
                        pieces = ReadPlayList(r.ReadToEnd());
                        r.Close();
                        Console.WriteLine("Плейлист перезагружен");
                        PrintSongList();
                        break;
                    case "play":
                        Console.Write(">>> ");
                        Song enteredSong = new Song() { text = Console.ReadLine() };
                        try
                        {
                            PlayPiece(enteredSong);
                        }
                        catch
                        {
                            Console.CursorLeft = 4;
                            Console.WriteLine("Не удалось воспроизвести мелодию из-за ошибки в записи");
                        }
                        break;
                    case "q":
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

        static void PlayPiece(Song piece)
        {
            //Console.CursorVisible = false;
            NotationTranstalor.Note[] notes = translator.TranslateNotation(piece.text);
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
                Wave audioFile = new Wave(22050);
                foreach (NotationTranstalor.Note note in notes)
                {
                    audioFile.addWave((int)note.Frequncy, note.Duration);
                }
                MemoryStream audioFileStream = new MemoryStream();
                audioFile.saveFile(audioFileStream);
                audioFileStream.Position = 0;
                SoundPlayer player = new SoundPlayer(audioFileStream);
                player.PlaySync();
                audioFileStream.Close();
            }
            Console.CursorVisible = true;
        }

        static Song[] ReadPlayList(string file)
        {
            string[] splitted = file.Split('\n');
            Song[] pieces = new Song[splitted.Length];
            for (int i = 0; i < splitted.Length; i++)
            {
                //Is it proper song?
                splitted[i] = splitted[i].Replace('\r', '\0');
                if (splitted[i].Contains("(") && splitted[i].Contains(")"))
                {
                    int open = 0, close = 0;
                    for (int j = 0; j < splitted[i].Length; j++)
                    {
                        if (splitted[i][j] == '(')
                            open = j;
                        if (splitted[i][j] == ')')
                            close = j;
                    }
                    string text, name;
                    if (open < close)
                    {
                        text = splitted[i].Substring(open + 1, close - open - 1);
                        name = splitted[i].Substring(0, open);
                        pieces[i].text = text;
                        pieces[i].name = name;
                    }

                }
            }
            return pieces;
        }

        struct Song
        {
            public string name;
            public string text;
        }

    }
}