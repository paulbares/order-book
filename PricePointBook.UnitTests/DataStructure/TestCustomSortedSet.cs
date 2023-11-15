using PricePointBook.DataStructure;

namespace PricePointBook.UnitTests.DataStructure
{
    [TestClass]
    public class TestCustomSortedSet
    {
        [TestMethod]
        public void TestMinMax()
        {
            var comparer = Comparer<int>.Create((a, b) => a.CompareTo(b));
            var set = new CustomSortedSet<int>(comparer);
            for (int i = 0; i < 8; i++)
            {
                set.Add(i);
            }

            VerifyMinAndMax(0, 7, set);

            set.Remove(1);
            VerifyMinAndMax(0, 7, set);

            set.Remove(0); // remove the min
            VerifyMinAndMax(2, 7, set);

            set.Remove(7); // remove the max
            VerifyMinAndMax(2, 6, set);

            set.Remove(6); // remove the new max
            VerifyMinAndMax(2, 5, set);

            set.Remove(1); // remove the new min
            VerifyMinAndMax(2, 5, set);
        }

        private static void VerifyMinAndMax(int expectedMin, int expectedMax, CustomSortedSet<int> set)
        {
            Assert.AreEqual(expectedMin, set.MinValue);
            Assert.AreEqual(expectedMax, set.MaxValue);
        }
    }
}