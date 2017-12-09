using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace dwr
{
	class DorothyReportQuery : IQuery
	{
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

		public DorothyReportQuery()
		{
			_client = KustoClientFactory.CreateCslQueryProvider(E2elogsConnString);
		}

		// start/end times should be in format "yyyy-MM-ddTHH:mm:ss.fffZ"
		public async Task<List<EnvironmentStats>> GetResult(string startTime, string endTime, int queryIntervalInHours)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(string.Format("Querying interval {0} to {1}...", startTime, endTime));

			var query = String.Format(QueryTemplate, startTime, endTime, queryIntervalInHours.ToString() + "h");
			var maxRetries = 3;
			var retries = 0;
			var querySuccess = false;
			IDataReader reader;
			var statsList = new List<EnvironmentStats>();
			var sw = System.Diagnostics.Stopwatch.StartNew();
			while (retries < maxRetries && !querySuccess)
			{
				try
				{
					using (reader = _client.ExecuteQuery(query))
					{
						while (reader.Read())
						{
							var values = new object[7];
							reader.GetValues(values);
							var stats = new EnvironmentStats();
							stats.EnvironmentName = values[0].ToString();
							stats.Timestamp = Convert.ToDateTime(values[1]);
							stats.FastRequests = Convert.ToInt32(values[2]);
							stats.SlowRequests = Convert.ToInt32(values[3]);
							stats.FailedRequests = Convert.ToInt32(values[4]);
							stats.TimedoutRequests = Convert.ToInt32(values[5]);
							stats.UnknownRequests = Convert.ToInt32(values[6]);
							statsList.Add(stats);
						}
					}
					querySuccess = true;
				}
				catch (KustoServiceTimeoutException)
				{
					retries++;
					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine(string.Format("Query for interval starting at {0} timed out. Retrying...", startTime));
				}
				catch (Exception e)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(string.Format("Query for interval starting at {0} failed with exception {1}", startTime, e.Message));
				}
			}
			sw.Stop();
			if (querySuccess)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(string.Format("Finished query for interval starting at {0} in {1}", startTime,sw.Elapsed.ToString()));
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(string.Format("Query for interval starting at {0} failed after {1}. Report results will be incomplete.", startTime, sw.Elapsed));
				statsList.Add(new EnvironmentStats() { EnvironmentName = "FailedQuery" });
			}
			return statsList;
		}
	}
}
