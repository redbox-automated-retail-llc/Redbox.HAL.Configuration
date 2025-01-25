using Redbox.HAL.Component.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Redbox.HAL.Configuration
{
    public sealed class ConfigurationFileService : IConfigurationFileService
    {
        private readonly IRuntimeService RuntimeService;
        private readonly List<IConfigurationFile> Configs = new List<IConfigurationFile>();

        public IConfigurationFile Get(SystemConfigurations config)
        {
            if (config == SystemConfigurations.None)
                throw new ArgumentException("Unspecified configuration");
            return this.Configs.Find((Predicate<IConfigurationFile>)(each => each.Type == config));
        }

        public void DoForEach(Predicate<IConfigurationFile> a)
        {
            foreach (IConfigurationFile config in this.Configs)
            {
                if (!a(config))
                    break;
            }
        }

        public void BackupTo(IConfigurationFile f, string targetDir, ErrorList errors)
        {
            string dest = Path.Combine(targetDir, f.FileName);
            this.ForceCopy(f.FullSourcePath, dest, errors);
        }

        public void Restore(IConfigurationFile f, string fromDir, ErrorList errors)
        {
            string str = Path.Combine(fromDir, f.FileName);
            if (!File.Exists(str))
                errors.Add(Error.NewError("F002", "File doesn't exist", string.Format("Backup file {0} doesn't exist.", (object)str)));
            else
                this.ForceCopy(str, f.FullSourcePath, errors);
        }

        public bool Backup(IConfigurationFile f, ErrorList errors)
        {
            if (File.Exists(f.FullSourcePath))
            {
                try
                {
                    this.RuntimeService.CreateBackup(f.FullSourcePath, BackupAction.Move);
                }
                catch (Exception ex)
                {
                    errors.Add(Error.NewError("F003", "Backup failure", ex.Message));
                    return false;
                }
            }
            return !File.Exists(f.FullSourcePath);
        }

        public ConfigurationFileService(IRuntimeService rts)
        {
            this.RuntimeService = rts;
            this.Configs.Add((IConfigurationFile)new RedboxConfigurationFile());
            this.Configs.Add((IConfigurationFile)new LegacySlotData());
            this.Configs.Add((IConfigurationFile)new LegacySystemData());
        }

        private void ForceCopy(string src, string dest, ErrorList errors)
        {
            try
            {
                File.Copy(src, dest, true);
            }
            catch (Exception ex)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Format("Failed to copy {0} --> {1}", (object)src, (object)dest));
                stringBuilder.AppendLine(ex.Message);
                errors.Add(Error.NewError("F001", "Copy failure", stringBuilder.ToString()));
            }
        }
    }
}
