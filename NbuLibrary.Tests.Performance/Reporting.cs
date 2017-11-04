using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuLibrary.Core.Reporting;
using System.IO;

namespace NbuLibrary.Tests.Performance
{
    [TestClass]
    public class Reporting
    {
        [TestMethod]
        public void Test_Reporting_GetReports()
        {
            using (var rs = new ReportingServer())
            {
                var folders = rs.GetFolders().ToList();
                Assert.AreEqual(1, folders.Count());

                var serviceFolders = rs.GetFolders(folders.Single().Path);
                Assert.AreEqual(2, serviceFolders.Count());

                var askTheLibFolder = serviceFolders.Where(x => x.Name.ToLower().Contains("askthelib")).SingleOrDefault();
                Assert.IsNotNull(askTheLibFolder);
                var reports = rs.GetReports(askTheLibFolder.Path);
                Assert.AreEqual(1, reports.Count());
            }
        }

        [TestMethod]
        public void Test_Reporting_CreateFolder()
        {
            using (var rs = new ReportingServer())
            {
                var folders = rs.GetFolders().ToList();
                Assert.AreEqual(1, folders.Count());

                var exFolders = rs.GetFolders(folders.Single().Path).ToList();

                string toCreate = "test";
                bool ok = rs.CreateFolder(toCreate, folders[0].Path);
                Assert.IsTrue(ok);
                var newFolders = rs.GetFolders(folders.Single().Path).ToList();
                Assert.AreEqual(exFolders.Count + 1, newFolders.Count);
                Assert.IsNotNull(newFolders.SingleOrDefault(x => x.Name == toCreate));
            }
        }

        [TestMethod]
        public void Test_Reporting_CreateReport()
        {
            using (var rs = new ReportingServer())
            {
                var folders = rs.GetFolders().ToList();
                Assert.AreEqual(1, folders.Count());

                var exFolders = rs.GetFolders(folders.Single().Path).ToList();

                string toCreate = "test";
                bool ok = rs.CreateFolder(toCreate, folders[0].Path);
                Assert.IsTrue(ok);
                var newFolders = rs.GetFolders(folders.Single().Path).ToList();
                var newFolder = newFolders.SingleOrDefault(x => x.Name == toCreate);

                string rdlPath = @"D:\Users\kiko\Documents\Visual Studio 2008\Projects\NbuLibReport1\NbuLibReport1\Report3.rdl";
                ok = rs.CreateReport("testreport1", File.ReadAllBytes(rdlPath), newFolder.Path);
                Assert.IsTrue(ok);
                var reports = rs.GetReports(newFolder.Path).ToList();
                Assert.AreEqual(1, reports.Count);
                Assert.AreEqual("testreport1", reports[0].Name);
            }
        }
    }
}
