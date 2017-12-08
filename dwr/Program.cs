using System;
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
			var report = new DorothyReport("2017-12-06T00:00:00Z", 6, 1);
			report.Generate().Wait();
			
			Console.ReadLine();
		}
	}
}
