using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Conkist.GDK.Services.Tests
{
    public class PlayfabSeviceTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void PlayfabSeviceTestsSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PlayfabSeviceTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
