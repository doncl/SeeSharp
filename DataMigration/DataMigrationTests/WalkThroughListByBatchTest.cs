using System;
using System.Collections.Generic;
using DataMigration;
using NUnit.Framework;

namespace DataMigrationTests
{
    [TestFixture]
    public class WalkThroughListByBatchTest
    {
        /// <summary>
        /// Tests that the WalkThroughListBatchByBatch correctly invokes its Action on each 
        /// member of the list, once and once only.
        /// </summary>
        [Test]
        public void WalkThroughListBatchByBatchTest()
        {
            WalkThroughListByBatchesHelper(20489, 500);
        }

        /// <summary>
        /// Helper function that, given a size of list and batch size, invokes 
        /// WalkThroughListBatchByBatch, and verifies that each item in the list is processed
        /// once, and once only.
        /// </summary>
        /// <param name="sizeOfLists">The size of list to test.</param>
        /// <param name="batchSize">The size of each batch.</param>
        private static void WalkThroughListByBatchesHelper(int sizeOfLists, int batchSize)
        {
            var ids = new List<int>();
            for (var i = 0; i < sizeOfLists; i++) {
                ids.Add(i + 1);
            }

            var dones = new bool[sizeOfLists];

            UpdateStageHelper.WalkThroughListBatchByBatch(1,
                ids,
                batchSize,
                (loadnum, listIds, size) => {
                    foreach (var id in listIds) {
                        Assert.IsFalse(dones[id - 1]);  // make sure it's never been processed.
                        dones[id - 1] = true;          // process it.
                    }
                    return true;
                });

            // Assert that all items have been processed.
            Array.ForEach(dones, Assert.IsTrue);   
        }
    }
}

