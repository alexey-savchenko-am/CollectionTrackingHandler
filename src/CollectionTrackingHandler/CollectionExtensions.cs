namespace CollectionTrackingHandler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	public class CollectionExtensions
    {
		public static async Task<TOut[]> HandleItemsInParallel<T, TOut>(ICollection<T> collection,
					Func<T, Task<TOut>> itemProcessor,
					Func<int, int, TimeSpan, Task> onProgress = null,
					byte maxThreadCount = 10)
		{
			using var semaphore = new SemaphoreSlim(maxThreadCount);

			var count = collection.Count();
			var startedAt = DateTime.Now;

			var allTasks = processItemsInternal(semaphore);
			var onCompletionTask = Task.WhenAll(allTasks);

			while (await Task.WhenAny(onCompletionTask, Task.Delay(1000)) != onCompletionTask)
				await showProgress();

			await showProgress();

			return onCompletionTask.Result;

			Task showProgress()
			{
				if (onProgress != null)
					return onProgress(allTasks.Count(t => t.IsCompleted), count, DateTime.Now - startedAt);
				return Task.CompletedTask;
			}

			IEnumerable<Task<TOut>> processItemsInternal(SemaphoreSlim semaphore)
			{
				var tasks = new List<Task<TOut>>(count);

				foreach (var item in collection)
				{
					var t = semaphore
						.WaitAsync()
						.ContinueWith(_ =>
						{
							return Task.Run(() =>
							{
								try
								{
									return itemProcessor(item);
								}
								finally
								{
									semaphore.Release();
								}
							});
						});

					tasks.Add(t.Unwrap());

				}

				return tasks;
			}
		}
	}
}
