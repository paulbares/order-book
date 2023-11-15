using PricePointBook.DataStructure;

namespace PricePointBook.UnitTests.DataStructure
{
    [TestClass]
    public class TestBinaryHeap
    {
        [TestMethod]
        public void TestAddAndRemove()
        {
            var comparer = Comparer<long>.Create((a, b) => a.CompareTo(b));
            var heap = new BinaryHeapLong(16, comparer);

            // Add elements and check the min
            heap.Add(7);
            Assert.AreEqual(7, heap.Peek());
            heap.Add(3);
            Assert.AreEqual(3, heap.Peek());
            heap.Add(1);
            Assert.AreEqual(1, heap.Peek());
            heap.Add(5);
            Assert.AreEqual(1, heap.Peek());
            heap.Add(2);
            Assert.AreEqual(1, heap.Peek());

            // Remove
            heap.Remove(2);
            Assert.AreEqual(1, heap.Peek());
            heap.Remove(1);
            Assert.AreEqual(3, heap.Peek());
            heap.Remove(5);
            Assert.AreEqual(3, heap.Peek());
            heap.Remove(3);
            Assert.AreEqual(7, heap.Peek());
        }

        [TestMethod]
        public void TestGetElements()
        {
            var comparer = Comparer<long>.Create((a, b) => a.CompareTo(b));
            var heap = new BinaryHeapLong(16, comparer);

            heap.Add(1);
            heap.Add(3);
            heap.Add(3);
            heap.Add(3);
            heap.Add(2);
            heap.Add(2);
            heap.Add(6);
            heap.Add(7);

            // Verify the uniqueness of the elements
            CollectionAssert.AreEqual(new List<long> { 1, 2, 3, 6, 7 }, heap.GetOrderedElements().ToList());
        }
    }
}