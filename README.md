# CollectionTrackingHandler
An extension method for a collection that allows the elements of that collection to be processed in parallel with a limited number of threads and with ability to track progress.

[![NuGet version (CollectionTrackingHandler)](https://img.shields.io/nuget/vpre/CollectionTrackingHandler?color=orange&label=nuget%20package)](https://www.nuget.org/packages/CollectionTrackingHandler/1.0.0.1)

# Usage

```csharp
using CollectionTrackingHandler;

var list = new List<string>{...};

Func<string, Task<string>> itemProcessor 
	= async i => 
	{ 
		// imitate long-running operation
		await Task.Delay(new Random().Next(1000, 10_000));
		return i; 
	};

Func<int, int, TimeSpan, Task> displayProgress 
	= async (completedTaskCount, commonTaskCount, timeEllapsed) => 
	{
		Console.WriteLine($"{completedTaskCount}/{commonTaskCount} within {timeEllapsed.Seconds} sec.");
	};

var result = await list.HandleItemsInParallel(itemProcessor, displayProgress, maxThreadCount: 10);

```
