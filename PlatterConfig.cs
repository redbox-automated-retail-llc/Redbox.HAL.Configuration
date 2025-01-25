using System;

namespace Redbox.HAL.Configuration
{
    public sealed class PlatterConfig
    {
        private const int SellThruSlotWidth = 466;

        public PlatterType Type { get; private set; }

        public int SlotsPerQuadrant { get; private set; }

        public int? SellThruOffset { get; private set; }

        public Decimal SlotWidth { get; private set; }

        public int QuadrantCount { get; private set; }

        public int SlotCount { get; private set; }

        public PlatterData Data { get; private set; }

        public static PlatterConfig Get(PlatterData data) => new PlatterConfig(data);

        public int[] ComputeOffsets(Decimal startOffset)
        {
            if (PlatterType.Qlm == this.Type || this.Type == PlatterType.None)
                throw new InvalidOperationException(string.Format("Can't compute segments on type {0}", (object)this.Type));
            int[] offsets = new int[this.QuadrantCount];
            Decimal num1 = startOffset;
            for (int index = 0; index < this.QuadrantCount; ++index)
            {
                offsets[index] = (int)num1;
                Decimal num2 = PlatterType.Sparse != this.Type ? (Decimal)this.SlotsPerQuadrant * this.SlotWidth + this.SlotWidth : (Decimal)(this.SlotsPerQuadrant - 1) * this.SlotWidth + 0.5M + 466M;
                num1 += num2;
            }
            return offsets;
        }

        private PlatterConfig(PlatterData data)
        {
            this.Data = data;
            switch (data.SegmentOffsets.Length)
            {
                case 4:
                    this.Type = PlatterType.Qlm;
                    break;
                case 6:
                    this.Type = PlatterType.Dense;
                    break;
                case 12:
                    this.Type = PlatterType.Sparse;
                    break;
                default:
                    this.Type = PlatterType.None;
                    break;
            }
            this.Configure();
        }

        private PlatterConfig(PlatterType t)
        {
            this.Type = t;
            this.Configure();
        }

        private void Configure()
        {
            switch (this.Type)
            {
                case PlatterType.Sparse:
                    this.SlotWidth = 173.3M;
                    this.SellThruOffset = new int?(915);
                    this.QuadrantCount = 12;
                    this.SlotsPerQuadrant = 6;
                    this.SlotCount = 72;
                    break;
                case PlatterType.Dense:
                    this.SlotWidth = 166.6667M;
                    this.QuadrantCount = 6;
                    this.SlotsPerQuadrant = 15;
                    this.SlotCount = 90;
                    break;
                case PlatterType.Qlm:
                    this.SlotWidth = 177.7M;
                    break;
            }
        }
    }
}
