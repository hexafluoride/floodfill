using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace flood {
    class MainClass {
        static string SAVE_DIR = "./.ffill_saves";
        public static void Main(string[] args) {
            if (args.Length != 0) {
                if(args[0] == "--help") 
                {
                    Console.WriteLine("Usage: flood.exe [/path/to/save/file] | [--help]\n");
                    Console.WriteLine("--help     \tShows this text.\n");
                    Console.WriteLine("FloodFill will automatically start a specified game if a save file is specified.");
                    return;
                } 
                else
                {
                    StartGameByFile(string.Join(" ", args));
                }
            }
            while(true) {
                Console.Clear();
                Console.WriteLine("Welcome to Flood Fill™!");
                Console.WriteLine("Press\n S to start,\n O to change options,\n L to load a saved game,\n D to delete a saved game or\n Q to quit.");
                switch(Console.ReadKey(true).KeyChar) {
                    case 'S': //Start
                    case 's': Game();
                        continue;
                    case 'O': //Options
                    case 'o': Settings();
                        continue;
                    case 'L':
                    case 'l': LoadGames();
                        continue;
                    case 'D':
                    case 'd': DeleteSaveGames();
                        continue;
                    case 'Q'://Quit
                    case 'q':
                        return;
                }
            }
        }

        private static void Game(Grid grid) {
            Console.Clear();
            grid.PrintOut();
            bool savedirty = false; // only confirm quitting if game is not saved.

            while(!grid.Solved()) {
                char c = Console.ReadKey(true).KeyChar;
                if(c == 'H' || c == 'h') {
                    grid.Solve(false);
                    grid.PrintOut();
                    break;
                }
                if(c == 'S' || c == 's') {
                    Console.WriteLine("Saving game...");
                    string save = grid.Export();
                    if(!Directory.Exists(SAVE_DIR))
                        Directory.CreateDirectory(SAVE_DIR);
                    string stamp = DateTimeToUnixTimestamp(DateTime.Now).ToString();
                    File.WriteAllText(SAVE_DIR + "/" + stamp, save);
                    Console.WriteLine("Done.");
                    savedirty = false;
                    continue;
                }
                if(c == 'Q' || c == 'q') {
                    if(!savedirty || AskUser("Unsaved data will be lost! Continue?", false))
                        break;
                    Console.Clear();
                    grid.PrintOut();
                }
                if(!char.IsDigit(c))
                    continue;
                int k = int.Parse(c.ToString());
                if(k > Grid.MAX || k == 0)
                    continue;
                grid.Fill(k - 1);
                savedirty = true;
                Console.Clear();
                grid.PrintOut();
            }
            if(grid.Solved()) {
                Console.WriteLine("You win! \nPress any key to continue.");
                Console.ReadKey(true);
            }
        }

        public static void StartGameByFile(string filename)
        {
            if (!File.Exists(filename)) {
                Console.Error.WriteLine("ERROR: Save file doesn't exist.");
                Environment.Exit(1);
            }
            string cont = File.ReadAllText(filename);
            Game(Grid.Import(cont));
        }

        static void Game() {
            Game(new Grid());
        }

        public static void LoadGames() {
            if(!Directory.Exists(SAVE_DIR)) {
                Console.WriteLine("You need to save a game first to load it.\nPress S at any time while playing a game to save it.");
                Console.ReadKey(true);
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(SAVE_DIR);
            if(dir.GetFiles().Length == 0) {
                Console.WriteLine("You need to save a game first to load it.\nPress S at any time while playing a game to save it.");
                Console.ReadKey(true);
                return;
            }
            Console.WriteLine("Select a game to load it:");
            int c = 1;
            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files) {
                Console.WriteLine("{0}) {1}", c++, UnixTimeStampToDateTime(int.Parse(file.Name)).ToString());
            }
        select_save:
            string str = Console.ReadLine();
            int sel = 0;
            if(!int.TryParse(str, out sel) || sel > files.Length) {
                Console.WriteLine("Please try again.");
                goto select_save;
            }
            StartGameByFile(files[sel - 1].FullName);
        }

        public static void DeleteSaveGames() {
            if(!Directory.Exists(SAVE_DIR)) {
                Console.WriteLine("You need to save a game first.\nPress S at any time while playing a game to save it.");
                Console.ReadKey(true);
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(SAVE_DIR);
            if(dir.GetFiles().Length == 0) {
                Console.WriteLine("You need to save a game first.\nPress S at any time while playing a game to save it.");
                Console.ReadKey(true);
                return;
            }
            Console.WriteLine("Select a savegame to delete it.\nIf you want to delete more than one separate them with spaces:");
            int c = 1;
            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files) {
                Console.WriteLine("{0}) {1}", c++, UnixTimeStampToDateTime(int.Parse(file.Name)).ToString());
            }
            string[] strings = Console.ReadLine().Split(' ');
            foreach(string str in strings) {
                int sel = 0;
                if(!int.TryParse(str, out sel) || sel > files.Length) {
                    Console.WriteLine("Invalid number: {0}", str);
                    continue;
                } else
                    files[sel - 1].Delete();
            }
        }

        public static bool AskUser(string msg, bool def) {
            Console.Write(msg + "[" + (def ? "Y" : "N") + (def ? "/n" : "/y") + "]: ");
            char c = Console.ReadKey().KeyChar;
            if(c == 'y' || c == 'Y')
                return true;
            if(c == 'n' || c == 'N')
                return false;
            if(c == (char)10)
                return def;
            return AskUser(msg, def);
        }

        public static DateTime UnixTimeStampToDateTime(int stamp) {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dt = dt.AddSeconds(stamp).ToLocalTime();
            return dt;
        }

        public static int DateTimeToUnixTimestamp(DateTime dt) {
            return (int)(dt - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
        private static void Settings() {
            Grid.HEIGHT = PromptNumber("Enter grid height(must be an odd number)", Grid.HEIGHT);
            Grid.WIDTH = PromptNumber("Enter grid width(must be an odd number)", Grid.WIDTH);
            Grid.MAX = Math.Min(PromptNumber("Enter maximum value (max 9)", Grid.MAX), 9);
            if(Grid.HEIGHT % 2 == 0) Grid.HEIGHT++;
            if(Grid.WIDTH % 2 == 0) Grid.WIDTH++;
        }
        public static int PromptNumber(string msg, int def = -1) {
            if(def == -1) {
                Console.Write(msg + ": ");
                int of = 0;
                string ss = Console.ReadLine();
                if(!int.TryParse(ss, out of))
                    return PromptNumber(msg, def);
                return of;
            }
            Console.Write(msg + "[" + def + "]: ");
            int o = 0;
            string s = Console.ReadLine();
            if(s.Trim() == string.Empty)
                return def;
            if(!int.TryParse(s, out o))
                return PromptNumber(msg, def);
            return o;
        }

    }
    class Grid {
        public static int WIDTH = 19;   // these are just used for setting default values now
        public static int HEIGHT = 19;
        public static int MAX = 6;
        public int localwidth = WIDTH;  // for loading games
        public int localheight = HEIGHT;//
        public int localmax = MAX;      //

        public int[,] arr = new int[WIDTH, HEIGHT];
        ConsoleColor[] COLORS = new ConsoleColor[]{
                                        ConsoleColor.DarkRed,
                                        ConsoleColor.DarkGreen,
                                        ConsoleColor.DarkYellow,
                                        ConsoleColor.DarkCyan,
                                        ConsoleColor.DarkBlue,
                                        ConsoleColor.DarkGray,
                                        ConsoleColor.Blue,
                                        ConsoleColor.Red,
                                        ConsoleColor.Green,
                                };
        public Grid()
            : this(WIDTH, HEIGHT, MAX) {

        }
        public Grid(int width, int height, int max) {
            localwidth = width;
            localheight = height;
            localmax = max;
            arr = new int[width, height];
            Random rnd = new Random();
            for(int x = 0; x < localwidth; x++) {
                for(int y = 0; y < localheight; y++) {
                    arr[x, y] = rnd.Next(localmax);
                }
            }
        }

        public string Solve(bool returnSolvingSequence = false) {
            if(returnSolvingSequence) {
                string ret = "";
                while(!this.Solved()) {
                    int k = this.GetBestColor();
                    ret += k.ToString();
                    this.Fill(k);
                    System.Threading.Thread.Sleep(150);
                    this.PrintOut();
                }
                return ret;
            } else {
                while(!this.Solved()) {
                    this.Fill(GetBestColor());
                    System.Threading.Thread.Sleep(150);
                    this.PrintOut();
                }
                return "";
            }
        }
        public bool Solved() {
            int reference = arr[0, 0];//if not everything has this color it's not solved yet.
            for(int x = 0; x < localwidth; x++) {
                for(int y = 0; y < localheight; y++) {
                    if(arr[x, y] != reference)
                        return false;
                }
            }
            return true;
        }
        public void Fill(int color) {
            int originalcolor = arr[localwidth / 2, localheight / 2];
            bool[,] visited = new bool[localwidth, localheight];
            var nodes = new Stack<KeyValuePair<int, int>>();
            nodes.Push(new KeyValuePair<int, int>(localwidth / 2, localheight / 2));
            while(nodes.Count > 0) {
                var active = nodes.Pop();
                int x = active.Key;
                int y = active.Value;
                visited[x, y] = true;

                if(arr[x, y] != originalcolor) {
                    continue;
                }
                arr[x, y] = color;
                if(x + 1 < localwidth && !visited[x + 1, y])
                    nodes.Push(new KeyValuePair<int, int>(x + 1, y));
                if(x - 1 >= 0 && !visited[x - 1, y])
                    nodes.Push(new KeyValuePair<int, int>(x - 1, y));
                if(y - 1 >= 0 && !visited[x, y - 1])
                    nodes.Push(new KeyValuePair<int, int>(x, y - 1));
                if(y + 1 < localheight && !visited[x, y + 1])
                    nodes.Push(new KeyValuePair<int, int>(x, y + 1));
            }
        }

        public int GetBestColor() {
            //basically the same algorithm as the landfill but it doesn't fill but stores 
            //which neighbouring color is the most common. Only a heuristic - not really the best option.
            int[] colors = new int[localmax];
            int originalcolor = arr[localwidth / 2, localheight / 2];
            bool[,] visited = new bool[localwidth, localheight];
            var nodes = new Stack<KeyValuePair<int, int>>();
            nodes.Push(new KeyValuePair<int, int>(localwidth / 2, localheight / 2));
            while(nodes.Count > 0) {
                var active = nodes.Pop();
                int x = active.Key;
                int y = active.Value;
                visited[x, y] = true;
                if(arr[x, y] != originalcolor) {
                    colors[arr[x, y]]++;//this line differs!
                    continue;
                }
                if(x + 1 < localwidth && !visited[x + 1, y])
                    nodes.Push(new KeyValuePair<int, int>(x + 1, y));
                if(x - 1 >= 0 && !visited[x - 1, y])
                    nodes.Push(new KeyValuePair<int, int>(x - 1, y));
                if(y - 1 >= 0 && !visited[x, y - 1])
                    nodes.Push(new KeyValuePair<int, int>(x, y - 1));
                if(y + 1 < localheight && !visited[x, y + 1])
                    nodes.Push(new KeyValuePair<int, int>(x, y + 1));
            }
            return colors.ToList().IndexOf(colors.Max());
        }

        public string Export() {
            return string.Format("W:{0}\n" +
                                 "H:{1}\n" +
                                 "M:{2}\n" +
                                 "{3}\n",
                                 localwidth,
                                 localheight,
                                 localmax,
                                 this.RawDump());

        }
         public static Grid Import(string save)
        {
            string[] lines = save.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 4) {
                Console.Error.WriteLine("ERROR: Invalid save file, returning random Grid");
                return new Grid();
            }
            int w = int.Parse(lines[0].Split(new[] { ':' })[1]);
            int h = int.Parse(lines[1].Split(new[] { ':' })[1]);
            int m = int.Parse(lines[2].Split(new[] { ':' })[1]);
            string array_data = lines[3];
            Grid grid = new Grid(w, h, m);
            for(int x = 0; x < w; x++) {
                for(int y = 0; y < h; y++) {
                    int val = int.Parse(array_data[(x * w) + y].ToString());
                    if(val > m)
                    {
                        Console.Error.WriteLine("WARNING: Value bigger than MAX at {0}, {1}, truncating to MAX", x, y);
                        val = m;
                    }
                    grid.arr[x, y] = val;
                }
            }
            return grid;
        }

        public void PrintOut() {
            Console.Clear();
            for(int y = 0; y < localheight; y++) {
                for(int x = 0; x < localwidth; x++) {
                    Console.BackgroundColor = COLORS[arr[x, y]];
                    Console.Write(arr[x, y] + 1);
                }
                Console.WriteLine();
            }
            Console.BackgroundColor = ConsoleColor.Black;
        }
        public string RawDump() {
            string ret = "";
            for(int x = 0; x < localwidth; x++) {
                for(int y = 0; y < localheight; y++) {
                    ret += arr[x, y].ToString();
                }
            }
            return ret;
        }
    }
}