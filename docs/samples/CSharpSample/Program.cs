﻿using Chessie.ErrorHandling.CSharp;
using Cronix;
using Microsoft.FSharp.Core;
using System;
using System.Linq;
using System.Threading;

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
			var result = BootStrapper.InitService(args, scheduler =>
				{
					// schedule your job here
					scheduler.ScheduleJob("scheduled job", "* * * * *", EmbededJobs.Callback);
                    scheduler.ScheduleJob("scheduled job", Dsl.frequency(Dsl.CronExpr.Daily), EmbededJobs.Callback);
				});
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
