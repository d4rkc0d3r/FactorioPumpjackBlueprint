using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint.Pathfinding
{
    class PathPointMinHeap : IEnumerable<PathPoint>
    {
        private const int InitialCapacity = 0;
        private const int GrowFactor = 2;
        private const int MinGrow = 1;

        private int mCapacity = InitialCapacity;
        private PathPoint[] mHeap = new PathPoint[InitialCapacity];
        private int mTail;

        public int Count { get { return mTail; } }
        private int Capacity { get { return mCapacity; } }

        public PathPointMinHeap(params PathPoint[] pathPoints)
            : this((IEnumerable<PathPoint>)pathPoints)
        {

        }

        public PathPointMinHeap(IEnumerable<PathPoint> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            foreach (var item in collection)
            {
                if (Count == Capacity)
                {
                    Grow();
                }

                mHeap[mTail++] = item;
            }

            for (int i = Parent(mTail - 1); i >= 0; i--)
            {
                BubbleDown(i);
            }
        }

        public void Add(PathPoint item)
        {
            if (Count == Capacity)
                Grow();

            mHeap[mTail++] = item;
            BubbleUp(mTail - 1);
        }

        public PathPoint Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Heap is empty");
            return mHeap[0];
        }

        public PathPoint Pop()
        {
            if (Count == 0) throw new InvalidOperationException("Heap is empty");
            PathPoint ret = mHeap[0];
            mTail--;
            Swap(mTail, 0);
            BubbleDown(0);
            return ret;
        }

        private void BubbleUp(int i)
        {
            if (i == 0 || Dominates(mHeap[Parent(i)], mHeap[i]))
                return; //correct domination (or root)

            Swap(i, Parent(i));
            BubbleUp(Parent(i));
        }

        private void BubbleDown(int i)
        {
            int dominatingNode = Dominating(i);
            if (dominatingNode == i) return;
            Swap(i, dominatingNode);
            BubbleDown(dominatingNode);
        }

        private int Dominating(int i)
        {
            int dominatingNode = i;
            dominatingNode = GetDominating(YoungChild(i), dominatingNode);
            dominatingNode = GetDominating(OldChild(i), dominatingNode);

            return dominatingNode;
        }

        private int GetDominating(int newNode, int dominatingNode)
        {
            if (newNode < mTail && !Dominates(mHeap[dominatingNode], mHeap[newNode]))
                return newNode;
            return dominatingNode;
        }

        private void Swap(int i, int j)
        {
            PathPoint tmp = mHeap[i];
            mHeap[i] = mHeap[j];
            mHeap[j] = tmp;
        }

        private static int Parent(int i)
        {
            return (i + 1) / 2 - 1;
        }

        private static int YoungChild(int i)
        {
            return (i + 1) * 2 - 1;
        }

        private static int OldChild(int i)
        {
            return YoungChild(i) + 1;
        }

        private void Grow()
        {
            int newCapacity = mCapacity * GrowFactor + MinGrow;
            var newHeap = new PathPoint[newCapacity];
            Array.Copy(mHeap, newHeap, mCapacity);
            mHeap = newHeap;
            mCapacity = newCapacity;
        }

        public IEnumerator<PathPoint> GetEnumerator()
        {
            return mHeap.Take(Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool Dominates(PathPoint x, PathPoint y)
        {
            return x.PredictedDistance < y.PredictedDistance;
        }
    }
}
