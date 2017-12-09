using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dwr
{
	public static class DorothyReportHelpers
	{
		// TODO: format output
		public static void PrintReportToConsole(List<EnvironmentStats> statsList)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Report results...\n");

			Console.WriteLine("Platform\tFast\t\tSlow\t\tFailed\t\tTimedout\tUnknown\t\tApdex\t\t\t\tEnvironment");
			foreach (var result in SummarizeData(statsList))
			{
				
				Console.Write(result.Platform + "\t\t");
				Console.Write( result.FastRequests + "\t\t");
				Console.Write(result.SlowRequests + "\t\t");
				Console.Write(result.FailedRequests + "\t\t");
				Console.Write(result.TimedoutRequests + "\t\t");
				Console.Write(result.UnknownRequests + "\t\t");
				Console.Write(result.Apdex + "\t\t");
				Console.Write(result.EnvironmentName);
				Console.WriteLine();
			}
		}

		public static void WriteReportToExcel(List<EnvironmentStats> statsList, string endTime)
		{
			var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add(string.Format("DorothyReport-{0}", endTime));

			// Headers
			worksheet.Cell("A1").Value = "Environment";
			worksheet.Cell("B1").Value = "Platform";
			worksheet.Cell("C1").Value = "Fast";
			worksheet.Cell("D1").Value = "Slow";
			worksheet.Cell("E1").Value = "Timeout";
			worksheet.Cell("F1").Value = "Failed";
			worksheet.Cell("G1").Value = "Apdex";
			worksheet.Range("A1:G1").Style
				.Font.SetBold()
				.Fill.SetBackgroundColor(XLColor.Gray);


			for (int col = 1; col <= 7; col++)
			{
				var colLetter = Convert.ToChar(col + 64).ToString();
			}

			int row = 2;
			foreach (var result in SummarizeData(statsList))
			{
				// Should use reflection to enumerate properties? Order of properties?
				worksheet.Cell("A" + row).Value = result.EnvironmentName;
				worksheet.Cell("B" + row).Value = result.Platform;
				worksheet.Cell("C" + row).Value = result.FastRequests;
				worksheet.Cell("D" + row).Value = result.SlowRequests;
				worksheet.Cell("E" + row).Value = result.TimedoutRequests;
				worksheet.Cell("F" + row).Value = result.FailedRequests;
				worksheet.Cell("G" + row).Value = result.Apdex;
				row++;

			}
			var fileName = string.Format("DorothyReport-{0}.xlsx", endTime);
			workbook.SaveAs(fileName);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(string.Format("Report saved at {0}", Directory.GetCurrentDirectory() + "\\" + fileName));
		}

		public static IEnumerable<EnvironmentStats> SummarizeData(List<EnvironmentStats> statsList)
		{
			var results = (from stats in statsList
						   group stats by stats.EnvironmentName into groupedStats
						   select new EnvironmentStats
						   {
							   EnvironmentName = groupedStats.Select(e => e.EnvironmentName).First(),
							   FastRequests = groupedStats.Sum(e => e.FastRequests),
							   SlowRequests = groupedStats.Sum(e => e.SlowRequests),
							   FailedRequests = groupedStats.Sum(e => e.FailedRequests),
							   TimedoutRequests = groupedStats.Sum(e => e.TimedoutRequests),
							   UnknownRequests = groupedStats.Sum(e => e.UnknownRequests),
							   Apdex = (double)(groupedStats.Sum(e => e.FastRequests) + .5 * groupedStats.Sum(e => e.SlowRequests)) /
										   (double)(groupedStats.Sum(e => e.FastRequests) +
													groupedStats.Sum(e => e.SlowRequests) +
													groupedStats.Sum(e => e.TimedoutRequests) +
													groupedStats.Sum(e => e.FailedRequests)),
						   });
			return results;
		}
	}
}
