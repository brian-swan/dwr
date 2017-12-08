﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kusto.Data;
using System.Diagnostics;
using System.Data;
using Kusto.Data.Exceptions;

namespace dwr
{
	class Program
	{
		static void Main(string[] args)
		{
			// Get rid of these test cases when I can get VS to discover my test
			//var query = new MockDorothyReportQueryTemp();
			////var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 6, 2); // Test case: all should succeed
			//var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 6, 1); // Test case: some failed queries

			var query = new DorothyReportQuery();
			var report = new DorothyReport(query, "2017-12-06T06:00:00Z", 1, 1);

			var statsList = report.Generate();
			PrintReportToConsole(statsList.Result);

			Console.ReadLine();
		}

		public static void PrintReportToConsole(List<EnvironmentStats> statsList)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Report results...\n");

			var results = (from stats in statsList
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
		}
	}
}
