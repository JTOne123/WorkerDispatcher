using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkerDispatcher;

namespace UnitTests
{
	[TestFixture]
	public abstract class WorkDispatcherFixture
	{
		protected Mock<IWorkerHandler> MockWorkerHandler = new Mock<IWorkerHandler>();
		protected Mock<IActionInvoker> MockActionInvoker = new Mock<IActionInvoker>();

		public ActionDispatcherFactory Factory;

		protected IDispatcherToken DispatcherToken;

		public WorkDispatcherFixture()
		{
			Factory = new ActionDispatcherFactory(MockWorkerHandler.Object);

			DispatcherToken = Factory.Start(new ActionDispatcherSettings
			{
				Timeout = TimeSpan.FromSeconds(10)
			});
		}
	}

	[TestFixture]
	public class post_some_action : WorkDispatcherFixture
	{
		[SetUp]
		public void Initalize()
		{
			DispatcherToken.Post(MockActionInvoker.Object);
			DispatcherToken.Post(MockActionInvoker.Object);
			DispatcherToken.Post(p => Task.Delay(100, p));
		}

		[Test]
		public void should_be_two_action_execute()
		{
			DispatcherToken.WaitCompleted();

			MockActionInvoker.Verify(p => p.Invoke(It.IsAny<CancellationToken>()), Times.Exactly(2));
		}
	}

	[TestFixture]
	public class worker_longer : WorkDispatcherFixture
	{
		[SetUp]
		public void Initalize()
		{
			MockActionInvoker.Reset();
			MockWorkerHandler.Reset();

			DispatcherToken = Factory.Start(new ActionDispatcherSettings
			{
				Timeout = TimeSpan.FromSeconds(0.1)
			});

            DispatcherToken.Post(t => Task.Delay(10000, t));
		}

		[Test]
		public void should_be_cancelled_work()
		{
			DispatcherToken.WaitCompleted();

			MockWorkerHandler.Verify(p => p.HandleError(null, It.IsAny<decimal>(), true), Times.Once);
		}
	}

	[TestFixture]
	public class worker_patial_succcess : WorkDispatcherFixture
	{
		[SetUp]
		public void Initalize()
		{
			MockWorkerHandler.Invocations.Clear();
			MockActionInvoker.Invocations.Clear();

			DispatcherToken = Factory.Start(new ActionDispatcherSettings
			{
				Timeout = TimeSpan.FromSeconds(1)
			});            

            DispatcherToken.Post(MockActionInvoker.Object);
			DispatcherToken.Post(p => throw new ArgumentException());
			DispatcherToken.Post(MockActionInvoker.Object);
		}

		[Test]
		public void should_be_two_worker_from_all_success()
		{
			DispatcherToken.WaitCompleted();

			MockActionInvoker.Verify(p => p.Invoke(It.IsAny<CancellationToken>()), Times.Exactly(2));
		}

		[Test]
		public void should_be_from_all_worker_one_error()
		{
			DispatcherToken.WaitCompleted();

			MockWorkerHandler.Verify(p => p.HandleError(It.IsAny<Exception>(), It.IsAny<decimal>(), It.IsAny<bool>()), Times.Once);
		}
	}

	[TestFixture]
	public class post_null_action : WorkDispatcherFixture
	{
		[Test]
		public void should_be_fault_execute()
		{
			Assert.Throws<ArgumentNullException>(() => DispatcherToken.Post(default(IActionInvoker)));
		}

		[Test]
		public void should_be_data_fault_execute()
		{
			Assert.Throws<ArgumentNullException>(() => DispatcherToken.Post(default(IActionInvoker<object>), default(object)));
		}

		[Test]
		public void should_be_fn_fault_execute()
		{
			Assert.Throws<ArgumentNullException>(() => DispatcherToken.Post(default(Func<CancellationToken, Task>)));
		}
	}

	[TestFixture]
	public class post_data_success : WorkDispatcherFixture
	{
		const string ExpectData = "SomeData";

		protected Mock<IActionInvoker<string>> MockActionDataInvoker = new Mock<IActionInvoker<string>>();

		[SetUp]
		public void Initalize()
		{
			DispatcherToken.Post(MockActionDataInvoker.Object, ExpectData);
		}

		[Test]
		public void should_action_execute()
		{
			DispatcherToken.WaitCompleted();

			MockActionDataInvoker.Verify(p => p.Invoke(It.Is<string>(s => s == ExpectData), It.IsAny<CancellationToken>()), Times.Exactly(1));
		}
	}
}
