﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer
{
    public class SumCalculator : ISumCalculator
    {
        public int Sum(params int[] nums)
        {
            if (nums == null || nums.Length == 0) return 0;

            return nums.Sum();
        }
    }
}
