using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace AnimalShogi
{
    public class Player
    {
        public Player(TcpClient c, NetworkStream n, int i)
        {
            this.tcp = c;
            this.stream = n;
            this.pID = i;
        }

        public TcpClient Tcp() {
            return tcp;
        }

        public NetworkStream Stream() {
            return stream;
        }

        public Player Opponent() {
            return  opponent;
        }

        public int GameId() {
            return gID;
        }

        public bool FirstPlayer() {
            return firstPlayer;
        }

        public int PlayerId() {
            return pID;
        }

        public Color MyColor() {
            return color;
        }

        public bool Waiting() {
            return waiting;
        }

        public void SetOpponent(Player opp) {
            opponent = opp;
        }

        public void SetPlayerId(int id) {
            pID = id;
        }

        public void SetGameId(int id) {
            gID = id;
        }

        public void SetFirstPlayer(bool b) {
            firstPlayer = b;
        }

        public void SetColor(Color c) {
            color = c;
        }

        public void SetWaiting(bool w) {
            waiting = w;
        }

        private TcpClient tcp;
        private NetworkStream stream;
        private Player opponent;
        private int pID;
        private int gID;
        private bool firstPlayer;
        private bool waiting;

        private Color color;
    }

    public class Game
    {
        public Game(Player one, Player two, int i)
        {
            this.pos = new Position();
            this.player1 = one;
            this.player2 = two;
            this.id = i;
        }

        public Player Player1() {
            return player1;
        }

        public Player Player2() {
            return player2;
        }

        public int Id() {
            return id;
        }

        public Position pos;

        private Player player1;
        private Player player2;
        private int id;
    }

    public class Server
    {
        static List<Game> games = new List<Game>();
        static List<Player> players = new List<Player>();
        static readonly object gamesLock = new object();
        TcpListener listener;
        Thread clientThread;
        TcpClient client;
        NetworkStream nwStream;
        static Player waitingPlayer = null;
        static readonly object waitingPlayerLock = new object();
        int playerID = 1;
        int gameID = 1;

        string[] beginSummaryStr = new string[] {
            "BEGIN Game_Summary\nGame_ID:" + DateTime.Now.ToString("yyyyMMdd-HH-mm-ss") + "\nYour_Turn:+\nEND Game_Summary\n",
            "BEGIN Game_Summary\nGame_ID:" + DateTime.Now.ToString("yyyyMMdd-HH-mm-ss") + "\nYour_Turn:-\nEND Game_Summary\n",
        };

        byte[][] beginSummary;

        public Server(string port)
        {
            beginSummary = new byte[(int)Color.COLOR_NB][] {
                Encoding.GetEncoding("UTF-8").GetBytes(beginSummaryStr[(int)Color.BLACK]),
                Encoding.GetEncoding("UTF-8").GetBytes(beginSummaryStr[(int)Color.WHITE])
            };

            int portInt;
            string portString = port;
            bool good = true;

            do {
                while(Int32.TryParse(portString, out portInt) == false) 
                {
                    Console.WriteLine("Enter port");
                    portString = Console.ReadLine();
                }

                try {
                    IPAddress address = IPAddress.Any;
                    listener = new TcpListener(address, portInt);
                    listener.Start();
                    Console.WriteLine("Setup is successful. Waiting for clients");
                    matchMaking();
                } catch(Exception e) {
                    good = false;
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try again");
                }
            } while (good == false);
        }

        private void matchMaking()
        {
            while (true) //Server main loop
            {
                // wait for clients to connect
                client = listener.AcceptTcpClient();
                nwStream = client.GetStream();
                Player newPlayer = new Player(client, nwStream, playerID++);
                players.Add(newPlayer);
                clientThread = new Thread(new ParameterizedThreadStart(clientComm));
                clientThread.Start(newPlayer);
                addPlayer(newPlayer);
            }
        }

        private void addPlayer(Player nPlayer)
        {
            Console.WriteLine("Player #" + nPlayer.PlayerId() + " joined");

            lock (waitingPlayerLock)
            {
                if (waitingPlayer == null)
                {
                    waitingPlayer = nPlayer;
                }
                else
                {
                    waitingPlayer.SetFirstPlayer(true);
                    nPlayer.SetFirstPlayer(false);
                    waitingPlayer.SetGameId(gameID);
                    nPlayer.SetGameId(gameID);
                    waitingPlayer.SetOpponent(nPlayer);
                    nPlayer.SetOpponent(waitingPlayer);

                    // 先後をランダムに決める
                    int t = new Random().Next(10000);
                    if (t % 2 == 0) {
                        nPlayer.SetColor(Color.BLACK);
                        waitingPlayer.SetColor(Color.WHITE);
                    }
                    else {
                        nPlayer.SetColor(Color.WHITE);
                        waitingPlayer.SetColor(Color.BLACK);
                    }

                    lock (gamesLock)
                    {
                        games.Add(new Game(waitingPlayer, nPlayer, gameID));
                    }

                    //Tell clients to start game
                    waitingPlayer.Stream().Write(beginSummary[(int)waitingPlayer.MyColor()], 0, beginSummary[(int)waitingPlayer.MyColor()].Length);
                    nPlayer.Stream().Write(beginSummary[(int)nPlayer.MyColor()], 0,  beginSummary[(int)nPlayer.MyColor()].Length);
                    Console.WriteLine("Started game #" + gameID + ", with player #" + waitingPlayer.GameId() + " and player #" + nPlayer.PlayerId());
                    waitingPlayer = null;
                    gameID++;
                }
            }
        }

        private void clientComm(object p)
        {
            bool isready = false;
            int bytesRead;
            Player threadPlayer = (Player)p;
            TcpClient threadClient = threadPlayer.Tcp();
            NetworkStream threadStream = threadPlayer.Stream();
            byte[] buffer = new byte[255];

            byte[] start = Encoding.GetEncoding("UTF-8").GetBytes("START\n");
            byte[] abnormal = Encoding.GetEncoding("UTF-8").GetBytes("#ABNORMAL\n");
            byte[] gameover = Encoding.GetEncoding("UTF-8").GetBytes("#GAME_OVER\n");
            byte[] illegal = Encoding.GetEncoding("UTF-8").GetBytes("#ILLEGAL\n");
            byte[] win = Encoding.GetEncoding("UTF-8").GetBytes("#WIN\n");
            byte[] lose = Encoding.GetEncoding("UTF-8").GetBytes("#LOSE\n");
            byte[] draw = Encoding.GetEncoding("UTF-8").GetBytes("#DRAW\n");


            while(true)
            {
                // wait for data to come in
                try
                {
                    bytesRead = threadStream.Read(buffer, 0, 255);
                    string bufferStr = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine(bufferStr);

                    if (!isready && bufferStr.StartsWith("AGREE")) {
                        threadPlayer.Stream().Write(start, 0, start.Length);
                        isready = true;
                        continue;
                    }

                    if (   threadClient.Client.Connected
                        && threadClient.Client.Poll(1000, SelectMode.SelectRead) 
                        && (threadClient.Client.Available == 0))
                        break;

                    if (threadPlayer.Opponent() == null)
                        continue;

                    // if client clicks cancel
                    if (bytesRead == 0)
                    {
                        if (threadPlayer.Opponent() != null)
                        {
                            if (threadPlayer.Opponent().Tcp().Connected == true)
                            {
                                // tell opponent game ended
                                // threadPlayer.Opponent().Stream().Write(abnormal, 0, abnormal.Length);
                            }
                        }
                        break;
                    }
                    // resign
                    else if (bufferStr.StartsWith("resign"))
                    {
                        //tell opponent game ended
                        threadPlayer.Opponent().Stream().Write(gameover, 0, gameover.Length);
                        threadPlayer.Opponent().Stream().Write(win, 0, win.Length);
                        break;
                    }
                    else if (bufferStr.StartsWith("+") || (bufferStr.StartsWith("-"))) 
                    {
                        // 処理中に対局が無くなると死ぬ
                        int gameIdx = -1;
                        for (int i = 0; i < games.Count; i++)
                            if (games[i].Id() == threadPlayer.GameId())
                                gameIdx = i;

                        if (gameIdx == -1)
                        {
                            Console.WriteLine("ERROR");
                            break;
                        }  

                        Move move = new Move(bufferStr.Substring(1,6));

                        // illegal move
                        if (   (bufferStr.StartsWith("+") && threadPlayer.MyColor() != Color.BLACK)
                            || (bufferStr.StartsWith("-") && threadPlayer.MyColor() != Color.WHITE)
                            || !games[gameIdx].pos.IsLegalMove(move))
                        {
                            threadPlayer.Stream().Write(illegal, 0, illegal.Length);
                            threadPlayer.Stream().Write(lose, 0, lose.Length);
                            //tell opponent game ended
                            threadPlayer.Opponent().Stream().Write(gameover, 0, gameover.Length);
                            threadPlayer.Opponent().Stream().Write(win, 0, win.Length);
                            break;
                        }

                        // OKを送る
                        string mStr = (move.Promote() ? bufferStr.Substring(0, 5) : bufferStr.Substring(0, 4)) + ",OK\n";
                        byte[] ok = Encoding.GetEncoding("UTF-8").GetBytes(mStr);
                        threadPlayer.Stream().Write(ok, 0, ok.Length);
                        threadPlayer.Opponent().Stream().Write(ok, 0, ok.Length);
                        
                        // do move
                        if (games[gameIdx].pos.DoMove(move)) {
                            threadPlayer.Stream().Write(gameover, 0, gameover.Length);
                            threadPlayer.Stream().Write(win, 0, win.Length);
                            //tell opponent game ended
                            threadPlayer.Opponent().Stream().Write(gameover, 0, gameover.Length);
                            threadPlayer.Opponent().Stream().Write(lose, 0, win.Length);
                            break;
                        }
                    }
                    // chat
                    else
                    {
                        threadPlayer.Opponent().Stream().Write(buffer, 0, bytesRead);
                    }
                }
                // Client closed
                catch (System.IO.IOException)
                {
                    // tell opponent game ended
                    /*
                    if (threadPlayer.Opponent() != null)
                    {
                        threadPlayer.Opponent().Stream().Write(abnormal, 0, abnormal.Length);
                    }
                    */
                    break;
                }
            }
            Console.WriteLine("Player #" + threadPlayer.PlayerId() + " left");
            if (threadPlayer.Opponent() != null)
            {
                threadPlayer.Opponent().Stream().Close();
                threadPlayer.Opponent().Tcp().Close();
                players.Remove(threadPlayer.Opponent());
            }
            threadStream.Close();
            threadClient.Close();
            players.Remove(threadPlayer);

            lock(waitingPlayerLock)
            {
                if (waitingPlayer != null)
                {
                    if (waitingPlayer.PlayerId() == threadPlayer.PlayerId())
                    {
                        waitingPlayer = null;
                    }
                }
            }
            
            // find game from gameID and see if can remove from list
            lock (gamesLock)
            {
                for (int i = 0; i < games.Count; i++)
                {
                    if (games[i].Id() == threadPlayer.GameId())
                    {
                        if (threadPlayer.Opponent() != null)
                        {
                            if (games[i].Player1().Tcp().Connected == false && games[i].Player2().Tcp().Connected == false)
                            {
                                games.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}