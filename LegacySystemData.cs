using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Redbox.HAL.Configuration
{
    public sealed class LegacySystemData : IConfigurationFile, IDisposable
    {
        private bool Disposed;

        public SystemConfigurations Type => SystemConfigurations.SystemData;

        public string Path => "c:\\Gamp";

        public string FileName => "SystemData.dat";

        public string FullSourcePath { get; private set; }

        public IDictionary<string, int> SystemData { get; private set; }

        public KioskConfiguration KioskConfig { get; private set; }

        public void ImportFrom(IConfigurationFile config, ErrorList errors)
        {
            throw new NotImplementedException();
        }

        public ConversionResult ConvertTo(KioskConfiguration newConfig, ErrorList errors)
        {
            if (this.SystemData.Keys.Count == 0)
                return ConversionResult.InvalidFile;
            if (this.KioskConfig == KioskConfiguration.R504)
                return ConversionResult.UnsupportedConversion;
            return KioskConfiguration.R630 == this.KioskConfig && (!this.ConvertToVMZ() || !this.Write()) ? ConversionResult.Failure : ConversionResult.Success;
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.Disposed = true;
            this.SystemData.Clear();
        }

        public bool Write()
        {
            if (this.SystemData.Keys.Count == 0)
                return false;
            using (StreamWriter streamWriter = new StreamWriter(this.FullSourcePath))
            {
                foreach (string key in (IEnumerable<string>)this.SystemData.Keys)
                    streamWriter.WriteLine("{0},{1}", (object)key, (object)this.SystemData[key]);
            }
            return true;
        }

        public LegacySystemData()
        {
            this.FullSourcePath = System.IO.Path.Combine(this.Path, this.FileName);
            this.SystemData = (IDictionary<string, int>)new Dictionary<string, int>();
            this.OnReadSystemData((Action<string, int>)((key, value) => this.SystemData[key] = value));
            this.KioskConfig = KioskConfiguration.None;
            if (this.SystemData.Keys.Count <= 0)
                return;
            string key1 = "PlatterMaxSlots1";
            if (!this.SystemData.ContainsKey(key1))
                return;
            if (72 == this.SystemData[key1])
            {
                this.KioskConfig = KioskConfiguration.R504;
            }
            else
            {
                string key2 = "QLMDeckNumber";
                if (!this.SystemData.ContainsKey(key2))
                    return;
                this.KioskConfig = this.SystemData[key2] == 0 ? KioskConfiguration.R717 : KioskConfiguration.R630;
            }
        }

        private bool ConvertToVMZ()
        {
            if (this.SystemData.Keys.Count == 0)
                return false;
            for (int index = 1; index <= 9; ++index)
            {
                string key = string.Format("PlatterMaxSlots{0}", (object)index);
                if (this.SystemData.ContainsKey(key))
                    this.SystemData[key] = 90;
            }
            if (this.SystemData.ContainsKey("QLMDeckNumber"))
                this.SystemData["QLMDeckNumber"] = 0;
            if (this.SystemData.ContainsKey("LastDecknNumber"))
                this.SystemData["LastDecknNumber"] = 9;
            this.KioskConfig = KioskConfiguration.R717;
            return true;
        }

        private void OnReadSystemData(Action<string, int> action)
        {
            if (!File.Exists(this.FullSourcePath))
                return;
            foreach (string readAllLine in File.ReadAllLines(this.FullSourcePath))
            {
                string[] strArray = readAllLine.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int result;
                if (strArray.Length >= 2 && int.TryParse(strArray[1], out result))
                    action(strArray[0], result);
            }
        }
    }
}
