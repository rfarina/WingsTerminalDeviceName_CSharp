﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace ASNA.Monarch.Support
{
    partial class EoJ : ASNA.Monarch.DeviceNative.Page
    {
        protected override void OnLoad( EventArgs e )
        {
            Session.Abandon();
        }
    }
}
