using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace flood {
    enum GameResult {
        Lose, Win, Quit, Empty
    }
    class MainClass {
        static string SAVE_DIR = "./.ffill_saves";
        static int LEVELS_PER_STAGE = 10;
        static int STAGES = 5;
        public static void Main(string[] args) {
            if(args.Length != 0) {
                if(args[0] == "--help") {
                    Console.WriteLine("Usage: flood.exe [/path/to/save/file] | [--help]\n");
                    Console.WriteLine("--help     \tShows this text.\n");
                    Console.WriteLine("FloodFill will automatically start a specified game if a save file is specified.");
                    return;
                } else {
                    StartGameByFile(string.Join(" ", args));
                }
            }
            while(true) {
                Console.Clear();
                Console.WriteLine("Welcome to Flood Fill™!");
                Console.WriteLine("Press\n S to start in standard mode,\n C to start in challenge mode,\n O to change options,\n L to load a saved game,\n D to delete a saved game or\n Q to quit.");
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
                    case 'C':
                    case 'c': Challenge();
                        continue;
                    case 'Q'://Quit
                    case 'q':
                        return;
                }
            }
        }
        static void Challenge() {
            Challenge(new Grid(19, 19, 4));
        }
        private static void Challenge(Grid start, int c_width = 19, int c_height = 19, int c_max = 4, int level = 0, int stage = 1) {
            bool first = true;
            for(int i = stage; i < STAGES + 1; i++) {
                for(int k = level; k < LEVELS_PER_STAGE; k++) {
                    GameResult win = GameResult.Empty;
                    if(first) {
                        win = Game(start, LEVELS_PER_STAGE - k, string.Format("Stage {0}, level {1}", i, k), true, i, k);
                        first = false;
                    } else {
                        win = Game(new Grid(c_width, c_height, c_max), LEVELS_PER_STAGE - k, string.Format("Stage {0}, level {1}", i, k), true, i, k);
                    }
                    if(win == GameResult.Lose || win == GameResult.Quit) {
                        Console.Clear();
                        return;
                    }
                }
                c_width += 5;
                c_height += 5;
                c_max++;
            }
        }
        static void ResumeChallenge(int level, int stage, Grid grid) {
            int width = 19 + (stage * 5);
            int height = width;
            int max = 4 + stage;
            Challenge(grid, width, height, max, stage, level);
        }



        // TODO: Clean this up and break it into functions
        private static GameResult Game(Grid grid, int tolerance = 5, string display = "", bool challenge = false, int stage = -1, int level = -1) {
            int moves = grid.GetNumberOfSteps() + tolerance;
            bool savedirty = false; // only confirm quitting if game is not saved.
            PrintGame(grid, challenge, display, moves);
            while(!grid.Solved) {
                if(challenge && moves == 0) {
                    Console.WriteLine("You're out of moves!\nPress any key to continue.");
                    Console.ReadKey();
                    return GameResult.Lose;
                }
                char c = Console.ReadKey(true).KeyChar;
                if((c == 'H' || c == 'h')) {//why is this only possible in challenge mode?
                    grid.Solve(false);
                    break;
                } else if((c == 'S' || c == 's')) {
                    SaveGame(grid, challenge, stage, level);
                    savedirty = false;
                } else if(c == 'Q' || c == 'q') {
                    if(!savedirty || QuitGame(challenge)) {
                        return GameResult.Quit;
                    }
                } else if(char.IsDigit(c)) {
                    bool validDigit = grid.Move(c);
                    if(validDigit) {
                        moves--;
                        savedirty = true;
                    }
                } else {//no valid key was pressed - wait for next key.
                    continue;
                }
                //All input is handled - print the updated grid and check if challenge mode is lost
                PrintGame(grid, challenge, display, moves);
            }
            if(grid.Solved) {//this should always be true - if it's not something has gone wrong.
                PrintGame(grid, challenge, display, moves);
                if(challenge)
                    Console.WriteLine("You won with {0} moves left! \nPress any key to continue.", moves);
                else
                    Console.WriteLine("You won! \nPress any key to continue.");
                Console.ReadKey(true);
                return GameResult.Win;
            }
            //basically unreachable code.
            return GameResult.Quit;
        }

        private static void PrintGame(Grid grid, bool challengeMode, string display, int moves) {
            Console.Clear();
            Console.Write(display);
            if(challengeMode)
                Console.WriteLine(", {0} moves left.", moves);
            grid.PrintOut();
        }

        public static void SaveGame(Grid grid, bool challengeMode, int stage, int level) {
            Console.WriteLine("Saving game...");
            string save = grid.Export();
            if(!Directory.Exists(SAVE_DIR))
                Directory.CreateDirectory(SAVE_DIR);
            string stamp = DateTimeToUnixTimestamp(DateTime.Now).ToString();
            if(challengeMode) {
                stamp += "_";
                save += stage + ":" + level;
            }
            File.WriteAllText(SAVE_DIR + "/" + stamp, save);
            Console.WriteLine("Done. Press any key to continue");
            Console.ReadKey(true);
        }

        public static bool QuitGame(bool challengeMode) {
            string question;
            if(challengeMode)
                question = "You're in challenge mode! All your progress will be lost! Continue?";
            else
                question = "Unsaved data will be lost! Continue?";
            return AskUser(question, false);

        }

        public static void StartGameByFile(string filename) {
            if(!File.Exists(filename)) {
                Console.Error.WriteLine("ERROR: Save file doesn't exist.");
                Environment.Exit(1);
            }
            string cont = File.ReadAllText(filename);
            if(Path.GetFileName(filename).Contains("_")) {
                string[] lines = File.ReadAllLines(filename);
                int stage = int.Parse(lines[4].Split(new char[] { ':' })[0]);
                int level = int.Parse(lines[4].Split(new char[] { ':' })[1]);
                Grid grid = Grid.Import(cont);
                ResumeChallenge(level, stage, grid);
                return;
            }
            Game(Grid.Import(cont));
        }

        static void Game() {
            Game(new Grid());
        }

        static string EscapableReadLine(ConsoleKey escapekey = ConsoleKey.Escape) {
            string ret = "";
            while(true) {
                ConsoleKeyInfo key = Console.ReadKey();
                if(key.Key == ConsoleKey.Enter)
                    break;
                else if(key.Key == escapekey)
                    return null;
                ret += key.KeyChar;
            }
            return ret == "" ? null : ret;
        }

        public static void LoadGames() {
            //TODO: implement something to abort loading a game. (pressing escape or whatever)
            FileInfo[] files = PrintSaveList();
            if(files.Length == 0)
                return;
        select_save:
            string str = EscapableReadLine();
            if(str == null)
                return;
            int sel = 0;
            if(!Directory.Exists(SAVE_DIR)) {
                Console.Error.WriteLine("ERROR: Save directory disappeared! Press any key to continue");
                Console.ReadKey(false);
                return;
            }
            if(!int.TryParse(str, out sel) || sel > files.Length) {
                Console.WriteLine("Please try again.");
                goto select_save;
            }
            StartGameByFile(files[sel - 1].FullName);
        }

        public static void DeleteSaveGames() {
            FileInfo[] files = PrintSaveList("delete");
            if(files.Length == 0)
                return;
            string line = EscapableReadLine();
            if(line == null)
                return;
            string[] strings = line.Split(' ');
            foreach(string str in strings) {
                int sel = 0;
                if(!Directory.Exists(SAVE_DIR)) {
                    Console.Error.WriteLine("ERROR: Save directory disappeared! Press any key to continue");
                    Console.ReadKey(false);
                    return;
                }
                if(!int.TryParse(str, out sel) || sel > files.Length) {
                    Console.WriteLine("Invalid number: {0}", str);
                    continue;
                } else {
                    if(!Directory.Exists(SAVE_DIR)) {
                        Console.Error.WriteLine("ERROR: Save directory disappeared! Press any key to continue");
                        Console.ReadKey(false);
                        return;
                    }
                    files[sel - 1].Delete();
                }
            }
        }

        public static FileInfo[] PrintSaveList(string action = "load") {
            FileInfo[] empty = new FileInfo[0];
            if(!Directory.Exists(SAVE_DIR)) {
                Console.WriteLine("You need to save a game first to {0} it.\nPress S at any time while playing a game to save it.", action);
                Console.ReadKey(true);
                return empty;
            }
            DirectoryInfo dir = new DirectoryInfo(SAVE_DIR);
            if(dir.GetFiles().Length == 0) {
                Console.WriteLine("You need to save a game first to {0} it.\nPress S at any time while playing a game to save it.", action);
                Console.ReadKey(true);
                return empty;
            }
            Console.WriteLine("Select a game to {0} it:", action);
            int c = 1;
            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files) {
                string name = file.Name.Replace("_", "");
                if(name.Count(t => !char.IsNumber(t)) > 0) {
                    Console.Error.WriteLine("ERROR: Skipping save file {0}, invalid name", name);
                    continue;
                }
                DateTime time = UnixTimeStampToDateTime(int.Parse(name));
                Console.Write("{0}) {1}", c++, time.ToString());
                if(file.Name.Contains("_"))
                    Console.WriteLine(", challenge mode");
                else
                    Console.WriteLine();
            }
            return files;
        }

        public static bool AskUser(string msg, bool def) {
            Console.Write(msg + "[" + (def ? "Y" : "N") + (def ? "/n" : "/y") + "]: ");
            var k = Console.ReadKey();
            if(k.Key == ConsoleKey.Escape) {
                return def;
            }
            char c = k.KeyChar;
            if(c == 'y' || c == 'Y')
                return true;
            if(c == 'n' || c == 'N')
                return false;
            if(c == (char)10)
                return def;
            return AskUser(msg, def);
        }
        private static void Settings() {
            Grid.HEIGHT = PromptNumber("Enter grid height(must be an odd number)", Grid.HEIGHT);
            if(Grid.HEIGHT % 2 == 0) {
                Console.WriteLine("{0} is an illegal value for grid height - set it to {1} instead", Grid.HEIGHT, Grid.HEIGHT + 1);
                Grid.HEIGHT++;
            }
            Grid.WIDTH = PromptNumber("Enter grid width(must be an odd number)", Grid.WIDTH);
            if(Grid.WIDTH % 2 == 0) {
                Console.WriteLine("{0} is an illegal value for grid width - set it to {1} instead", Grid.WIDTH, Grid.WIDTH + 1);
                Grid.WIDTH++;
            }
            Grid.MAX = PromptNumber("Enter maximum value (max 9)", Grid.MAX);
            if(Grid.MAX <= 0 || Grid.MAX > 9) {
                int old = Grid.MAX;
                Grid.MAX = Math.Min(9, Math.Max(0, Grid.MAX));
                Console.WriteLine("{0} is an illegal value for maximum value - set it to {1} instead", old, Grid.MAX);
            }
            Console.WriteLine("Settings updated - press any key to continue.");
            Console.ReadKey(false);
        }
        public static DateTime UnixTimeStampToDateTime(int stamp) {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dt = dt.AddSeconds(stamp).ToLocalTime();
            return dt;
        }
        public static int DateTimeToUnixTimestamp(DateTime dt) {
            return (int)(dt - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
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
        public int seed = 0;

        public int[,] arr = new int[WIDTH, HEIGHT];
        ConsoleColor[] COLORS = new ConsoleColor[]{
                                        ConsoleColor.White,
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
        public Grid(int width, int height, int max, int s = 0) {
            localwidth = width;
            localheight = height;
            localmax = max;
            arr = new int[width, height];
            Random s1 = new Random();
            if(s == 0) {
                s = s1.Next();
                seed = s;
            }
            Random rnd = new Random(s);
            for(int x = 0; x < localwidth; x++) {
                for(int y = 0; y < localheight; y++) {
                    arr[x, y] = rnd.Next(localmax) + 1;
                }
            }
            arr[width / 2, height / 2] = 0;
        }

        static char lastChar = '\0';
        public bool Move(char c) {//returns true if char was valid.
            int k = int.Parse(c.ToString());
            //check if char is invalid or same as last char.
            if(c == lastChar || k > this.localmax || k == 0) { // check against maximum of the current game!
                return false;
            }
            lastChar = c;
            this.Fill(k);
            return true;
        }

        public string Solve(bool returnSolvingSequence = false) {
            if(returnSolvingSequence) {
                string ret = "";
                while(!this.Solved) {
                    int k = this.GetBestColor();
                    ret += k.ToString();
                    this.Fill(k);
                }
                return ret;
            } else {
                while(!this.Solved) {
                    this.Fill(GetBestColor());
                    System.Threading.Thread.Sleep(10);
                    Console.SetCursorPosition(0, 1);
                    this.PrintOut();
                }
                return "";
            }
        }
        public bool Solved {
            get {
                int reference = arr[0, 0];//if not everything has this color it's not solved yet.
                for(int x = 0; x < localwidth; x++) {
                    for(int y = 0; y < localheight; y++) {
                        if(arr[x, y] != reference)
                            return false;
                    }
                }
                return true;
            }
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

        public int GetNumberOfSteps() {
            Grid gr = new Grid(localwidth, localheight, localmax, seed);
            string str = gr.Solve(true);
            return str.Length;
        }

        public int GetBestColor() {
            //basically the same algorithm as the landfill but it doesn't fill but stores 
            //which neighbouring color is the most common. Only a heuristic - not really the best option.
            int[] colors = new int[localmax + 1];
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
                    colors[arr[x, y]]++;
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
        public static Grid Import(string save) {
            string[] lines = save.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if(lines.Length < 4) {
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
                    if(val > m) {
                        Console.Error.WriteLine("WARNING: Value bigger than MAX at {0}, {1}, truncating to MAX", x, y);
                        val = m;
                    }
                    grid.arr[x, y] = val;
                }
            }
            return grid;
        }

        public void PrintOut() {
            Console.CursorTop = 1;
            int left = (Console.WindowWidth - localwidth) / 2;
            for(int y = 0; y < localheight; y++) {
                Console.CursorLeft = left;
                for(int x = 0; x < localwidth; x++) {
                    Console.BackgroundColor = COLORS[arr[x, y]];
                    Console.Write(arr[x, y]);
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
