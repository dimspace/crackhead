using System;
namespace MonoTouchUtils
{
    public delegate void EnteringForeground();
    public delegate void EnteredBackground();

    public interface ApplicationEventEmitter
    {
        event EnteringForeground EnteringForeground;
        event EnteredBackground EnteredBackground;
    }
}

