using Connect4Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Connect4Tester
{
 
    /// <summary>
    ///This is a test class for Connect4ClientGameTest and is intended
    ///to contain all Connect4ClientGameTest Unit Tests
    ///</summary>
    [TestClass()]
    public class Connect4ClientGameTest
    {

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        /// A test for Connect4ClientGame Constructor/connection
        /// /summary>
        [TestMethod()]
        public void Connect4ClientGameConstructorTest()
        {
            AutoResetEvent ar = new AutoResetEvent(false);
            bool connected = false;
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5001, 30, Connect4Server.Connect4Service.WhoGoesFirst.random);
            Connect4ClientGame target = new Connect4ClientGame("Mr. test");
            target.ConnectionEstablished += (success) =>
            {
                if (success)
                {
                    connected = true;
                    ar.Set();
                }
            };
            target.StartConnection("localhost", 5001);
            ar.WaitOne(10000);
            Assert.IsTrue(connected);
        }

        /// <summary>
        /// A test for CancelConnection
        /// </summary>
        [TestMethod()]
        public void CancelConnectionTest()
        {
            AutoResetEvent ar = new AutoResetEvent(false);
            bool cancelled = false;
            Connect4ClientGame target = new Connect4ClientGame("Mr. test");
            target.ConnectionEstablished += (success) =>
            {
                if (!success)
                {
                    cancelled = true;
                    ar.Set();
                }
            };
            target.StartConnection("google.com", 4000);
            target.CancelConnection();
            ar.WaitOne(10000);
            Assert.IsTrue(cancelled);
        }

        /// <summary>
        /// A test for GameStarted
        /// </summary>
        [TestMethod()]
        public void GameStartedTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            bool started1 = false;
            bool started2 = false;
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5002, 30, Connect4Server.Connect4Service.WhoGoesFirst.random);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.GameStarted += (a, b, c) =>
            {
                started1 = true;
                ar1.Set();
            };
            target2.GameStarted += (a, b, c) =>
            {
                started2 = true;
                ar2.Set();
            };
            target1.StartConnection("localhost", 5002);
            target2.StartConnection("localhost", 5002);
            ar1.WaitOne(10000);
            ar2.WaitOne(10000);
            Assert.IsTrue(started1);
            Assert.IsTrue(started2);
        }

        /// <summary>
        /// A test for MakeMove
        /// </summary>
        [TestMethod()]
        public void MakeMoveTest()
        {
            AutoResetEvent ar = new AutoResetEvent(false);
            bool moveMade = false;
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5003, 30, Connect4Server.Connect4Service.WhoGoesFirst.random);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.MoveMade += (row, pos, you) =>
            {
                moveMade = true;
                ar.Set();
            };
            target2.MoveMade += (row, pos, you) =>
            {
                moveMade = true;
                ar.Set();
            };
            target1.StartConnection("localhost", 5003);
            target2.StartConnection("localhost", 5003);
            Thread.Sleep(1000);
            target1.MakeMove(4);
            target2.MakeMove(3);
            ar.WaitOne();
            Assert.IsTrue(moveMade);
        }

        /// <summary>
        /// A test for Resign
        /// </summary>
        [TestMethod()]
        public void ResignTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            bool resignHappened1 = false;
            bool resignHappened2 = false;
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5004, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (s) =>
            {
                if (s == DisconnectReason.youResigned)
                {
                    resignHappened1 = true;
                    ar1.Set();
                }
            };
            target2.Disconnected += (s) =>
            {
                if (s == DisconnectReason.opponentResigned)
                {
                    resignHappened2 = true;
                    ar2.Set();
                }
            };
            target1.StartConnection("localhost", 5004);
            target2.StartConnection("localhost", 5004);
            Thread.Sleep(1000);
            target1.Resign();
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(resignHappened1);
            Assert.IsTrue(resignHappened2);
        }

        [TestMethod()]
        public void ResignTest1()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            bool resignHappened1 = false;
            bool resignHappened2 = false;
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5011, 30, Connect4Server.Connect4Service.WhoGoesFirst.second);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (s) =>
            {
                if (s == DisconnectReason.youResigned)
                {
                    resignHappened1 = true;
                    ar1.Set();
                }
            };
            target2.Disconnected += (s) =>
            {
                if (s == DisconnectReason.opponentResigned)
                {
                    resignHappened2 = true;
                    ar2.Set();
                }
            };
            target1.StartConnection("localhost", 5011);
            target2.StartConnection("localhost", 5011);
            Thread.Sleep(1000);
            target1.Resign();
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(resignHappened1);
            Assert.IsTrue(resignHappened2);
        }

        /// <summary>
        ///A test for GameBoard
        ///</summary>
        [TestMethod()]
        public void GameBoardTest()
        {
            AutoResetEvent ar = new AutoResetEvent(false);
            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5005, 30, Connect4Server.Connect4Service.WhoGoesFirst.random);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.MoveMade += (row, pos, you) =>
            {
                ar.Set();
            };
            target2.MoveMade += (row, pos, you) =>
            {
                ar.Set();
            };
            target1.StartConnection("localhost", 5005);
            target2.StartConnection("localhost", 5005);
            Thread.Sleep(1000);
            target1.MakeMove(4);
            target2.MakeMove(4);
            ar.WaitOne();
            BoardSquareState state1 = target1.GameBoard.ElementAt(0).ElementAt(3);
            BoardSquareState state2 = target2.GameBoard.ElementAt(0).ElementAt(3);
            bool works = (state1 == state2) && (state1 != BoardSquareState.empty);
            Assert.IsTrue(works);
        }

        /// <summary>
        /// A test to make sure an exception is thrown if the name contains
        /// a newline.
        /// </summary>
        [TestMethod()]
        public void InvalidNameTest()
        {
            AutoResetEvent ar = new AutoResetEvent(false);
            try
            {
                Connect4ClientGame target1 = new Connect4ClientGame("\n");
                Assert.Fail("That should've thrown an exception.");
            }
            catch (InvalidOperationException)
            {

            }
        }

        /// <summary>
        /// Makes sure Win is called at the right time.
        /// </summary>
        [TestMethod()]
        public void WinTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            AutoResetEvent turn1 = new AutoResetEvent(false);
            AutoResetEvent turn2 = new AutoResetEvent(false);

            bool won1 = false;
            bool won2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5006, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youWon)
                {
                    won1 = true;
                    ar1.Set();
                }
            };
            target1.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.opponentWon)
                {
                    won2 = true;
                    ar2.Set();
                }
            };
            target2.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn2.Set();
                }
            };
            target1.StartConnection("localhost", 5006);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5006);
            Thread.Sleep(1000);
            target1.MakeMove(1);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(2);
            turn2.WaitOne();
            target2.MakeMove(2);
            turn1.WaitOne();
            target1.MakeMove(3);
            turn2.WaitOne();
            target2.MakeMove(3);
            turn1.WaitOne();
            target1.MakeMove(4);
            turn2.WaitOne();
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(won1);
            Assert.IsTrue(won2);
        }

        [TestMethod()]
        public void WinTest1()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            AutoResetEvent turn1 = new AutoResetEvent(false);
            AutoResetEvent turn2 = new AutoResetEvent(false);

            bool won1 = false;
            bool won2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5007, 30, Connect4Server.Connect4Service.WhoGoesFirst.second);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.opponentWon)
                {
                    won1 = true;
                    ar1.Set();
                }
            };
            target1.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youWon)
                {
                    won2 = true;
                    ar2.Set();
                }
            };
            target2.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn2.Set();
                }
            };
            target1.StartConnection("localhost", 5007);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5007);
            Thread.Sleep(1000);
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(1);
            turn2.WaitOne();
            target2.MakeMove(2);
            turn1.WaitOne();
            target1.MakeMove(2);
            turn2.WaitOne();
            target2.MakeMove(3);
            turn1.WaitOne();
            target1.MakeMove(3);
            turn2.WaitOne();
            target2.MakeMove(4);
            turn1.WaitOne();
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(won1);
            Assert.IsTrue(won2);
        }

        [TestMethod()]
        public void WinTest2()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            AutoResetEvent turn1 = new AutoResetEvent(false);
            AutoResetEvent turn2 = new AutoResetEvent(false);

            bool won1 = false;
            bool won2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5014, 30, Connect4Server.Connect4Service.WhoGoesFirst.second);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youWon)
                {
                    won1 = true;
                    ar1.Set();
                }
            };
            target1.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.opponentWon)
                {
                    won2 = true;
                    ar2.Set();
                }
            };
            target2.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn2.Set();
                }
            };

            target1.StartConnection("localhost", 5014);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5014);
            Thread.Sleep(1000);
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(2);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(2);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(2);
            turn2.WaitOne();
            target2.MakeMove(3);
            turn1.WaitOne();
            target1.MakeMove(2);
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(won1);
            Assert.IsTrue(won2);
        }

        [TestMethod()]
        public void ServerDisconnectTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);

            bool disconnected1 = false;
            bool disconnected2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5008, 30, Connect4Server.Connect4Service.WhoGoesFirst.second);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.serverDisconnected)
                {
                    disconnected1 = true;
                    ar1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.serverDisconnected)
                {
                    disconnected2 = true;
                    ar2.Set();
                }
            };

            target1.StartConnection("localhost", 5008);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5008);
            Thread.Sleep(1000);
            server.Shutdown();
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(disconnected1);
            Assert.IsTrue(disconnected2);
        }

        [TestMethod()]
        public void TimeOutTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);

            bool disconnected1 = false;
            bool disconnected2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5009, 3, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youOutOfTime)
                {
                    disconnected1 = true;
                    ar1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.opponentOutOfTime)
                {
                    disconnected2 = true;
                    ar2.Set();
                }
            };

            target1.StartConnection("localhost", 5009);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5009);
            Thread.Sleep(1000);
            Thread.Sleep(5000);
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(disconnected1);
            Assert.IsTrue(disconnected2);
        }

        [TestMethod()]
        public void TimeOutTest1()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);

            bool disconnected1 = false;
            bool disconnected2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5010, 3, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.opponentOutOfTime)
                {
                    disconnected1 = true;
                    ar1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youOutOfTime)
                {
                    disconnected2 = true;
                    ar2.Set();
                }
            };

            target1.StartConnection("localhost", 5010);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5010);
            Thread.Sleep(1000);
            target1.MakeMove(1);
            Thread.Sleep(6000);
            ar1.WaitOne();
            ar2.WaitOne();
            Assert.IsTrue(disconnected1);
            Assert.IsTrue(disconnected2);
        }

        [TestMethod()]
        public void FullTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent turn1 = new AutoResetEvent(false);
            AutoResetEvent turn2 = new AutoResetEvent(false);

            bool badMove = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5012, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.IllegalMove += () =>
            {
                badMove = true;
                ar1.Set();
            };
            target1.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn1.Set();
                }
            };

            target2.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn2.Set();
                }
            };
            target1.StartConnection("localhost", 5012);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5012);
            Thread.Sleep(1000);

            target1.MakeMove(1);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(1);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(1);
            turn2.WaitOne();
            target2.MakeMove(1);
            turn1.WaitOne();
            target1.MakeMove(1);

            ar1.WaitOne();
            Assert.IsTrue(badMove);
        }

        [TestMethod()]
        public void DrawTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);
            AutoResetEvent ar2 = new AutoResetEvent(false);
            AutoResetEvent turn1 = new AutoResetEvent(false);
            AutoResetEvent turn2 = new AutoResetEvent(false);

            bool draw1 = false;
            bool draw2 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5013, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            Connect4ClientGame target2 = new Connect4ClientGame("Mrs. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.draw)
                {
                    draw1 = true;
                    ar1.Set();
                }
            };
            target1.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn1.Set();
                }
            };

            target2.Disconnected += (r) =>
            {
                if (r == DisconnectReason.draw)
                {
                    draw2 = true;
                    ar2.Set();
                }
            };
            target2.MoveMade += (row, pos, you) =>
            {
                if (!you)
                {
                    turn2.Set();
                }
            };

            target1.StartConnection("localhost", 5013);
            Thread.Sleep(500);
            target2.StartConnection("localhost", 5013);
            Thread.Sleep(1000);

            for (int i = 0; i < 3; i++)
            {
                target1.MakeMove(1);
                turn2.WaitOne();
                target2.MakeMove(3);
                turn1.WaitOne();
                target1.MakeMove(2);
                turn2.WaitOne();
                target2.MakeMove(4);
                turn1.WaitOne();
                target1.MakeMove(5);
                turn2.WaitOne();
                target2.MakeMove(7);
                turn1.WaitOne();
                target1.MakeMove(6);
                turn2.WaitOne();

                target2.MakeMove(1);
                turn1.WaitOne();
                target1.MakeMove(3);
                turn2.WaitOne();
                target2.MakeMove(2);
                turn1.WaitOne();
                target1.MakeMove(4);
                turn2.WaitOne();
                target2.MakeMove(5);
                turn1.WaitOne();
                target1.MakeMove(7);
                turn2.WaitOne();
                target2.MakeMove(6);
                turn1.WaitOne();
            }

            ar1.WaitOne();
            ar2.WaitOne();

            Assert.IsTrue(draw1);
            Assert.IsTrue(draw2);
        }

        [TestMethod()]
        public void CancelBeforeGameTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);

            bool youLeft1 = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5015, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.youLeft)
                {
                    youLeft1 = true;
                    ar1.Set();
                }
            };

            target1.StartConnection("localhost", 5015);
            Thread.Sleep(500);
            Thread.Sleep(1000);
            target1.CancelConnection();
            ar1.WaitOne();

            Assert.IsTrue(youLeft1);
        }

        [TestMethod()]
        public void ServerDiesAfterConnectBeforeReceiveTest()
        {
            AutoResetEvent ar1 = new AutoResetEvent(false);

            bool serverDied = false;

            Connect4Server.Connect4Service server = new Connect4Server.Connect4Service(5016, 30, Connect4Server.Connect4Service.WhoGoesFirst.first);
            Connect4ClientGame target1 = new Connect4ClientGame("Mr. test");
            target1.ConnectionEstablished += (r) =>
            {
                if (r)
                {
                    server.Shutdown();
                }
            };

            target1.Disconnected += (r) =>
            {
                if (r == DisconnectReason.serverDisconnected)
                {
                    serverDied = true;
                    ar1.Set();
                }
            };

            target1.StartConnection("localhost", 5016);
            Thread.Sleep(500);
            Thread.Sleep(1000);
            target1.CancelConnection();
            ar1.WaitOne();

            Assert.IsTrue(serverDied);
        }

    }

}