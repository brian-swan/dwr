using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dwr
{
	class DorothyReport
	{
		private DateTimeOffset _endTime;
		private int _reportPeriodInHours;
		private DateTimeOffset _startTime;
		private int _queryIntervalInHours;
		private ConcurrentBag<EnvironmentStats> _statsList;
		private const string E2elogsConnString = "https://cate2e.kusto.windows.net/e2elogs;Fed=true";
		private ICslQueryProvider _client; 
		private const string QueryTemplate =
			@"set query_datascope='hotcache';
					let startTime = datetime('{0}');
					let endTime = datetime('{1}');
					Interactions
					| where Timestamp > startTime and Timestamp <= endTime
					| where Environment startswith 'dorothy'
					| where Environment != 'dorothy-redis-mssql-windows-koreacentral'
					| where Environment != 'dorothy-test-deployment'
					| extend TesterPlatform = tostring(Blob.Information.Tester.Platform)
					| where TesterPlatform == 'Azure'
					| mvexpand Component = Blob['Interaction']['GetServerHealth']['Components']
					| extend TimeTaken = totimespan(Component.TimeTaken) / 1ms
					| extend TimeBucket = case(
							 (Happiness == 'Perfect' and(TimeTaken < 500)), 0,
							 (Happiness != 'Unacceptable' and(TimeTaken < 10000)), 1,
							 ((Happiness == 'Unacceptable') and(HappinessExplanation contains 'timed')), 2,
							 ((Happiness == 'Unacceptable') and(HappinessExplanation contains 'too long')), 2,
							 ((Happiness == 'Unacceptable') and(HappinessExplanation contains 'reset')), 3,
							 ((Happiness == 'Unacceptable') and(HappinessExplanation contains 'Resource temporarily')), 3,
							 ((Happiness == 'Unacceptable') and(HappinessExplanation contains 'Connection refused')), 3,
							 99)
					| summarize Fast = count(TimeBucket == 0), Slow = count(TimeBucket == 1), Failed = count(TimeBucket == 3), Timeout = count(TimeBucket == 2), Unknown = count(TimeBucket == 99) by Environment, bin(Timestamp, {2})
					| order by Environment asc";

		public DorothyReport(string endTime, int reportPeriodInHours = 7 * 24, int queryIntervalInHours = 12)
		{
			_endTime = DateTimeOffset.Parse(endTime).UtcDateTime;
			_reportPeriodInHours = reportPeriodInHours;
			_startTime = _endTime.AddHours(-_reportPeriodInHours);
			_queryIntervalInHours = queryIntervalInHours;
			_statsList = new ConcurrentBag<EnvironmentStats>();//new List<EnvironmentStats>();
			_client = KustoClientFactory.CreateCslQueryProvider(E2elogsConnString);
		}

		public async Task Generate()
		{
			Console.WriteLine("Generating report...");
			Console.WriteLine(string.Format("Report dates: {0} to {1}", _startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
			Console.WriteLine(string.Format("Query interval: {0} hours\n", _queryIntervalInHours));
			var sw = Stopwatch.StartNew();
			var allQueriesSucceeded = true;
			//for (int i = 0; i < _reportPeriodInHours; i += _queryIntervalInHours)
			//{
			//	var queryStartTime = _startTime.AddHours(i);
			//	var queryEndTime = queryStartTime.AddHours(_queryIntervalInHours);
			//	Console.ForegroundColor = ConsoleColor.White;
			//	Console.WriteLine(string.Format("Querying interval {0} to {1}...", queryStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), queryEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
			//	var query = String.Format(QueryTemplate, queryStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), queryEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), "1h");
			//	if(!GetResult(query))
			//	{
			//		allQueriesSucceeded = false;
			//	}
			//}
			var parallelTaskCount = _reportPeriodInHours/_queryIntervalInHours;
			var tasks = new List<Task>(parallelTaskCount);
			var querySuccess = new ConcurrentBag<bool>();
			for (var t = 0; t < parallelTaskCount; t++)
			{
				var env = "a";
				if(t%3 == 1)
						env = "b";
				if (t % 3 == 2)
					env = "c";
				var task = t; // preventing access to modified closure
				tasks.Add(Task.Run(async () => querySuccess.Add(await GetResult(env))));
			}
			await Task.WhenAll(tasks);
			if(querySuccess.Contains(false))
			{
				allQueriesSucceeded = false;
			}
			Console.WriteLine(allQueriesSucceeded);
			sw.Stop();
			if (allQueriesSucceeded)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(string.Format("All queries finished in {0}\n", sw.Elapsed.ToString()));
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(string.Format("At least one query failed. Results are incomplete."));
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Report results...\n");

			var results = (from stats in _statsList
										 group stats by stats.EnvironmentName into groupedStats
										 select new
										 {
											 Env = groupedStats.Select(e => e.EnvironmentName).First(),
											 FastRequests = groupedStats.Sum(e => e.FastRequests),
											 Slowequests = groupedStats.Sum(e => e.SlowRequests),
											 FailedRequests = groupedStats.Sum(e => e.FailedRequests),
											 TimedoutRequests = groupedStats.Sum(e => e.TimedoutRequests),
											 UnknownRequests = groupedStats.Sum(e => e.UnknownRequests),
											 Apdex = (double)(groupedStats.Sum(e => e.FastRequests) + groupedStats.Sum(e => e.SlowRequests))/(double)(groupedStats.Sum(e => e.FastRequests) + groupedStats.Sum(e => e.SlowRequests) + groupedStats.Sum(e=>e.TimedoutRequests) + groupedStats.Sum(e=>e.FailedRequests)),
										 });


			foreach(var result in results)
			{
				Console.Write(result.Env + ": ") ;
				Console.Write(" Fast: " + result.FastRequests);
				Console.Write(" Slow: " + result.Slowequests);
				Console.Write(" Failed: " + result.FailedRequests);
				Console.Write(" Timedout: " + result.TimedoutRequests);
				Console.Write(" Unknown: " + result.UnknownRequests);
				Console.Write(" Apdex: " + result.Apdex);
				Console.WriteLine();
			}
		}

		private async Task<bool> GetResult(string query)
		{
			//****************for testing*****************
			var stats = new EnvironmentStats();
			var value = 1;
			if (query == "b")
				value = 2;
			if (query == "c")
				value = 3;
			stats.EnvironmentName = "Env_" + query;
			Console.WriteLine(stats.EnvironmentName);
			stats.Timestamp = Convert.ToDateTime("2017-12-01T0" + value + ":00:00.000Z");
			stats.FastRequests = value;
			stats.SlowRequests = value;
			stats.FailedRequests = value;
			stats.TimedoutRequests = value;
			stats.UnknownRequests = value;
			_statsList.Add(stats);
			if (query == "c")
				return false;
			return true;
			//********************************************
			//var maxRetries = 3;
			//var retries = 0;
			//var querySuccess = false;
			//IDataReader reader;
			//var sw = Stopwatch.StartNew();
			//while (retries < maxRetries && !querySuccess)
			//{
			//	try
			//	{
			//		using (reader = _client.ExecuteQuery(query))
			//		{
			//			while (reader.Read())
			//			{
			//				var stats = new EnvironmentStats();
			//				var values = new object[7];
			//				reader.GetValues(values);
			//				stats.EnvironmentName = values[0].ToString();
			//				stats.Timestamp = Convert.ToDateTime(values[1]);
			//				stats.FastRequests = Convert.ToInt32(values[2]);
			//				stats.SlowRequests = Convert.ToInt32(values[3]);
			//				stats.FailedRequests = Convert.ToInt32(values[4]);
			//				stats.TimedoutRequests = Convert.ToInt32(values[5]);
			//				stats.UnknownRequests = Convert.ToInt32(values[6]);
			//				if (!_uniqueEnvironments.Contains(stats.EnvironmentName))
			//				{
			//					_uniqueEnvironments.Add(stats.EnvironmentName);
			//				}
			//				_statsList.Add(stats);
			//			}
			//		}
			//		querySuccess = true;
			//	}
			//	catch (KustoServiceTimeoutException)
			//	{
			//		retries++;
			//		Console.ForegroundColor = ConsoleColor.White;
			//		Console.WriteLine("Request timed out. Retrying...");
			//	}
			//	catch (Exception e)
			//	{
			//		Console.ForegroundColor = ConsoleColor.Red;
			//		Console.WriteLine(string.Format("Query failed with exception {0}", e.Message));
			//	}
			//}
			//sw.Stop();
			//if (querySuccess)
			//{
			//	Console.ForegroundColor = ConsoleColor.Green;
			//	Console.WriteLine(string.Format("Finished query in {0}", sw.Elapsed.ToString()));
			//	return true;
			//}
			//else
			//{
			//	Console.ForegroundColor = ConsoleColor.Red;
			//	Console.WriteLine(string.Format("Query failed after {0}. Report results will be incomplete.", sw.Elapsed));
			//	return false;
			//}
		}
	}
}
