using System;
using System.Collections.Generic;
using System.Text;

namespace SerialReceiver
{
    public interface IWindowModel
    {
        public void OnWindowLoaded();
        public void OnWindowClosing();
    }
}
