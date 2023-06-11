using System.Collections.Generic;
using MistyRobotics.Common.Data;

namespace MysticCommon
{
    public class PackageData
    {
        public PackageData(MysticMode mode, string dataName)
        {
            CurrentMode = mode;
            DataName = dataName;
        }
        public MysticMode CurrentMode { get; private set; }
        public string DataName { get; private set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Context ModeContext { get; set; } = new Context();        
    }
}