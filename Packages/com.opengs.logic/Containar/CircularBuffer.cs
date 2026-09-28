#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore { 
    public class CircularLinkedList<T>
    {
        private readonly LinkedList<T> list = new();
        private LinkedListNode<T>? current;

        public int Count => list.Count;

        public void Add(T item)
        {
            list.AddLast(item); // 要素を追加

            // 最初に追加されたアイテムが `current` になる
            if (current == null)
            {
                current = list.First;
            }
            else
            {
                current = current.Next ?? list.First; // 循環する
            }
        }

        public T GetNext()
        {
            if (current == null) throw new InvalidOperationException("List is empty.");

            T value = current.Value;
            current = current.Next ?? list.First; // 次のノード、なければ最初に戻る
            return value;
        }

        public T Peek()
        {
            if (current == null) throw new InvalidOperationException("List is empty.");
            return current.Value;
        }
    }
}

