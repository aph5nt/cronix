
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cronix;
using System.Threading;
using CSharpSample.Jobs;
using Microsoft.FSharp.Core;
using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;

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


	public class Program
	{
		public static void Main(string[] args)
		{
			var result = BootStrapper.InitService(new FSharpOption<string[]>(args), null);
			result.Match(
				(state, msgs) =>
				{
					Console.WriteLine(state);
                },
				(msgs) =>
				{
					msgs.ToList().ForEach(Console.WriteLine);
				});
		}
	}
}
