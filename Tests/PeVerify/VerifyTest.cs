using System.Reflection;
using Xunit;

namespace Tests.PeVerify
{
    /// <summary>
    /// Runs PeVerify on the exe.
    /// </summary>
    public class VerifyTest
    {
        private readonly Assembly _assembly;

        public VerifyTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

#if(DEBUG)
        [Fact]
        public void PeVerify()
        {
            Verifier.Verify(this._assembly.CodeBase.Remove(0, 8));
        }
#endif
    }
}