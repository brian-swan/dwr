using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace dwr
{
	public class DorothyReport
	{
		private DateTimeOffset _endTime;
		private int _reportPeriodInHours;
		private DateTimeOffset _startTime;
		private int _queryIntervalInHours;
		private ConcurrentBag<List<EnvironmentStats>> _statsList;
		private IQuery _query;
		
		public DorothyReport(IQuery query, string endTime, int reportPeriodInHours = 7 * 24, int queryIntervalInHours = 12)
		{
			_endTime = DateTimeOffset.Parse(endTime).UtcDateTime;
			_reportPeriodInHours = reportPeriodInHours;
			_startTime = _endTime.AddHours(-_reportPeriodInHours);
			_queryIntervalInHours = queryIntervalInHours;
			_statsList = new ConcurrentBag<List<EnvironmentStats>>();
			_query = query;
		}

		public async Task<List<EnvironmentStats>> Generate()
		{
			Console.WriteLine("Generating report...");
			Console.WriteLine(string.Format("Report dates: {0} to {1}", _startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
			Console.WriteLine(string.Format("Query interval: {0} hours\n", _queryIntervalInHours));
			var sw = Stopwatch.StartNew();
			var parallelTaskCount = _reportPeriodInHours/_queryIntervalInHours;
			var tasks = new List<Task>(parallelTaskCount);
			for (var t = 0; t < parallelTaskCount; t++)
			{
				var queryStartTime = _startTime.AddHours(t * _queryIntervalInHours); 
				var queryEndTime = queryStartTime.AddHours(_queryIntervalInHours);
				var task = t; // preventing access to modified closure
				tasks.Add(Task.Run(async () => _statsList.Add(await _query.GetResult(queryStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), queryEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _queryIntervalInHours))));
			}
			await Task.WhenAll(tasks);
			sw.Stop();
			
			// Flatten lists into one list
			var statsList = new List<EnvironmentStats>();
			foreach(var list in _statsList)
			{
				foreach(var e in list)
				{
					statsList.Add(e);
				}
			}
			if (statsList.Any(x => x.EnvironmentName == "FailedQuery"))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(string.Format("At least one query failed. Results are incomplete."));
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(string.Format("All queries finished in {0}\n", sw.Elapsed.ToString()));
			}

			return statsList;
		}
	}
}
