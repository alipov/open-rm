using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRm.Tests.Unit
{
    
    
    /// <summary>
    ///This is a test class for ServerTest and is intended
    ///to contain all ServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ServerTest
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
        ///A test for ProcessReceivedMessageRequest
        ///</summary>
        [TestMethod()]
        [DeploymentItem("OpenRm.Server.Host.exe")]
        public void ProcessReceivedMessageRequestTest()
        {
            // Private Accessor for ProcessReceivedMessageRequest is not found. Please rebuild the containing project or run the Publicize.exe manually.
            Assert.Inconclusive("Private Accessor for ProcessReceivedMessageRequest is not found. Please rebuild t" +
                    "he containing project or run the Publicize.exe manually.");
        }
    }
}
