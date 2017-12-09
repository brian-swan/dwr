using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using dwr;
using System.Linq;
using System.IO;

namespace UnitTests
{
	[TestClass]
	public class ReportTests
	{
		[TestMethod]
		public void TestStatsAreCalculatedCorrectly()
		{
			var query = new MockDorothyReportQuery();
			var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 6, 2);
			var statsList = report.Generate().Result;
			var resultsList = DorothyReportHelpers.SummarizeData(statsList).ToList();
			Assert.AreEqual(3, resultsList.Count());
			var env2 = resultsList.Where(x => x.EnvironmentName == "Env_2").SingleOrDefault();
			Assert.AreEqual(0.375, env2.Apdex);
		}

		[TestMethod]
		public void TestFailedQueriesAreCaught()
		{
			var query = new MockDorothyReportQuery();
			var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 6, 1);
			var statsList = report.Generate().Result;
			var resultsList = DorothyReportHelpers.SummarizeData(statsList).ToList();
			Assert.AreEqual(4, resultsList.Count());
			Assert.IsTrue(resultsList.Any(x => x.EnvironmentName == "FailedQuery"));
		}

		[TestMethod]
		public void TestExcelFileIsCreated()
		{
			var endTime = "2017-12-06T06:00:00Z";
			var eTime = DateTimeOffset.Parse(endTime).UtcDateTime;
			var query = new MockDorothyReportQuery();
			var report = new DorothyReport(query, endTime , 6, 2);
			var statsList = report.Generate().Result;
			DorothyReportHelpers.WriteReportToExcel(statsList, eTime.ToString("yyyyMMddTHHmmssZ"));
			var fileName = string.Format("DorothyReport-{0}.xlsx", eTime.ToString("yyyyMMddTHHmmssZ"));
			var filePath = Directory.GetCurrentDirectory() + "\\" + fileName;
			Assert.IsTrue(File.Exists(filePath));
			File.Delete(filePath);
		}
	}
}
