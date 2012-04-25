using Connect4Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Sockets;

namespace Connect4Tester
{
    
    /// <summary>
    ///This is a test class for Connect4ServiceTest and is intended
    ///to contain all Connect4ServiceTest Unit Tests
    ///</summary>
    [TestClass()]
    public class Connect4ServiceTest
    {
        private static System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
         
        private class SocketTestHelper : TcpClient
        {

            public SocketTestHelper(string hostName, int port)
                : base(hostName, port)
            {
            }

            public void Send(string msg)
            {
                byte[] buffer = encoder.GetBytes(msg);
                int BytesToSend = buffer.Length;

                while (BytesToSend != 0)
                {
                    BytesToSend -= Client.Send(buffer, buffer.Length - BytesToSend, BytesToSend, SocketFlags.None);
                }
            }

            /// <summary>
            /// Receives bytes from the server until the desired number of \n characters
            /// are received. Returns the string composed of the first numLines lines that
            /// were received.
            /// </summary>
            /// <param name="numLines"></param>
            /// <param name="timeout"></param>
            /// <returns></returns>
            public string Recieve(int numLines, int timeout)
            {
                this.Client.ReceiveTimeout = timeout;
                string message = "";
                Byte[] buffer = new byte[1024];
                int BytesRead = 0;
                int indexLastLineBreak = 0;
                int breaksFound = 0;

                try
                {
                    while (breaksFound != numLines)
                    {
                        BytesRead = Client.Receive(buffer);
                        message += encoder.GetString(buffer, 0, BytesRead);
                        for (int i = 0; i < message.Length; i++)
                        {
                            if (message[i] == '\n')
                            {
                                breaksFound++;
                                if (breaksFound == numLines)
                                {
                                    indexLastLineBreak = i;
                                    break;
                                }
                            }
                        }
                    }

                    message = message.Substring(0, indexLastLineBreak + 1);
                }
                catch (SocketException)
                {
                }

                return message;
            }

            /// <summary>
            /// Returns all bytes currently waiting to be received from the server
            /// in the form of a string.
            /// </summary>
            /// <param name="timeout"></param>
            /// <returns></returns>
            public string ReceiveCurrent(int timeout)
            {
                string message = "";
                this.Client.ReceiveTimeout = timeout;
                Byte[] buffer = new byte[1024];
                int bytesRead = 0;

                bytesRead = Client.Receive(buffer);
                message += encoder.GetString(buffer, 0, bytesRead);

                return message;
            }
        }


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
        ///Testing creating a game when two players join
        ///</summary>
        [TestMethod()]
        public void CreateGameTest()
        {
            Connect4Service target = new Connect4Service(6001, 30, Connect4Service.WhoGoesFirst.random);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6001);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6001);

            client1.Send("@name\n#Mr.Test\n");
            client2.Send("@name\n#Mrs.Test\n");

            string message1 = client1.Recieve(3, 2000);
            string message2 = client2.Recieve(3, 2000);

            Assert.IsTrue("@play\r\n#Mrs.Test\r\n#30\r\n" == message1 || "@play\n#Mrs.Test\n#30\n" == message1);
            Assert.IsTrue("@play\r\n#Mr.Test\r\n#30\r\n" == message2 || "@play\n#Mr.Test\n#30\n" == message2);
        }

        /// <summary>
        /// Testing the server properly detects win: condition 4 in a row down
        ///</summary>
        [TestMethod()]
        public void WinGameTest1()
        {
            Connect4Service target = new Connect4Service(6002, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6002);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6002);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");
            
            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#1\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 4 in a row left
        ///</summary>
        [TestMethod()]
        public void WinGameTest2()
        {
            Connect4Service target = new Connect4Service(6003, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6003);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6003);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#4\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 4 in a row right
        ///</summary>
        [TestMethod()]
        public void WinGameTest3()
        {
            Connect4Service target = new Connect4Service(6004, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6004);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6004);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#1\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 1 right, 2 left
        ///</summary>
        [TestMethod()]
        public void WinGameTest4()
        {
            Connect4Service target = new Connect4Service(6005, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6005);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6005);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#2\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 2 right 1 left
        ///</summary>
        [TestMethod()]
        public void WinGameTest5()
        {
            Connect4Service target = new Connect4Service(6006, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6006);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6006);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#3\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 3 up-right
        ///</summary>
        [TestMethod()]
        public void WinGameTest6()
        {
            Connect4Service target = new Connect4Service(6007, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6007);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6007);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#5\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#1\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 2 up-right, 1 down-left
        ///</summary>
        [TestMethod()]
        public void WinGameTest7()
        {
            Connect4Service target = new Connect4Service(6008, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6008);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6008);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#5\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#2\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 1 up-right, 2 down-left
        ///</summary>
        [TestMethod()]
        public void WinGameTest8()
        {
            Connect4Service target = new Connect4Service(6009, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6009);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6009);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#5\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#3\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 3 down-left
        ///</summary>
        [TestMethod()]
        public void WinGameTest9()
        {
            Connect4Service target = new Connect4Service(6010, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6010);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6010);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#5\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#4\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 3 up-left
        ///</summary>
        [TestMethod()]
        public void WinGameTest10()
        {
            Connect4Service target = new Connect4Service(6011, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6011);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6011);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#7\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#4\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 2 up-left, 1 down-right
        ///</summary>
        [TestMethod()]
        public void WinGameTest11()
        {
            Connect4Service target = new Connect4Service(6012, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6012);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6012);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#7\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#3\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 1 up-left, 2 down-right
        ///</summary>
        [TestMethod()]
        public void WinGameTest12()
        {
            Connect4Service target = new Connect4Service(6013, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6013);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6013);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#7\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#2\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server properly detects win: condition 3 down-right
        ///</summary>
        [TestMethod()]
        public void WinGameTest13()
        {
            Connect4Service target = new Connect4Service(6014, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6014);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6014);

            client1.Send("@name\n#Mr.Test\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@name\n#Mrs.Test\n");

            //Confirm the game has started
            client1.Recieve(4, 10000);

            client1.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#2\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#3\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#1\n");
            System.Threading.Thread.Sleep(20);
            client1.Send("@move\n#4\n");
            System.Threading.Thread.Sleep(20);
            client2.Send("@move\n#7\n");
            System.Threading.Thread.Sleep(20);

            //at least mostly clear out the current mass of tick commands
            //client1.ReceiveCurrent(2000);
            //client2.ReceiveCurrent(2000);

            client1.Send("@move\n#1\n");

            System.Threading.Thread.Sleep(20);
            //Receive the remainder
            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@win\n#black\n") || message1.Contains("@win\r\n#black\r\n"));
            Assert.IsTrue(message2.Contains("@win\n#black\n") || message2.Contains("@win\r\n#black\r\n"));
        }

        /// <summary>
        /// Testing the server doesn't crash if a user disconnects before being paired into a game.
        ///</summary>
        [TestMethod()]
        public void DisconnectTest1()
        {
            Connect4Service target = new Connect4Service(6015, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6015);

            client1.Close();

            try
            {
                SocketTestHelper client2 = new SocketTestHelper("localhost", 6015);
            }
            catch (Exception e)
            {
                Assert.Fail("Unable to connect to the server:" + e.Message);
            }
        }

        /// <summary>
        /// Testing the server doesn't crash if a user disconnects after being paired into a game
        ///</summary>
        [TestMethod()]
        public void DisconnectTest2()
        {
            Connect4Service target = new Connect4Service(6016, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6016);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6016);

            //Give the server a bit of time to put the clients into a game
            System.Threading.Thread.Sleep(10);

            client1.Close();

            try
            {
                SocketTestHelper client3 = new SocketTestHelper("localhost", 6016);
            }
            catch (Exception e)
            {
                Assert.Fail("Unable to connect to the server:" + e.Message);
            }
        }

        /// <summary>
        /// Testing the server can handle having several people join at the same time,
        /// and pairs them all into games in the order they were named.
        ///</summary>
        [TestMethod()]
        public void MultipleConnections()
        {
            Connect4Service target = new Connect4Service(6017, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client3 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client4 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client5 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client6 = new SocketTestHelper("localhost", 6017);
            SocketTestHelper client7 = new SocketTestHelper("localhost", 6017);

            client1.Send("@name\n#1\n");
            System.Threading.Thread.Sleep(25);
            client2.Send("@name\n#2\n");
            System.Threading.Thread.Sleep(25);
            client3.Send("@name\n#3\n");
            System.Threading.Thread.Sleep(25);
            client4.Send("@name\n#4\n");
            System.Threading.Thread.Sleep(25);
            client5.Send("@name\n#5\n");
            System.Threading.Thread.Sleep(25);
            client6.Send("@name\n#6\n");
            System.Threading.Thread.Sleep(25);
            client7.Send("@name\n#7\n");
            System.Threading.Thread.Sleep(25);

            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);
            string message3 = client3.ReceiveCurrent(2000);
            string message4 = client4.ReceiveCurrent(2000);
            string message5 = client5.ReceiveCurrent(2000);
            string message6 = client6.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@play") && message1.Contains("#2"));
            Assert.IsTrue(message2.Contains("@play") && message2.Contains("#1"));
            Assert.IsTrue(message3.Contains("@play") && message3.Contains("#4"));
            Assert.IsTrue(message4.Contains("@play") && message4.Contains("#3"));
            Assert.IsTrue(message5.Contains("@play") && message5.Contains("#6"));
            Assert.IsTrue(message6.Contains("@play") && message6.Contains("#5"));

            try
            {
                string message7 = client7.ReceiveCurrent(100);
                Assert.Fail("client7 should not have been paired into a game.");
            }
            catch (Exception)
            {
                //Exception expected - pass through
            }
        }

        /// <summary>
        /// Testing the server can handle having messages sent in bits and pieces - ie
        /// like from Telnet.
        ///</summary>
        [TestMethod()]
        public void SplitMessages()
        {
            Connect4Service target = new Connect4Service(6018, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6018);
            SocketTestHelper client2 = new SocketTestHelper("localhost", 6018);

            client1.Send("@na");
            System.Threading.Thread.Sleep(5);
            client1.Send("me\n");
            System.Threading.Thread.Sleep(5);
            client1.Send("#1");
            System.Threading.Thread.Sleep(5);
            client1.Send("\n");
            System.Threading.Thread.Sleep(5);

            client2.Send("@");
            System.Threading.Thread.Sleep(5);
            client2.Send("n");
            System.Threading.Thread.Sleep(5);
            client2.Send("a");
            System.Threading.Thread.Sleep(5);
            client2.Send("m");
            System.Threading.Thread.Sleep(5);
            client2.Send("e");
            System.Threading.Thread.Sleep(5);
            client2.Send("\r");
            System.Threading.Thread.Sleep(5);
            client2.Send("\n");
            System.Threading.Thread.Sleep(5);
            client2.Send("#");
            System.Threading.Thread.Sleep(5);
            client2.Send("2");
            System.Threading.Thread.Sleep(5);
            client2.Send("\r");
            System.Threading.Thread.Sleep(5);
            client2.Send("\n");
            System.Threading.Thread.Sleep(5);

            string message1 = client1.ReceiveCurrent(2000);
            string message2 = client2.ReceiveCurrent(2000);

            Assert.IsTrue(message1.Contains("@play"));
            Assert.IsTrue(message2.Contains("@play"));
        }

        /// <summary>
        /// Testing the server will send @ignoring commands when given appropriately bad input.
        ///</summary>
        [TestMethod()]
        public void IgnoringMessages()
        {
            Connect4Service target = new Connect4Service(6019, 30, Connect4Service.WhoGoesFirst.first);

            SocketTestHelper client1 = new SocketTestHelper("localhost", 6019);

            client1.Send("#name\n@aric\n@name\n");
            System.Threading.Thread.Sleep(50);
            string test = client1.ReceiveCurrent(2000);

            Assert.IsTrue("@ignoring\n#aric\n" == test || "@ignoring\r\n#aric\r\n" == test);
        }
    }
}
