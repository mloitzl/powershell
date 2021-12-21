using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Management.Automation.Runspaces;

namespace PnP.PowerShell.Tests.Webs
{
    [TestClass]
    public class GetSubWebsTests
    {
        #region Test Setup/CleanUp
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            // This runs on class level once before all tests run
            //using (var ctx = TestCommon.CreateClientContext())
            //{
            //}
        }

        [ClassCleanup]
        public static void Cleanup(TestContext testContext)
        {
            // This runs on class level once
            //using (var ctx = TestCommon.CreateClientContext())
            //{
            //}
        }

        [TestInitialize]
        public void Initialize()
        {
            using (var scope = new PSTestScope())
            {
                // Example
                // scope.ExecuteCommand("cmdlet", new CommandParameter("param1", prop));
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var scope = new PSTestScope())
            {
                try
                {
                    // Do Test Setup - Note, this runs PER test
                }
                catch (Exception)
                {
                    // Describe Exception
                }
            }
        }
        #endregion

        #region Scaffolded Cmdlet Tests
        //TODO: This is a scaffold of the cmdlet - complete the unit test
        //[TestMethod]
        public void GetPnPSubWebsTest()
        {
            using (var scope = new PSTestScope(true))
            {
                // Complete writing cmd parameters

				// From Cmdlet Help: If provided, only the subsite with the provided Id, GUID or the Web instance will be returned
				var identity = "";
				// From Cmdlet Help: If provided, recursion through all subsites and their children will take place to return them as well
				var recurse = "";

                var results = scope.ExecuteCommand("Get-PnPSubWebs",
					new CommandParameter("Identity", identity),
					new CommandParameter("Recurse", recurse));
                
                Assert.IsNotNull(results);
            }
        }
        #endregion
    }
}
            