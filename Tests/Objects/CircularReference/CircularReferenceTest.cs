using System.Reflection;
using AssemblyToProcess.CircularReference;
using Xunit;

namespace Tests.Objects.CircularReference
{
    public class CircularReferenceTest
    {
        private readonly Assembly _assembly;

        public CircularReferenceTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void CircWork()
        {
            CircularReferenceOne cr = new CircularReferenceOne
                {
                    Identifier = 1,
                    CR2 = new CircularReferenceTwo
                        {
                            Identifier = 2
                        }
                };
            cr.CR2.CR1 = cr;

            var copy = cr.HCopy();
        }

        [Fact]
        public void BasicTest()
        {
        }
    }
}