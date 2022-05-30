namespace CollectionTrackingHandler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	public static class CollectionExtensions
	{
		/// <summary>
		/// Process items of collection in parallel by limited number of threads
		/// </summary>
		/// <param name="collection">collection to process</param>
		/// <param name="semaphore"></param>
		/// <param name="itemProcessor">callback func invokes for each item of collection</param>
		/// <typeparam name="T">type of collection elements</typeparam>
		/// <typeparam name="TOut">return type of callback function</typeparam>
		/// <returns></returns>
		public static IList<Task<TOut>> ProcessItemsParallel<T, TOut>(
			this ICollection<T> collection,
			SemaphoreSlim semaphore,
			Func<T, Task<TOut>> itemProcessor)
		{
			var tasks = new List<Task<TOut>>(collection.Count);

			foreach (var item in collection)
			{
				var task = semaphore
					.WaitAsync()
					.ContinueWith(_ =>
					{
						return Task.Run(async () =>
						{
							try
							{
								return await itemProcessor(item);
							}
							finally
							{
								semaphore.Release();
							}
						});
					});

				tasks.Add(task.Unwrap());
			}

			return tasks;
		}

		/// <summary>
		/// Process items of collection in parallel by limited number of threads
		/// with ability to track a progress of operation
		/// </summary>
		public static async Task<TOut[]> ProcessItemsParallel<T, TOut>(
			this ICollection<T> collection,
			Func<T, Task<TOut>> itemProcessor,
			Func<int, int, Task> onProgressTrack,
			int trackEachMs = 1000,
			byte maxThreadCount = 10)
		{
			using var semaphore = new SemaphoreSlim(maxThreadCount);

			var count = collection.Count;
			var allTasks = ProcessItemsParallel(collection, semaphore, itemProcessor);
			var allTasksCompletedTask = Task.WhenAll(allTasks);

			while (await Task.WhenAny(allTasksCompletedTask, Task.Delay(trackEachMs)) != allTasksCompletedTask)
			{
				await onProgressTrack(allTasks.Count(t => t.IsCompleted), count);
			}

			await onProgressTrack(allTasks.Count(t => t.IsCompleted), count);

			// all tasks are completed at this point, so we may just return the result
			return allTasksCompletedTask.Result;
		}
	}
}
