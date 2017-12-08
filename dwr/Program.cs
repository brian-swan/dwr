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
			var endTime = DateTimeOffset.Parse("2017-12-06T00:00:00Z").UtcDateTime; //Convert.ToDateTime( ();//DateTime.UtcNow();
			var reportPeriodInHours = 3;// 7 * 24; // 1 week
			var startTime = endTime.AddHours(-reportPeriodInHours);
			var queryIntervalInHours = 1; // queries have to be broken into shorter intervals
			var statsList = new List<EnvironmentStats>();
			var uniqueEnvironments = new List<string>();
			string e2elogsConnString = "https://cate2e.kusto.windows.net/e2elogs;Fed=true";
			string queryTemplate = 
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

			var client = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(e2elogsConnString);
			Console.WriteLine("Generating report...");
			Console.WriteLine(string.Format("Report dates: {0} to {1}", startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
			Console.WriteLine(string.Format("Query interval: {0} hours\n", queryIntervalInHours));
			var sw = Stopwatch.StartNew();
			var allQueriesSucceeded = true;
			for (int i = 0; i < reportPeriodInHours; i+=queryIntervalInHours)
			{
				var queryStartTime = startTime.AddHours(i);
				var queryEndTime = queryStartTime.AddHours(queryIntervalInHours);
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(string.Format("Querying interval {0} to {1}...", queryStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), queryEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
				var query = String.Format(queryTemplate, queryStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), queryEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), "1h");
				var maxRetries = 3;
				var retries = 0;
				var querySuccess = false;
				IDataReader reader;
				while (retries < maxRetries && !querySuccess)
				{
					try
					{
						reader = client.ExecuteQuery(query);
						while (reader.Read())
						{
							var stats = new EnvironmentStats();
							var values = new object[7];
							reader.GetValues(values);
							stats.EnvironmentName = values[0].ToString();
							stats.Timestamp = Convert.ToDateTime(values[1]);
							stats.FastRequests = Convert.ToInt32(values[2]);
							stats.SlowRequests = Convert.ToInt32(values[3]);
							stats.FailedRequests = Convert.ToInt32(values[4]);
							stats.TimedoutRequests = Convert.ToInt32(values[5]);
							stats.UnknownRequests = Convert.ToInt32(values[6]);
							if (!uniqueEnvironments.Contains(stats.EnvironmentName))
							{
								uniqueEnvironments.Add(stats.EnvironmentName);
							}
							statsList.Add(stats);
						}
						reader.Dispose();
						querySuccess = true;
					}
					catch (KustoServiceTimeoutException)
					{
						retries++;
						Console.ForegroundColor = ConsoleColor.White;
						Console.WriteLine("Request timed out. Retrying...");
					}
					catch(Exception e)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(string.Format("Query failed with exception {0}", e.Message));
					}
				}
				if (querySuccess)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(string.Format("Finished query in {0}", sw.Elapsed.ToString()));
				}
				else
				{
					allQueriesSucceeded = false;
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(string.Format("Query failed after {0}. Report results will be incomplete.", sw.Elapsed));
				}
			}
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
			foreach (var environment in uniqueEnvironments)
			{
				var fastRequests = statsList.Where(item => item.EnvironmentName == environment).Sum(item => item.FastRequests);
				var slowRequests = statsList.Where(item => item.EnvironmentName == environment).Sum(item => item.SlowRequests);
				var failedRequests = statsList.Where(item => item.EnvironmentName == environment).Sum(item => item.FailedRequests);
				var timedoutRequests = statsList.Where(item => item.EnvironmentName == environment).Sum(item => item.TimedoutRequests);
				var unknownRequests = statsList.Where(item => item.EnvironmentName == environment).Sum(item => item.UnknownRequests);
				var apdex = ((double)fastRequests + .5 * (double)slowRequests) / ((double)(fastRequests + slowRequests + fastRequests + timedoutRequests));
				Console.Write(string.Format("{0}, Fast: {1}, Slow: {2}, Failed: {3}, Timeout: {4}, Unknown: {5}, Apdex: {6}", 
					environment, fastRequests, slowRequests, failedRequests, timedoutRequests, unknownRequests, apdex ));
				Console.WriteLine();
			}
			Console.ReadLine();
		}
	}
}