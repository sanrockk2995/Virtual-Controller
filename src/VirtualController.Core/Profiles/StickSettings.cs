using System;

namespace VirtualController.Profiles
{
    public class StickSettings : StickConfig
    {
        public StickSettings()
        {
            InputSource = "Keyboard";
        }
    }

    public class MouseStickSettings : StickConfig
    {
        public MouseStickSettings()
        {
            InputSource = "Mouse";
        }
    }
}
