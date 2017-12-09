using System;

namespace dwr
{
	public class EnvironmentStats
	{
		public EnvironmentStats()
		{ }

		public string EnvironmentName { get; set; }
		public string Platform => EnvironmentName.Contains("aws") ? "AWS" : "Azure";
		public DateTime Timestamp { get; set; }
		public int FastRequests { get; set; }
		public int SlowRequests { get; set; }
		public int FailedRequests { get; set; }
		public int TimedoutRequests { get; set; }
		public int UnknownRequests { get; set; }
		public double Apdex { get; set; }
	}
}
