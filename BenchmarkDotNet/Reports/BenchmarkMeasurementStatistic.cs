﻿using System.Linq;

namespace BenchmarkDotNet.Reports
{
    internal sealed class BenchmarkMeasurementStatistic : IBenchmarkMeasurementStatistic
    {
        public string Name { get; }
        public long Min { get; }
        public long Max { get; }
        public long Median { get; }
        public double StandardDeviation { get; }
        public double Error { get; }

        public BenchmarkMeasurementStatistic(string name, long[] values)
        {
            Name = name;
            Min = values.Min();
            Max = values.Max();
            Median = values.Median();
            StandardDeviation = values.StandardDeviation();
            Error = (Max - Min) * 1.0 / Min;
        }
    }
}