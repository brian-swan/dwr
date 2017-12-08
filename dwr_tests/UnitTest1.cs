using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using dwr;
using System.Linq;

namespace dwr_tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public async void TestMethod1()
		{
			var query = new MockDorothyReportQuery();
			var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 6, 2);
			var statsList = report.Generate();

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Report results...\n");

			var results = (from stats in statsList.Result
						   group stats by stats.EnvironmentName into groupedStats
						   select new
						   {
							   Env = groupedStats.Select(e => e.EnvironmentName).First(),
							   FastRequests = groupedStats.Sum(e => e.FastRequests),
							   Slowequests = groupedStats.Sum(e => e.SlowRequests),
							   FailedRequests = groupedStats.Sum(e => e.FailedRequests),
							   TimedoutRequests = groupedStats.Sum(e => e.TimedoutRequests),
							   UnknownRequests = groupedStats.Sum(e => e.UnknownRequests),
							   Apdex = (double)(groupedStats.Sum(e => e.FastRequests) + groupedStats.Sum(e => e.SlowRequests)) / (double)(groupedStats.Sum(e => e.FastRequests) + groupedStats.Sum(e => e.SlowRequests) + groupedStats.Sum(e => e.TimedoutRequests) + groupedStats.Sum(e => e.FailedRequests)),
						   });


			foreach (var result in results)
			{
				Console.Write(result.Env + ": ");
				Console.Write(" Fast: " + result.FastRequests);
				Console.Write(" Slow: " + result.Slowequests);
				Console.Write(" Failed: " + result.FailedRequests);
				Console.Write(" Timedout: " + result.TimedoutRequests);
				Console.Write(" Unknown: " + result.UnknownRequests);
				Console.Write(" Apdex: " + result.Apdex);
				Console.WriteLine();
			}
			Console.ReadLine();
			Assert.AreEqual(1, 1);
		}
	}
}
