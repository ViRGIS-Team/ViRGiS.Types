/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

//from https://gist.github.com/ditzel/194ec800053ce7083b73faa1be9101b0#file-kdtree-cs


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis {

    public class KdTree<T> : IEnumerable<T> where T : Component
    {
        protected KdNode Root;
        protected KdNode Last;
        private readonly bool _just2D;
        private float _lastUpdate = -1f;
        protected KdNode[] Open;

        private int _count;
        private float _averageSearchLength;
        private float _averageSearchDeep;

        /// <summary>
        /// create a tree
        /// </summary>
        /// <param name="just2D">just use x/z</param>
        public KdTree(bool just2D = false)
        {
            _just2D = just2D;
        }

        public T this[int key]
        {
            get
            {
                if (key >= _count)
                    throw new ArgumentOutOfRangeException();
                var current = Root;
                for (var i = 0; i < key; i++)
                    current = current.Next;
                return current.Component;
            }
        }

        /// <summary>
        /// add item
        /// </summary>
        /// <param name="item">item</param>
        private void Add(T item)
        {
            _add(new KdNode() { Component = item });
        }

        /// <summary>
        /// batch add items
        /// </summary>
        /// <param name="items">items</param>
        public void AddAll(List<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        /// <summary>
        /// find all objects that matches the given predicate
        /// </summary>
        /// <param name="match">lamda expression</param>
        public KdTree<T> FindAll(Predicate<T> match)
        {
            var list = new KdTree<T>(_just2D);
            foreach (var node in this)
                if (match(node))
                    list.Add(node);
            return list;
        }

        /// <summary>
        /// find first object that matches the given predicate
        /// </summary>
        /// <param name="match">lamda expression</param>
        public T Find(Predicate<T> match)
        {
            var current = Root;
            while (current != null)
            {
                if (match(current.Component))
                    return current.Component;
                current = current.Next;
            }
            return null;
        }

        /// <summary>
        /// Remove at position i (position in list or loop)
        /// </summary>
        public void RemoveAt(int i)
        {
            var list = new List<KdNode>(_getNodes());
            list.RemoveAt(i);
            Clear();
            foreach (var node in list)
            {
                node.OldRef = null;
                node.Next = null;
            }
            foreach (var node in list)
                _add(node);
        }

        /// <summary>
        /// remove all objects that matches the given predicate
        /// </summary>
        /// <param name="match">lamda expression</param>
        public void RemoveAll(Predicate<T> match)
        {
            var list = new List<KdNode>(_getNodes());
            list.RemoveAll(n => match(n.Component));
            Clear();
            foreach (var node in list)
            {
                node.OldRef = null;
                node.Next = null;
            }
            foreach (var node in list)
                _add(node);
        }

        /// <summary>
        /// count all objects that matches the given predicate
        /// </summary>
        /// <param name="match">lamda expression</param>
        /// <returns>matching object count</returns>
        public int CountAll(Predicate<T> match)
        {
            int count = 0;
            foreach (var node in this)
                if (match(node))
                    count++;
            return count;
        }

        /// <summary>
        /// clear tree
        /// </summary>
        private void Clear()
        {


            //rest for the garbage collection
            Root = null;
            Last = null;
            _count = 0;
        }

        /// <summary>
        /// Update positions (if objects moved)
        /// </summary>
        /// <param name="rate">Updates per second</param>
        public void UpdatePositions(float rate)
        {
            if (Time.timeSinceLevelLoad - _lastUpdate < 1f / rate)
                return;

            _lastUpdate = Time.timeSinceLevelLoad;

            UpdatePositions();
        }

        /// <summary>
        /// Update positions (if objects moved)
        /// </summary>
        private void UpdatePositions()
        {
            //save old traverse
            var current = Root;
            while (current != null)
            {
                current.OldRef = current.Next;
                current = current.Next;
            }

            //save root
            current = Root;

            //reset values
            Clear();

            //readd
            while (current != null)
            {
                _add(current);
                current = current.OldRef;
            }
        }

        /// <summary>
        /// Method to enable foreach-loops
        /// </summary>
        /// <returns>Enumberator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            var current = Root;
            while (current != null)
            {
                yield return current.Component;
                current = current.Next;
            }
        }

        /// <summary>
        /// Convert to list
        /// </summary>
        /// <returns>list</returns>
        public List<T> ToList()
        {
            var list = new List<T>();
            foreach (var node in this)
                list.Add(node);
            return list;
        }

        /// <summary>
        /// Method to enable foreach-loops
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private float _distance(Vector3 a, Vector3 b)
        {
            if (_just2D)
                return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
            else
                return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
        }

        private float _getSplitValue(int level, Vector3 position)
        {
            if (_just2D)
                return (level % 2 == 0) ? position.x : position.z;
            else
                return (level % 3 == 0) ? position.x : (level % 3 == 1) ? position.y : position.z;
        }

        private void _add(KdNode newNode)
        {
            _count++;
            newNode.Left = null;
            newNode.Right = null;
            newNode.Level = 0;
            var parent = _findParent(newNode.Component.transform.position);

            //set last
            if (Last != null)
                Last.Next = newNode;
            Last = newNode;

            //set root
            if (parent == null)
            {
                Root = newNode;
                return;
            }

            var splitParent = _getSplitValue(parent);
            var splitNew = _getSplitValue(parent.Level, newNode.Component.transform.position);

            newNode.Level = parent.Level + 1;

            if (splitNew < splitParent)
                parent.Left = newNode; //go left
            else
                parent.Right = newNode; //go right
        }

        private KdNode _findParent(Vector3 position)
        {
            //travers from root to bottom and check every node
            var current = Root;
            var parent = Root;
            while (current != null)
            {
                var splitCurrent = _getSplitValue(current);
                var splitSearch = _getSplitValue(current.Level, position);

                parent = current;
                current = splitSearch < splitCurrent ? current.Left : //go left
                    current.Right; //go right
            }
            return parent;
        }

        /// <summary>
        /// Find the closest object to given position
        /// <param name="position">position</param>
        /// <returns>closest object</returns>
        /// </summary>
        public T FindClosest(Vector3 position)
        {
            return _findClosest(position);
        }

        /// <summary>
        /// Find close objects to given position
        /// </summary>
        /// <param name="position">position</param>
        /// <returns>close object</returns>
        public IEnumerable<T> FindClose(Vector3 position)
        {
            var output = new List<T>();
            _findClosest(position, output);
            return output;
        }

        private T _findClosest(Vector3 position, List<T> traversed = null)
        {
            if (Root == null)
                return null;

            var nearestDist = float.MaxValue;
            KdNode nearest = null;

            if (Open == null || Open.Length < _count)
                Open = new KdNode[_count];
            for (int i = 0; i < Open.Length; i++)
                Open[i] = null;

            var openAdd = 0;
            var openCur = 0;

            if (Root != null)
                Open[openAdd++] = Root;

            while (openCur < Open.Length && Open[openCur] != null)
            {
                var current = Open[openCur++];
                if (traversed != null)
                    traversed.Add(current.Component);

                var nodeDist = _distance(position, current.Component.transform.position);
                if (nodeDist < nearestDist)
                {
                    nearestDist = nodeDist;
                    nearest = current;
                }

                var splitCurrent = _getSplitValue(current);
                var splitSearch = _getSplitValue(current.Level, position);

                if (splitSearch < splitCurrent)
                {
                    if (current.Left != null)
                        Open[openAdd++] = current.Left; //go left
                    if (Mathf.Abs(splitCurrent - splitSearch) * Mathf.Abs(splitCurrent - splitSearch) < nearestDist && current.Right != null)
                        Open[openAdd++] = current.Right; //go right
                }
                else
                {
                    if (current.Right != null)
                        Open[openAdd++] = current.Right; //go right
                    if (Mathf.Abs(splitCurrent - splitSearch) * Mathf.Abs(splitCurrent - splitSearch) < nearestDist && current.Left != null)
                        Open[openAdd++] = current.Left; //go left
                }
            }

            _averageSearchLength = (99f * _averageSearchLength + openCur) / 100f;
            _averageSearchDeep = (99f * _averageSearchDeep + nearest.Level) / 100f;

            return nearest.Component;
        }

        private float _getSplitValue(KdNode node)
        {
            return _getSplitValue(node.Level, node.Component.transform.position);
        }

        private IEnumerable<KdNode> _getNodes()
        {
            var current = Root;
            while (current != null)
            {
                yield return current;
                current = current.Next;
            }
        }

        protected class KdNode
        {
            internal T Component;
            internal int Level;
            internal KdNode Left;
            internal KdNode Right;
            internal KdNode Next;
            internal KdNode OldRef;
        }
    }
}
