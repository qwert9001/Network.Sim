﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleApplication36.Miscellaneous {
	/// <summary>
	/// Implements a blocking priority queue.
	/// </summary>
	/// <typeparam name="T">The type of the elements stored in the queue.</typeparam>
	/// <remarks>The priority queue is implemented using a min heap.</remarks>
	class BlockingPriorityQueue<T> where T : IComparable<T> {
		/// <summary>
		/// The underlying data-structure for storing and retrieving elements.
		/// </summary>
		private readonly List<T> list = new List<T>();

		/// <summary>
		/// The maximum number of elements the queue can accomodate.
		/// </summary>
		private readonly int maxSize;

		/// <summary>
		/// Initializes a new instance of the BlockingPriorityQueue class using
		/// the specified maximum capacity.
		/// </summary>
		/// <param name="maxSize">The maximum number of elements the queue can
		/// accomodate.</param>
		public BlockingPriorityQueue(int maxSize = Int32.MaxValue) {
			this.maxSize = maxSize;
		}

		/// <summary>
		/// Enqueues the item in the queue.
		/// </summary>
		/// <param name="item">The item to enqueue.</param>
		/// <remarks>If a maximum size was specified when this instance was
		/// initialized, a call to Enqueue may block until space is available
		/// to store the provided item.</remarks>
		public void Enqueue(T item) {
			lock (list) {
				while (list.Count >= maxSize)
					Monitor.Wait(list);
				list.Add(item);
				int ci = list.Count - 1;
				// Bubble up the heap.
				while (ci > 0) {
					int pi = (ci - 1) / 2;
					if (list[ci].CompareTo(list[pi]) >= 0)
						break;
					T tmp = list[ci];
					list[ci] = list[pi];
					list[pi] = tmp;
					ci = pi;
				}
				// Wake up any blocked dequeue.
				if (list.Count == 1)
					Monitor.PulseAll(list);
			}
		}

		/// <summary>
		/// Dequeues the item with the highest priority.
		/// </summary>
		/// <returns>The dequeued item.</returns>
		/// <remarks>A call to Dequeue may block until an item is available to be
		/// removed.</remarks>
		public T Dequeue() {
			lock (list) {
				while (list.Count == 0)
					Monitor.Wait(list);
				int li = list.Count - 1;
				T item = list[0];
				// Put the last item at the front and bubble it down.
				list[0] = list[li];
				list.RemoveAt(li);
				li--;
				int pi = 0;
				while (true) {
					int ci = pi * 2 + 1;
					if (ci > li)
						break;
					int rc = ci + 1;
					if (rc <= li && list[rc].CompareTo(list[ci]) < 0)
						ci = rc;
					if (list[pi].CompareTo(list[ci]) <= 0)
						break;
					T tmp = list[pi];
					list[pi] = list[ci];
					list[ci] = tmp;
					pi = ci;
				}
				// Wake up any blocked enqueue.
				if (list.Count == maxSize - 1)
					Monitor.PulseAll(list);
				return item;
			}
		}

		public T Peek() {
			lock (list) {
				if (list.Count == 0)
					return default(T);
				return list[0];
			}
		}

		/// <summary>
		/// Removes the specified item from the queue.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>true if the item was removed; otherwise false.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the specified
		/// item is not contained in the list.</exception>
		public void Remove(T item) {
			lock (list) {
				int index = Find(item);
				RemoveAt(index);
				// Wake up any blocked enqueue.
				if (list.Count == maxSize - 1)
					Monitor.PulseAll(list);
			}
		}

		/// <summary>
		/// A delegate for removing items from the queue.
		/// </summary>
		/// <param name="item">The item to examine.</param>
		/// <returns>true if the item should be removed from the queue;
		/// otherwise false.</returns>
		public delegate bool removeItem(T item);

		/// <summary>
		/// Removes all items from the queue that satisfy the condition by the
		/// specified predicate.
		/// </summary>
		/// <param name="removeHandler">The delegate that defines the conditions
		/// of the element to remove.</param>
		/// <returns>The number of elements removed from the queue.</returns>
		public int Remove(removeItem removeHandler) {
			lock (list) {
				ISet<T> removeSet = new HashSet<T>();
				foreach (T item in list) {
					if (removeHandler.Invoke(item))
						removeSet.Add(item);
				}
				foreach (T item in removeSet)
					Remove(item);
				return removeSet.Count;
			}
		}

		/// <summary>
		/// Tries to dequeue an item from the queue.
		/// </summary>
		/// <param name="value">The item to be removed from the queue.</param>
		/// <returns>true if an item could be removed; otherwise, false.</returns>
		public bool TryDequeue(out T value) {
			lock (list) {
				while (list.Count == 0) {
					value = default(T);
					return false;
				}
				value = Dequeue();
				return true;
			}
		}

		/// <summary>
		/// Finds the index of the specified item using a linear search.
		/// </summary>
		/// <param name="item">The item to find the list index for.</param>
		/// <returns>The index of the item.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the specified
		/// item is not contained in the list.</exception>
		int Find(T item) {
			for (int i = 0; i < list.Count; i++) {
				if (Object.ReferenceEquals(list[i], item))
					return i;
			}
			throw new InvalidOperationException("The item is not contained " +
				"in the priority queue.");
		}

		/// <summary>
		/// Removes the item at the specified index from the list.
		/// </summary>
		/// <param name="index">The index at which to remove an item.</param>
		void RemoveAt(int index) {
			// Move last item into the position of the to-be-removed item.
			int li = list.Count - 1;
			list[index] = list[li];
			list.RemoveAt(li);
			li--;
			if (list.Count == 0 || index == list.Count)
				return;
			// If the new item is less than its parent, bubble up.
			int ci = index;
			int pi = (ci - 1) / 2;
			while (list[pi].CompareTo(list[ci]) > 0) {
				T tmp = list[ci];
				list[ci] = list[pi];
				list[pi] = tmp;
				ci = pi;
				pi = (ci - 1) / 2;
			}
			// If we haven't bubbled up, we might have to bubble down.
			if (ci == index) {
				while (ci <= li) {
					int rc = ci + 1;
					if (rc <= li && list[rc].CompareTo(list[ci]) < 0)
						ci = rc;
					if (list[pi].CompareTo(list[ci]) <= 0)
						break;
					T tmp = list[pi];
					list[pi] = list[ci];
					list[ci] = tmp;
					pi = ci;
					ci = pi * 2 + 1;
				}
			}
		}
	}
}
