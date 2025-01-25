using System;
using System.Collections.Generic;
using System.Text;

namespace Redbox.HAL.Configuration
{
    public sealed class PlatterData
    {
        public int YOffset { get; private set; }

        public int[] SegmentOffsets { get; internal set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.YOffset);
            foreach (int segmentOffset in this.SegmentOffsets)
                stringBuilder.Append(string.Format(",{0}", (object)segmentOffset));
            return stringBuilder.ToString();
        }

        internal PlatterData(List<int> data)
          : this(data, true)
        {
        }

        internal PlatterData(List<int> data, bool removeZeroes)
        {
            this.YOffset = data[0];
            data.RemoveAt(0);
            if (removeZeroes)
                data.RemoveAll((Predicate<int>)(each => each == 0));
            this.SegmentOffsets = data.ToArray();
        }
    }
}
