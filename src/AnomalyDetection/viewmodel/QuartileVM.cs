using System;
using System.Collections.Generic;
using System.Linq;

namespace AnomalyDetection.ViewModel
{
    public class QuartileVM
    {
        public static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0)
                throw new InvalidOperationException("Values list is empty.");

            double position = (sortedValues.Count + 1) * percentile / 100.0;
            int lowerIndex = (int)Math.Floor(position) - 1;
            int upperIndex = (int)Math.Ceiling(position) - 1;

            if (lowerIndex < 0)
                return sortedValues[0];
            if (upperIndex >= sortedValues.Count)
                return sortedValues[sortedValues.Count - 1];

            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            double fraction = position - Math.Floor(position);
            return sortedValues[lowerIndex] + fraction * (sortedValues[upperIndex] - sortedValues[lowerIndex]);
        }
    }
}
