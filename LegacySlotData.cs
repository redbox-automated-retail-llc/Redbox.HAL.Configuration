using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Redbox.HAL.Configuration
{
    public sealed class LegacySlotData : IConfigurationFile, IDisposable
    {
        private bool Disposed;

        public SystemConfigurations Type => SystemConfigurations.SlotData;

        public string Path => "c:\\Gamp";

        public string FileName => "SlotData.dat";

        public string FullSourcePath { get; private set; }

        public List<PlatterConfig> SlotData { get; private set; }

        public KioskConfiguration KioskConfig { get; private set; }

        public void ImportFrom(IConfigurationFile config, ErrorList errors)
        {
            throw new NotImplementedException();
        }

        public ConversionResult ConvertTo(KioskConfiguration newConfig, ErrorList errors)
        {
            if (this.SlotData.Count == 0)
                return ConversionResult.InvalidFile;
            if (KioskConfiguration.R504 == this.KioskConfig)
                return ConversionResult.UnsupportedConversion;
            return KioskConfiguration.R630 == this.KioskConfig && (!this.ConvertToVMZ() || !this.WriteSlotData()) ? ConversionResult.Failure : ConversionResult.Success;
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            this.SlotData.Clear();
        }

        public bool WriteSlotData()
        {
            if (this.SlotData.Count == 0)
                return false;
            using (StreamWriter streamWriter = new StreamWriter(this.FullSourcePath))
            {
                foreach (PlatterConfig platterConfig in this.SlotData)
                    streamWriter.WriteLine(platterConfig.Data.ToString());
            }
            return true;
        }

        public LegacySlotData()
          : this(true)
        {
        }

        public LegacySlotData(bool removeZeroes)
        {
            this.FullSourcePath = System.IO.Path.Combine(this.Path, this.FileName);
            this.SlotData = new List<PlatterConfig>();
            if (File.Exists(this.FullSourcePath))
                this.ReadLegacyFile(removeZeroes);
            this.KioskConfig = KioskConfiguration.None;
            if (this.SlotData.Count <= 0)
                return;
            if (this.SlotData[1].Type == PlatterType.Sparse)
                this.KioskConfig = KioskConfiguration.R504;
            else
                this.KioskConfig = this.SlotData[this.SlotData.Count - 1].Type == PlatterType.Qlm ? KioskConfiguration.R630 : KioskConfiguration.R717;
        }

        private bool ConvertToVMZ()
        {
            if (this.SlotData.Count == 0)
                return false;
            this.SlotData[this.SlotData.Count - 1].Data.SegmentOffsets = this.SlotData[this.SlotData.Count - 2].Data.SegmentOffsets;
            this.KioskConfig = KioskConfiguration.R717;
            return true;
        }

        private void ReadLegacyFile(bool removeZeroes)
        {
            int num = 1;
            bool flag = false;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string readAllLine in File.ReadAllLines(this.FullSourcePath))
            {
                List<int> data = new List<int>();
                int result;
                for (int index = 0; index < readAllLine.Length; ++index)
                {
                    if (char.IsWhiteSpace(readAllLine[index]) || readAllLine[index] == ',')
                    {
                        if (stringBuilder.Length > 0)
                        {
                            if (!int.TryParse(stringBuilder.ToString(), out result))
                            {
                                result = 0;
                                LogHelper.Instance.Log(LogEntryType.Error, "The value {0} in slotdata.dat (line {1}) is invalid", (object)stringBuilder.ToString(), (object)num);
                                flag = true;
                            }
                            else
                                data.Add(result);
                            stringBuilder.Remove(0, stringBuilder.Length);
                        }
                    }
                    else
                        stringBuilder.Append(readAllLine[index]);
                }
                if (stringBuilder.Length > 0)
                {
                    if (!int.TryParse(stringBuilder.ToString(), out result))
                    {
                        result = 0;
                        LogHelper.Instance.Log(string.Format("The value {0} in slotdata.dat (line {1}) is invalid", (object)stringBuilder.ToString(), (object)num), LogEntryType.Error);
                        flag = true;
                    }
                    else
                        data.Add(result);
                    stringBuilder.Remove(0, stringBuilder.Length);
                }
                ++num;
                if (data.Count > 0)
                    this.SlotData.Add(PlatterConfig.Get(new PlatterData(data, removeZeroes)));
            }
            if (!flag)
                return;
            this.SlotData.Clear();
        }
    }
}
