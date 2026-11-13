using Xunit;

// All tests share one application database that is reset before every test, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
