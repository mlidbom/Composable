﻿using System;
using System.Threading;
using Composable.Logging;

namespace Composable.SystemExtensions.Threading
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class ThreadExceptionHandler
    {
        //Urgent: We need something much better than this which lets us surface exceptions to tests and endpoints etc in a sane fashion. Things should not just stop happening and we should not just swallow exceptions either.
        internal static ThreadStart WrapThreadStart(ThreadStart start) =>
            () =>
            {
                try
                {
                    start.Invoke();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
                {
                    Logger.For<ThreadExceptionHandler>().Info($"Thread: {Thread.CurrentThread.Name} is terminating because it received a: {exception.GetType().Name}.");
                }
                catch(Exception exception)
                {
                    Logger.For<ThreadExceptionHandler>().Error(exception, $"Error occured on background poller thread: {Thread.CurrentThread.Name}. Thread is no longer running.");
                }
            };
    }
}