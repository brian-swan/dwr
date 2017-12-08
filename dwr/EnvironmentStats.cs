using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dwr
{
	class EnvironmentStats
	{
		public EnvironmentStats()
		{ }

		public string EnvironmentName { get; set; }
		public DateTime Timestamp { get; set; }
		public int FastRequests { get; set; }
		public int SlowRequests { get; set; }
		public int FailedRequests { get; set; }
		public int TimedoutRequests { get; set; }
		public int UnknownRequests { get; set; }
	}
}
