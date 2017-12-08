using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dwr
{
	public interface IQuery
	{
		// need to make this return a task of generic type
		Task<List<EnvironmentStats>> GetResult(string startTime, string endTime, int queryIntervalInHours);
	}
}
