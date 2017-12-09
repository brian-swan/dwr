using System;

namespace dwr
{
	class Program
	{
		static int Main(string[] args)
		{
			string endTime = "";
			int reportPeriodInHours = 0;
			int queryIntervalInHours = 0;

			if(args.Length == 0)
			{
				endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
				reportPeriodInHours = 7 * 24;
				queryIntervalInHours = 12;
			}
			else
			{
				try
				{
					DateTimeOffset dateTime;
					DateTimeOffset.TryParse(args[0], out dateTime);
					endTime = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
					reportPeriodInHours = Int32.Parse(args[1]);
					queryIntervalInHours = Int32.Parse(args[2]);
					if(Math.Floor((double)reportPeriodInHours/queryIntervalInHours) != (double)reportPeriodInHours / queryIntervalInHours)
					{
						throw new ArgumentException("Report period must be multiple of query interval");
					}
				}
				catch(Exception)
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("Something went wrong. If you are supplying parameters...");
					Console.WriteLine("The first should be a string representing the end time for the report in the form 'yyyy-MM-ddTHH:mm:ss.fffZ'.");
					Console.WriteLine("The second should be an int representing the period for the report in hours, e.g. 168, which is one week.");
					Console.WriteLine("The third should be an int representing the interval into which the report period should be broken up, e.g. 12.");
					Console.ForegroundColor = ConsoleColor.White;
					return 1;
				}

			}
			
			var query = new DorothyReportQuery();
			var report = new DorothyReport(query, endTime, reportPeriodInHours, queryIntervalInHours);

			var statsList = report.Generate().Result;
			DorothyReportHelpers.PrintReportToConsole(statsList);

			var eTime = DateTimeOffset.Parse(endTime).UtcDateTime;
			var sTime = eTime.AddHours(-reportPeriodInHours);

			DorothyReportHelpers.WriteReportToExcel(statsList, eTime.ToString("yyyyMMddTHHmmssZ"));

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
			return 0;
		}
	}
}
