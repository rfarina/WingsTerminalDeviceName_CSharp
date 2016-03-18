<%@ Application Language="C#" %>

<script runat="server">

    private void Application_Start(object sender, System.EventArgs e)
    {
        System.Collections.Hashtable ActiveJobTable = null;

        base.Application.Lock();
        ActiveJobTable = new System.Collections.Hashtable();
        base.Application["ActiveJobs"] = ActiveJobTable;
        base.Application.UnLock();
    }


    private void Session_Start(object sender, System.EventArgs e)
    {
        object Device = null;
        ASNA.Monarch.WebJob Job = null;
        System.Collections.Hashtable ActiveJobTable = null;
        string fileName = "";

        fileName = System.IO.Path.GetFileNameWithoutExtension(base.Request.Path);

        if (fileName.StartsWith("!") || base.Request.Form["__isDspF__"] != null)
            return;

        base.Session["MonarchInitiated"] = null;
        Job = NewJob();
        base.Session["Job"] = Job;
        Device = Job.Start(this.Session.SessionID);
        base.Session["Device"] = Device;


        ActiveJobTable = base.Application["ActiveJobs"] as System.Collections.Hashtable;
        object _lock0 = ActiveJobTable.SyncRoot;
        lock (_lock0)
        {
            ActiveJobTable.Add((this.Session.SessionID + Job.PsdsJobNumber.ToString()), Job);
        }

        ReadAlternatePagesConfig();
    }

    private void ReadAlternatePagesConfig()
    {
        string userAgent = Request.UserAgent;

        if (userAgent == null)
            return;

        XDocument alternatePagesConfigDoc = XDocument.Load(Request.PhysicalApplicationPath + "/App_Data/AlternatePages.config");
        if (alternatePagesConfigDoc == null)
            return;

        foreach (var agentNode in alternatePagesConfigDoc.Root.Elements("agent"))
        {
            string[] words = agentNode.Attribute("contains").Value.Split(' ');

            string subfolder = agentNode.Attribute("subfolder").Value.Trim();
            foreach (var word in words)
            {
                if (!userAgent.Contains(word.Trim()))
                {
                    subfolder = null;
                    break;
                }
            }
            if (subfolder != null)
            {
                Session["Monarch_AlternateSubfolder"] = subfolder;
                return;
            }
        }
    }

    private void Session_End(object sender, System.EventArgs e)
    {
        System.Collections.Hashtable ActiveJobTable = null;
        ASNA.Monarch.WebJob Job = null;

        Job = base.Session["Job"] as ASNA.Monarch.WebJob;

        if (Job != null)
        {
            Job.RequestShutdown(20);

            ActiveJobTable = base.Application["ActiveJobs"] as System.Collections.Hashtable;
            object _lock1 = ActiveJobTable.SyncRoot;
            lock (_lock1)
            {
                ActiveJobTable.Remove((this.Session.SessionID + Job.PsdsJobNumber.ToString()));
            }
        }
    }
    

	ASNA.Monarch.WebJob NewJob ()
    {
        return new WingsLogic.WingsJob();
    }
</script>
