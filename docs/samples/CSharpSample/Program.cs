
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cronix;
using System.Threading;
using CSharpSample.Jobs;
using Microsoft.FSharp.Core;

namespace CSharpSample
{
	public static class EmbededJobs
	{
		public static void Callback(CancellationToken token)
		{
			Console.WriteLine("executing embeded job from CSharpSample.dll at {0}", DateTime.Now);
			Thread.Sleep(100);
		}
	}


	class Program
	{
		static void Main(string[] args)
		{
			BootStrapper.initService(new FSharpOption<string[]>(null), null);
        }
	}
}
