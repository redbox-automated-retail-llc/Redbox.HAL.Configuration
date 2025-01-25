using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Redbox.HAL.Configuration
{
    public sealed class GampHelper
    {
        public const string GampPath = "c:\\Gamp";
        internal const int DenseQuadrants = 6;
        internal const int SparseQuadrants = 12;
        internal const int DenseSlotsPerQuadrant = 15;
        internal const int SparseSlotsPerQuadrant = 6;
        internal const int DefaultSellThru = 915;
        internal const Decimal DenseSlotWidth = 166.6667M;
        internal const Decimal QlmSlotWidth = 177.7M;
        internal const string SlotData = "C:\\gamp\\SlotData.dat";
        internal const string SystemData = "C:\\gamp\\SystemData.dat";

        public int GetPlatterSlots()
        {
            List<List<int>> intListList = new GampHelper().ReadLegacySlotDataFile(false);
            if (intListList.Count == 0)
                return -1;
            return intListList[0].Count == 7 || intListList[0][7] == 0 ? 90 : 72;
        }

        public IDictionary<string, int> ReadLegacySystemDataFile()
        {
            Dictionary<string, int> parameters = new Dictionary<string, int>();
            this.OnReadSystemData((Action<string, int>)((key, value) => parameters[key] = value));
            return (IDictionary<string, int>)parameters;
        }

        public GampBackupResult WriteSystemData(IDictionary<string, int> data, bool createBackup)
        {
            IRuntimeService service = ServiceLocator.Instance.GetService<IRuntimeService>();
            GampBackupResult gampBackupResult = new GampBackupResult();
            if (createBackup)
                gampBackupResult.BackupFile = service.CreateBackup("C:\\gamp\\SystemData.dat", BackupAction.Move);
            using (StreamWriter streamWriter = new StreamWriter("C:\\gamp\\SystemData.dat"))
            {
                foreach (string key in (IEnumerable<string>)data.Keys)
                    streamWriter.WriteLine("{0},{1}", (object)key, (object)data[key]);
            }
            gampBackupResult.Success = true;
            return gampBackupResult;
        }

        public GampBackupResult WriteSlotDataFile(List<List<int>> data, bool createBackup)
        {
            GampBackupResult gampBackupResult = new GampBackupResult();
            IRuntimeService service = ServiceLocator.Instance.GetService<IRuntimeService>();
            if (createBackup)
                gampBackupResult.BackupFile = service.CreateBackup("C:\\gamp\\SlotData.dat", BackupAction.Move);
            using (StreamWriter streamWriter = new StreamWriter("C:\\gamp\\SlotData.dat"))
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (List<int> intList in data)
                {
                    foreach (int num in intList)
                    {
                        stringBuilder.Append(num);
                        stringBuilder.Append(",");
                    }
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    streamWriter.WriteLine(stringBuilder.ToString());
                    stringBuilder.Capacity = 512;
                    stringBuilder.Length = 0;
                }
            }
            gampBackupResult.Success = true;
            return gampBackupResult;
        }

        public List<List<int>> ReadLegacySlotDataFile(bool removeZeroes)
        {
            string path = "C:\\gamp\\SlotData.dat";
            List<List<int>> intListList = new List<List<int>>();
            if (!File.Exists(path))
                return intListList;
            int num = 1;
            bool flag = false;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string readAllLine in File.ReadAllLines(path))
            {
                List<int> intList = new List<int>();
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
                                LogHelper.Instance.Log(string.Format("The value {0} in slotdata.dat (line {1}) is invalid", (object)stringBuilder.ToString(), (object)num), LogEntryType.Error);
                                flag = true;
                            }
                            else
                                intList.Add(result);
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
                        intList.Add(result);
                    stringBuilder.Remove(0, stringBuilder.Length);
                }
                ++num;
                if (intList.Count > 0)
                    intListList.Add(intList);
            }
            if (flag)
                intListList.Clear();
            else if (removeZeroes)
            {
                foreach (List<int> intList in intListList)
                    intList.RemoveAll((Predicate<int>)(each => each == 0));
            }
            return intListList;
        }

        public List<int> ComputeDenseQuadrants(Decimal? startOffset)
        {
            List<int> denseQuadrants = new List<int>();
            Decimal? nullable1 = startOffset;
            denseQuadrants.Add((int)nullable1.Value);
            for (int index = 0; index < 5; ++index)
            {
                Decimal? nullable2 = new Decimal?(2666.6672M);
                Decimal? nullable3 = nullable1;
                Decimal? nullable4 = nullable2;
                nullable1 = nullable3.HasValue & nullable4.HasValue ? new Decimal?(nullable3.GetValueOrDefault() + nullable4.GetValueOrDefault()) : new Decimal?();
                denseQuadrants.Add((int)nullable1.Value);
            }
            return denseQuadrants;
        }

        public void ConvertToVMZ()
        {
            IDictionary<string, int> data = this.ReadLegacySystemDataFile();
            for (int index = 1; index <= 9; ++index)
            {
                string key = string.Format("PlatterMaxSlots{0}", (object)index);
                if (data.ContainsKey(key))
                    data[key] = 90;
            }
            if (data.ContainsKey("QLMDeckNumber"))
                data["QLMDeckNumber"] = 0;
            if (data.ContainsKey("LastDecknNumber"))
                data["LastDecknNumber"] = 9;
            this.WriteSystemData(data, true);
        }

        private void OnReadSystemData(Action<string, int> action)
        {
            if (!File.Exists("C:\\gamp\\SystemData.dat"))
                return;
            foreach (string readAllLine in File.ReadAllLines("C:\\gamp\\SystemData.dat"))
            {
                string[] strArray = readAllLine.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int result;
                if (strArray.Length >= 2 && int.TryParse(strArray[1], out result))
                    action(strArray[0], result);
            }
        }
    }
}
