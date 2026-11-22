/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The three properties of <see cref="ObjectIDGenerator"/> that the rest of the catalog rests on and that no
	/// higher-layer test can reach: identifiers start at one, which is what leaves zero free to mean "this object has
	/// not been given an identifier"; raising the generator to a catalog store's maximum never lowers it past the
	/// built-in objects created before the store was read; and exhaustion is refused rather than wrapping into the
	/// negative identifiers that mean something else.
	///
	/// What is deliberately not covered is that exhaustion is permanent - the generator pins its counter at
	/// Int32.MaxValue on the wrap so it cannot climb back through identifiers in use. It takes another two billion
	/// calls to tell the two apart and the counter is not readable, so there is nothing a test can observe.
	///
	/// Each case drives a generator of its own, which is also how every engine allocates since
	/// bug-object-id-counter-is-process-wide.
	/// </summary>
	[TestFixture]
	public class ObjectIDGeneratorTest
	{
		[Test]
		public void AllocationStartsAtOneSoZeroIsNeverAnIdentifier()
		{
			ObjectIDGenerator generator = new ObjectIDGenerator();

			Assert.That(generator.Allocate(), Is.EqualTo(1));
			Assert.That(generator.Allocate(), Is.EqualTo(2));
		}

		[Test]
		public void EnsureAllocatedThroughRaisesTheGeneratorButNeverLowersIt()
		{
			ObjectIDGenerator generator = new ObjectIDGenerator();
			generator.EnsureAllocatedThrough(500);

			Assert.That(generator.Allocate(), Is.EqualTo(501), "the generator was not raised to the store's maximum");

			// An empty or first-run catalog store reports a maximum below the built-in objects already numbered.
			generator.EnsureAllocatedThrough(0);

			Assert.That(generator.Allocate(), Is.EqualTo(502), "a lower maximum pulled the generator back onto identifiers in use");
		}

		[Test]
		public void ExhaustionIsRefusedRatherThanWrappingNegative()
		{
			ObjectIDGenerator generator = new ObjectIDGenerator();
			generator.EnsureAllocatedThrough(Int32.MaxValue);

			SchemaException exception = Assert.Throws<SchemaException>(() => generator.Allocate());
			Assert.That(exception.Code, Is.EqualTo((int)SchemaException.Codes.ObjectIDsExhausted));
		}
	}
}
