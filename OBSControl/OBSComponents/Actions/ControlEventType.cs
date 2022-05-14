using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public enum ControlEventType
    {
        None,
        Delay,
        SceneChange,
        GameStart,
        SongStart,
        SongFinished,
        GameFinished,
        StartRecord,
        StopRecord
    }
}
