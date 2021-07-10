using Dirichlet.Numerics;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests.InternalTypesTests
{
    [TestFixture]
    public class UInt128Tests
    {
        [Test]
        public void IntegerParts_ShouldWork()
        {
            UInt128 a = UInt128.MaxValue;
            
            Assert.That(a.I0, Is.EqualTo(-1));
            Assert.That(a.I1, Is.EqualTo(-1));
            Assert.That(a.I2, Is.EqualTo(-1));
            Assert.That(a.I3, Is.EqualTo(-1));

            UInt128.Create(out UInt128 b, 0xFFFF_FFFF, 0xFFFF_FFFF);
            
            Assert.That(b.I0, Is.EqualTo(-1));
            Assert.That(b.I1, Is.EqualTo(0));
            Assert.That(b.I2, Is.EqualTo(-1));
            Assert.That(b.I3, Is.EqualTo(0));
        }
    }
}