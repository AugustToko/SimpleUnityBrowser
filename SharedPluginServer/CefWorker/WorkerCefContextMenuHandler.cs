﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace SharedPluginServer
{
    class WorkerCefContextMenuHandler: CefContextMenuHandler
    {
        protected override void OnBeforeContextMenu(CefBrowser browser,
            CefFrame frame,
            CefContextMenuParams state,
            CefMenuModel model)
        {
            //disable for now
            model.Clear();
        }
    }
}
