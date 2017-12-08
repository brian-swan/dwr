using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dwr
{
	public class MockDorothyReportQueryTemp : IQuery
	{
		public MockDorothyReportQueryTemp()
		{

		}

		public async Task<List<EnvironmentStats>> GetResult(string startTime, string endTime, int queryIntervalInHours)
		{
			var statsList = new List<EnvironmentStats>();
			var fakeStats = 0;
			switch (startTime)
			{
				case "2017-12-06T00:00:00.000Z":
					fakeStats = 1;
					break;
				case "2017-12-06T02:00:00.000Z":
					fakeStats = 2;
					break;
				case "2017-12-06T04:00:00.000Z":
					fakeStats = 3;
					break;
				default:
					fakeStats = 0;
					break;
			}
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(string.Format("Querying interval {0} to {1}...", startTime, endTime));
			var stats = new EnvironmentStats();
			stats.EnvironmentName = fakeStats == 0 ? "FailedQuery" : "Env_" + fakeStats.ToString();
			stats.Timestamp = Convert.ToDateTime(startTime);
			stats.FastRequests = fakeStats;
			stats.SlowRequests = fakeStats;
			stats.FailedRequests = fakeStats;
			stats.TimedoutRequests = fakeStats;
			stats.UnknownRequests = fakeStats;
			statsList.Add(stats);
			return statsList;
		}
	}
}
