using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoliCon.Modules
{
    public partial class HandyWindow : HandyControl.Controls.Window, IDialogWindow
    {
        public IDialogResult Result { get; set ; }
    }
}
