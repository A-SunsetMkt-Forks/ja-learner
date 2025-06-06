﻿namespace ja_learner
{
    internal class UserConfig
    {
        public static bool useExtraPrompt = false;
        private static string extraPromptFilename = "";
        public static bool UseProxy = false;
        public static string ExtraPromptFilename
        {
            get
            {
                return extraPromptFilename;
            }
            set
            {
                if (extraPromptFilename != value)
                {
                    extraPromptFilename = value;
                    UpdateExtraPrompt();
                }
            }
        }
        public static string ExtraPrompt { get; private set; } = string.Empty;

        public static void UpdateExtraPrompt()
        {
            var filePath = Path.Combine(Program.APP_SETTING.GPT.ExtraPromptDir, extraPromptFilename);
            try
            {
                ExtraPrompt = File.ReadAllText(filePath);
            }
            catch { }
        }


        public static string[] GetExtraPromptFiles()
        {
            return Directory.CreateDirectory(Program.APP_SETTING.GPT.ExtraPromptDir) // 如果文件夹不存在，创建文件夹
                    .GetFiles()
                    .Select(x => x.Name)
                    .ToArray(); 
        }
    }
}
