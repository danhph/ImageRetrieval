using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageRetrieval.Core
{
    /// <summary>
    /// Manages Frequency Distributions for items of type T
    /// </summary>
    /// <typeparam name="T">Type for item</typeparam>
    public class FrequencyDist<T>
    {
        /// <summary>
        /// Construct Frequency Distribution for the given list of items
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        public FrequencyDist(List<T> li)
        {
            CalcFreqDist(li);
        }

        /// <summary>
        /// Construct Frequency Distribution for the given list of items, across all keys in itemValues
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        /// <param name="itemValues">Entire list of itemValues to include in the frequency distribution</param>
        public FrequencyDist(List<T> li, List<T> itemValues)
        {
            CalcFreqDist(li);
            // add items to frequency distribution that are in itemValues but missing from the frequency distribution
            foreach (var v in itemValues)
            {
                if (!ItemFreq.Keys.Contains(v))
                {
                    ItemFreq.Add(v, new Item {value = v, count = 0});
                }
            }

            // check that all values in li are in the itemValues list
            foreach (var v in li)
            {
                if (!itemValues.Contains(v))
                    throw new Exception(string.Format(
                        "FrequencyDist: Value in list for frequency distribution not in supplied list of values: '{0}'.",
                        v));
            }
        }

        /// <summary>
        /// Calculate the frequency distribution for the values in list
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        void CalcFreqDist(List<T> li)
        {
            itemFreq = new SortedList<T, Item>((from item in li
                    group item by item
                    into theGroup
                    select new Item {value = theGroup.FirstOrDefault(), count = theGroup.Count()})
                .ToDictionary(q => q.value, q => q));
        }

        SortedList<T, Item> itemFreq = new SortedList<T, Item>();

        /// <summary>
        /// Getter for the Item Frequency list
        /// </summary>
        public SortedList<T, Item> ItemFreq
        {
            get { return itemFreq; }
        }

        public int Freq(T value)
        {
            if (itemFreq.Keys.Contains(value))
            {
                return itemFreq[value].count;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the list of distinct values between two lists
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <returns></returns>
        public static List<T> GetDistinctValues(List<T> l1, List<T> l2)
        {
            return l1.Concat(l2).ToList().Distinct().ToList();
        }

        /// <summary>
        /// Manages a count of items (int, string etc) for frequency counts
        /// </summary>
        /// <typeparam name="T">The type for item</typeparam>
        public class Item
        {
            /// <summary>
            /// The value of the item, e.g. int or string
            /// </summary>
            public T value { get; set; }

            /// <summary>
            /// The count of the item
            /// </summary>
            public int count { get; set; }
        }
    }

    public class Cosine
    {
        ///
        /// <summary>
        /// Calculates the distance between frequency distributions calculated from lists of items
        /// </summary>
        /// <typeparam name="T">Type of the list item, e.g. int or string</typeparam>
        /// <param name="l1">First list of items</param>
        /// <param name="l2">Second list of items</param>
        /// <returns>Distance in degrees. 90 is totally different, 0 exactly the same</returns>
        public static double Distance<T>(List<T> l1, List<T> l2)
        {
            if (l1.Count() == 0 || l2.Count() == 0)
            {
                throw new Exception("Cosine Distance: lists cannot be zero length");
            }

            // find distinct list of items from two lists, used to align frequency distributions from two lists
            List<T> dvs = FrequencyDist<T>.GetDistinctValues(l1, l2);
            // calculate frequency distributions for each list.
            FrequencyDist<T> fd1 = new FrequencyDist<T>(l1, dvs);
            FrequencyDist<T> fd2 = new FrequencyDist<T>(l2, dvs);

            if (fd1.ItemFreq.Count() != fd2.ItemFreq.Count)
            {
                throw new Exception("Cosine Distance: Frequency count vectors must be same length");
            }

            double dotProduct = 0.0;
            double l2norm1 = 0.0;
            double l2norm2 = 0.0;
            for (int i = 0; i < fd1.ItemFreq.Values.Count(); i++)
            {
                if (!EqualityComparer<T>.Default.Equals(fd1.ItemFreq.Values[i].value, fd2.ItemFreq.Values[i].value))
                    throw new Exception("Mismatched values in frequency distribution for Cosine distance calculation");

                dotProduct += fd1.ItemFreq.Values[i].count * fd2.ItemFreq.Values[i].count;
                l2norm1 += fd1.ItemFreq.Values[i].count * fd1.ItemFreq.Values[i].count;
                l2norm2 += fd2.ItemFreq.Values[i].count * fd2.ItemFreq.Values[i].count;
            }

            double cos = dotProduct / (Math.Sqrt(l2norm1) * Math.Sqrt(l2norm2));
            // convert cosine value to radians then to degrees
            return Math.Acos(cos) * 180.0 / Math.PI;
        }
    }
}