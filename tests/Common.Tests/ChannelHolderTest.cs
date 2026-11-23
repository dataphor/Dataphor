/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

using NUnit.Framework;

using Alphora.Common.WCF;

namespace Alphora.Dataphor.Common.Tests
{
	/// <summary>
	/// The channel holder every WCF client in the product keeps its channel in: what it reuses, what it replaces, and
	/// what it does with the channel it replaced.
	/// </summary>
	/// <remarks>
	/// No WCF here, and no network: the holder reaches a channel only through ICommunicationObject, so a stub with a
	/// settable state and a hook inside its state read shows every case, including the one that needed a severed
	/// connection to reproduce before this seam existed.
	/// </remarks>
	[TestFixture]
	public class ChannelHolderTest
	{
		/// <summary>
		/// A channel is reused from the moment it is created. WCF opens a channel implicitly inside the first operation
		/// called on it, so a channel fresh from a factory reports Created and reaches Opened only once a call on it has
		/// connected; the pattern this holder replaced treated anything but Opened as unusable, and so created a channel
		/// on every call for as long as a whole network round trip - the caching it existed to provide never hitting at
		/// all over exactly the interval two threads compete over.
		/// </summary>
		[Test]
		public void TestAChannelIsReusedBeforeItHasOpened()
		{
			int createdCount = 0;
			using (ChannelHolder<IStubService> holder = new ChannelHolder<IStubService>(() => { createdCount++; return new StubChannel(); }))
			{
				IStubService first = holder.GetChannel();
				Assert.That(((ICommunicationObject)first).State, Is.EqualTo(CommunicationState.Created), "The stub did not start in the state a channel out of a channel factory reports.");

				IStubService second = holder.GetChannel();

				Assert.That(second, Is.SameAs(first), "The holder created a second channel while the first was still usable.");
				Assert.That(createdCount, Is.EqualTo(1), "The holder called the channel factory more than once.");
			}
		}

		/// <summary>
		/// A channel that is finished is replaced, and the channel it replaced is aborted rather than dropped. Aborted
		/// and not closed: a channel being replaced is faulted, superseded or closing, so there is no orderly shutdown to
		/// attempt, and a close against a server that has gone blocks for want of a time-out on the client's binding.
		/// </summary>
		[Test]
		public void TestAFaultedChannelIsReplacedAndTheReplacedOneAborted()
		{
			List<StubChannel> created = new List<StubChannel>();
			using (ChannelHolder<IStubService> holder = new ChannelHolder<IStubService>(() => { StubChannel channel = new StubChannel(); created.Add(channel); return channel; }))
			{
				StubChannel first = (StubChannel)holder.GetChannel();
				first.SetState(CommunicationState.Faulted);

				IStubService second = holder.GetChannel();

				Assert.That(second, Is.Not.SameAs(first), "The holder handed back a channel that had faulted.");
				Assert.That(created, Has.Count.EqualTo(2), "The holder did not create a replacement.");
				Assert.That(first.AbortCount, Is.EqualTo(1), "The channel the holder replaced was not aborted exactly once; nothing else would have released its socket.");
				Assert.That(first.CloseCount, Is.EqualTo(0), "The channel the holder replaced was closed rather than aborted.");
			}
		}

		/// <summary>
		/// A fault arriving while the holder is testing its channel does not make the call return null. The pattern this
		/// holder replaced read its channel field three times - a null test, a state test, and the return - and a Faulted
		/// event raised on a communication-stack thread between the second and the third cleared the field, so the call
		/// returned null and the caller's next line dereferenced it: a NullReferenceException out of the middle of an
		/// ordinary operation, where the honest answer was a communication failure. The stub raises its fault from inside
		/// the state read itself, which is that instant, deterministically and on one thread.
		/// </summary>
		[Test]
		public void TestAFaultDuringTheStateTestDoesNotReturnNull()
		{
			using (ChannelHolder<IStubService> holder = new ChannelHolder<IStubService>(() => new StubChannel()))
			{
				StubChannel first = (StubChannel)holder.GetChannel();
				first.SetState(CommunicationState.Opened);
				first.OnStateRead(channel => channel.RaiseFaulted());

				IStubService result = holder.GetChannel();

				Assert.That(result, Is.Not.Null, "The holder returned null to its caller.");
				Assert.That(result, Is.SameAs(first), "The holder did not return the channel its state test settled on.");
				Assert.That(first.StateReadCount, Is.GreaterThan(0), "The holder never read the channel's state, so this case exercised nothing.");
			}
		}

		/// <summary>
		/// Disposal releases the channel the holder is holding - closed, because this is the one orderly shutdown the
		/// holder attempts - and every later call is refused by an exception that names the object rather than handing
		/// back a channel nothing will close. The clients this holder sits in dispose while their keep-alive thread may
		/// still be calling, so what a call after disposal raises is part of the contract: ObjectDisposedException, which
		/// the keep-alive already swallows, and never NullReferenceException.
		/// </summary>
		[Test]
		public void TestDisposeClosesTheHeldChannelAndRefusesLaterCalls()
		{
			ChannelHolder<IStubService> holder = new ChannelHolder<IStubService>(() => new StubChannel());
			StubChannel channel = (StubChannel)holder.GetChannel();
			channel.SetState(CommunicationState.Opened);

			holder.Dispose();

			Assert.That(channel.CloseCount, Is.EqualTo(1), "Disposal did not close the channel the holder was holding.");
			Assert.That(channel.AbortCount, Is.EqualTo(0), "Disposal aborted an open channel rather than closing it.");
			Assert.That(() => holder.GetChannel(), Throws.InstanceOf<ObjectDisposedException>(), "A call after disposal was not refused by an exception naming the object.");

			holder.Dispose();

			Assert.That(channel.CloseCount, Is.EqualTo(1), "A second disposal shut the channel down again.");
		}

		/// <summary>
		/// Every channel the holder creates is handed to a caller, and every channel it replaces is aborted exactly once.
		/// This is the accounting the pattern it replaced got wrong: two threads that both found the field unusable both
		/// created a channel, the second assignment overwrote the first without closing it, and the method returned the
		/// field - so the loser's channel, a WCF channel over a TCP connection with no receive time-out, was referenced
		/// by nobody and closed by nobody for the life of the process. Measured on the shipped client before the fix: of
		/// 8,000 channels created by two threads, 13, 130 and 58 went to no caller in three successive runs.
		/// </summary>
		/// <remarks>
		/// The counters are exact, so none of these assertions can pass vacuously; only whether threads actually collide
		/// is left to timing, and the stubs fault themselves every few calls so that replacement happens throughout the
		/// run rather than only at the start.
		/// </remarks>
		[Test]
		public void TestEveryChannelCreatedIsReturnedToACaller()
		{
			const int ThreadCount = 4;
			const int IterationsPerThread = 2500;
			const int CallsPerChannel = 3;

			List<StubChannel> created = new List<StubChannel>();
			object createdSyncHandle = new object();
			ChannelHolder<IStubService> holder =
				new ChannelHolder<IStubService>
				(
					() =>
					{
						StubChannel channel = new StubChannel();
						channel.OnStateRead(c => { if (c.StateReadCount >= CallsPerChannel) c.SetState(CommunicationState.Faulted); });
						// Taken inside the holder's own lock, and taken nowhere else, so there is no second order to deadlock on.
						lock (createdSyncHandle)
							created.Add(channel);
						return channel;
					}
				);
			try
			{
				List<Exception> errors = new List<Exception>();
				Thread[] threads = new Thread[ThreadCount];
				for (int index = 0; index < ThreadCount; index++)
				{
					threads[index] =
						new Thread
						(
							() =>
							{
								try
								{
									for (int iteration = 0; iteration < IterationsPerThread; iteration++)
										((StubChannel)holder.GetChannel()).Returned = true;
								}
								catch (Exception exception)
								{
									lock (errors)
										errors.Add(exception);
								}
							}
						);
					threads[index].IsBackground = true;
				}

				foreach (Thread thread in threads)
					thread.Start();
				foreach (Thread thread in threads)
					thread.Join();

				Assert.That(errors, Is.Empty, "A GetChannel call threw: " + (errors.Count > 0 ? errors[0].ToString() : String.Empty));

				List<StubChannel> channels;
				lock (createdSyncHandle)
					channels = new List<StubChannel>(created);

				Assert.That(channels.Count, Is.GreaterThan(ThreadCount), "No channel was ever replaced, so this case exercised no collision.");
				foreach (StubChannel channel in channels)
				{
					Assert.That(channel.Returned, Is.True, "A channel the holder created was handed to no caller: that is the leak this holder exists to make unreachable.");
					Assert.That(channel.AbortCount, Is.LessThanOrEqualTo(1), "A channel was aborted more than once.");
					Assert.That(channel.CloseCount, Is.EqualTo(0), "A channel being replaced was closed rather than aborted.");
				}

				// Every channel but the one the holder still holds was replaced, and replacement aborts.
				Assert.That(channels.FindAll(channel => channel.AbortCount == 1).Count, Is.EqualTo(channels.Count - 1), "The number of channels aborted is not the number created less the one the holder still holds.");
			}
			finally
			{
				holder.Dispose();
			}
		}

		/// <summary>The service interface the stub channels implement, standing in for one of the product's contracts.</summary>
		private interface IStubService
		{
			void DoSomething();
		}

		/// <summary>A channel that is nothing but the state the holder reads and the shutdown calls it makes.</summary>
		private class StubChannel : IStubService, ICommunicationObject
		{
			private volatile CommunicationState _state = CommunicationState.Created;
			private volatile Action<StubChannel> _onStateRead;
			private int _stateReadCount;
			private int _abortCount;
			private int _closeCount;

			/// <summary>The state the holder tests, counted, and with a case's hook run inside the read.</summary>
			public CommunicationState State
			{
				get
				{
					Interlocked.Increment(ref _stateReadCount);
					Action<StubChannel> onStateRead = _onStateRead;
					if (onStateRead != null)
						onStateRead(this);
					return _state;
				}
			}

			/// <summary>How many times the holder has read this channel's state.</summary>
			public int StateReadCount { get { return _stateReadCount; } }

			public int AbortCount { get { return _abortCount; } }

			public int CloseCount { get { return _closeCount; } }

			/// <summary>Whether GetChannel handed this channel to a caller.</summary>
			public volatile bool Returned;

			public void SetState(CommunicationState state)
			{
				_state = state;
			}

			/// <summary>Runs an action inside every state read, which is how a case puts a fault inside the holder's own test.</summary>
			public void OnStateRead(Action<StubChannel> onStateRead)
			{
				_onStateRead = onStateRead;
			}

			/// <summary>Raises the channel's Faulted event, as the communication stack does when a connection dies.</summary>
			public void RaiseFaulted()
			{
				EventHandler faulted = Faulted;
				if (faulted != null)
					faulted(this, EventArgs.Empty);
			}

			public void Abort()
			{
				Interlocked.Increment(ref _abortCount);
				_state = CommunicationState.Closed;
			}

			public void Close()
			{
				Interlocked.Increment(ref _closeCount);
				_state = CommunicationState.Closed;
			}

			public event EventHandler Faulted;

			#region IStubService members

			public void DoSomething() { }

			#endregion

			#region The rest of ICommunicationObject, which ChannelHolder never reaches

			public event EventHandler Closed { add { } remove { } }
			public event EventHandler Closing { add { } remove { } }
			public event EventHandler Opened { add { } remove { } }
			public event EventHandler Opening { add { } remove { } }

			public void Close(TimeSpan timeout) { throw new NotSupportedException(); }
			public IAsyncResult BeginClose(AsyncCallback callback, object state) { throw new NotSupportedException(); }
			public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state) { throw new NotSupportedException(); }
			public void EndClose(IAsyncResult result) { throw new NotSupportedException(); }
			public void Open() { throw new NotSupportedException(); }
			public void Open(TimeSpan timeout) { throw new NotSupportedException(); }
			public IAsyncResult BeginOpen(AsyncCallback callback, object state) { throw new NotSupportedException(); }
			public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state) { throw new NotSupportedException(); }
			public void EndOpen(IAsyncResult result) { throw new NotSupportedException(); }

			#endregion
		}
	}
}
