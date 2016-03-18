using System;
using System.Text;
using System.Collections.Generic;
using AVRRuntime = ASNA.VisualRPG.Runtime;
using ASNA.DataGate.Client;
using ASNA.DataGate.DataLink;
using ASNA.DataGate.Common;

namespace WingsLogic
{
    public partial class WingsJob : ASNA.Monarch.WebJob
    {
        private AVRRuntime.Database myDatabase;

        override protected AVRRuntime.Database getDatabase()
        {
            return myDatabase;
        }

        override public void Dispose( bool disposing )
        {
            if( disposing )
                myDatabase.Close();
            base.Dispose( disposing );
        }

        #region LogonScreen

        class LogonInfo
        {
            public string Server;
            public string User;
            public string Password;
            public string Program;
            public string Menu;
            public string Library;
            public string Message;
            public int Port;

            public LogonInfo()
            {
                Server = "cypress";
                User = "rickso";
                Password = "";
                Program = "custinq";
                Menu = "";
                Library = "WNGBAS";
                Message = "";
                Port = 5140;
            }
        }

        private void promptLogon( LogonInfo logonInfo )
        {
            ASNA.Monarch.DynamicDisplayFile wfSignon = new ASNA.Monarch.DynamicDisplayFile( "~/Monarch/SignOn.aspx" );
            wfSignon.Open();

            char[] myIndicators = new string('0', 100).ToCharArray();

            logonInfo.Password = "";
            wfSignon.Write( "RSIGNON", myIndicators, populateSignonBuffer, logonInfo );
            wfSignon.Read();
            System.Data.DataRow row = wfSignon.DataSet.Tables[ "RSIGNON" ].Rows[ 0 ];

            logonInfo.Server = row[ "SYSTEM" ].ToString().Trim();
            logonInfo.User = row[ "USER" ].ToString().Trim();
            logonInfo.Password = row[ "PASSWORD" ].ToString().Trim();
            logonInfo.Port = ( int )decimal.Parse( row[ "PORT" ].ToString() );
            logonInfo.Program = row[ "PROGRAM" ].ToString().Trim();
            logonInfo.Menu = row[ "MENU" ].ToString().Trim();
            logonInfo.Library = row[ "LIBRARY" ].ToString().Trim();
            logonInfo.Message = "";

            wfSignon.Close();
        }

        void populateSignonBuffer( string formatName, System.Data.DataRow newRow, object cookie )
        {
            LogonInfo logonInfo = cookie as LogonInfo;

            newRow[ "SYSTEM" ] = logonInfo.Server;
            newRow[ "USER" ] = logonInfo.User;
            newRow[ "PASSWORD" ] = logonInfo.Password;
            newRow[ "PORT" ] = logonInfo.Port;
            newRow[ "PROGRAM" ] = logonInfo.Program;
            newRow[ "MENU" ] = logonInfo.Menu;
            newRow[ "LIBRARY" ] = logonInfo.Library;
            newRow[ "MESSAGE" ] = logonInfo.Message;
        }

        private void logon( bool parmsHaveValues, ref string libraryName, ref string programName, ref string menuName, string message )
        {
            LogonInfo logonInfo = new LogonInfo();
            if( parmsHaveValues )
            {
                logonInfo.Message = message;
                logonInfo.Server = myDatabase.Server;
                logonInfo.User = myDatabase.User;
                logonInfo.Port = myDatabase.Port;
                logonInfo.Library = libraryName;
                logonInfo.Program = programName;
                logonInfo.Menu = menuName;
            }
            myDatabase = new AVRRuntime.Database( "", AVRRuntime.VirtualTerminal.MonarchWeb, AVRRuntime.OpenAccessDspF.Wings );

            while( true )
            {
                promptLogon( logonInfo );

                myDatabase.Server = logonInfo.Server;
                myDatabase.User = logonInfo.User;
                myDatabase.Password = logonInfo.Password;
                myDatabase.Port = logonInfo.Port;

                try
                {
                    myDatabase.Open();
                    break;
                }
                catch( dgException dgEx )
                {
                    logonInfo.Message = dgEx.Message;
                }
            }
            libraryName = logonInfo.Library;
            programName = logonInfo.Program;
            menuName = logonInfo.Menu;
        }

        #endregion

        override protected void ExecuteStartupProgram()
        {
            string libraryName = null;
            string programName = null;
            string menuName = null;
            string message = "";
            bool parmsHaveValues = false;
            while( true )
            {
                logon( parmsHaveValues, ref libraryName, ref programName, ref menuName, message );
                parmsHaveValues = true;

                try
                {
                    if( !string.IsNullOrWhiteSpace( libraryName ) )
                        callQCmdExec( "ChgCurLib " + libraryName );
                }
                catch
                {
                    myDatabase.Close();
                    message = string.Format( "Current library {0} not found", libraryName.ToUpper() );
                    continue;
                }
                try
                {
                    if( !string.IsNullOrWhiteSpace( programName ) )
                        callProgram( programName );
                }
                catch( Exception e )
                {
                    if (!(e is ASNA.Monarch.UnsupportedOperationException))
                        myDatabase.Close();
                    message = e.Message;
                    continue;
                }
                if( string.IsNullOrWhiteSpace( menuName ) )
                    goMenu( "Main" );
                else
                {
                    try
                    {
                        goMenu( menuName );
                    }
                    catch
                    {
                        myDatabase.Close();
                        message = string.Format( "Menu {0} does not exist", menuName.ToUpper() );
                        continue;
                    }
                }
                break;
            }
        }

        void goMenu( string menuName )
        {
            callQCmdExec( "Go " + menuName );
        }

        void callProgram( string programName )
        {
            As400Program program;
            program = new As400Program( myDatabase.Connection, programName );
            program.Execute();
        }

        private void callQCmdExec( string command )
        {
            string programName = "QCmdExc";
            decimal len = ( decimal )command.Length;
            ProgParm Parm1 = new ProgParm( new ProgParmType( "Parm1", 0, FieldType.NewChar( ( int )len ) ), DataDirection.InputOutput );
            ProgParm Parm2 = new ProgParm( new ProgParmType( "Parm2", 0, FieldType.NewPacked( 15, 5 ) ), DataDirection.InputOutput );
            As400Program execPgm = new As400Program( myDatabase.Connection, programName );
            execPgm.AppendParm( Parm1 );
            execPgm.AppendParm( Parm2 );
            execPgm.ObjectToParm( Parm1, command as object );
            execPgm.ObjectToParm( Parm2, len as object );
            try
            {
                execPgm.Execute();
            }
            catch( dgException dgEx )
            {
                if( dgEx.Error != dgErrorNumber.dgEiCONNLOST )
                    throw dgEx;
                try
                {
                    myDatabase.Close();
                }
                catch( dgException )
                {
                }
            }
        }
    }
}
