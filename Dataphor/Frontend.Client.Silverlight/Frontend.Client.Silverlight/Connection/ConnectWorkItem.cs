using System;
using System.Windows;
using System.ComponentModel;
using Alphora.Dataphor.DAE.Listener;
using System.Windows.Controls;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Manages the connection work-flow. </summary>
	public class ConnectWorkItem  : INotifyPropertyChanged
	{
		public ConnectWorkItem(ContentControl AContainer)
		{
			FContainer = AContainer;
		}
		
		private ContentControl FContainer;
		
		private ConnectStatus FStatus;

		public ConnectStatus Status
		{
			get { return FStatus; }
			set
			{
				if (FStatus != value)
				{
					FStatus = value;
					Exception = null;
					NotifyPropertyChanged("Status");
				}
			}
		}
		
		public void Back()
		{
			switch (Status)
			{
				case ConnectStatus.SelectHost:
				case ConnectStatus.LoadingInstances: break;
				case ConnectStatus.SelectInstance : Status = ConnectStatus.SelectHost; break;
				case ConnectStatus.Login : Status = ConnectStatus.SelectInstance; break;
				case ConnectStatus.Connecting: break;
				case ConnectStatus.SelectApplication : Status = ConnectStatus.SelectInstance; break;
				case ConnectStatus.StartingApplication: break;
				case ConnectStatus.Complete : Status = ConnectStatus.SelectApplication; break;
			}
		}
		
		private Exception FException;
		
		public Exception Exception
		{
			get { return FException; }
			set
			{
				if (value != FException)
				{
					FException = value;
					NotifyPropertyChanged("Exception");
				}
			}
		}
		
		private string FHostName;
		
		public string HostName
		{
			get { return FHostName; }
			set
			{
				if (FHostName != value)
				{
					FHostName = value;
					NotifyPropertyChanged("HostName");
				}
			}
		}
		
		private string[] FInstances;
		
		public string[] Instances
		{
			get { return FInstances; }
			set 
			{ 
				if (FInstances != value)
				{
					FInstances = value; 
					NotifyPropertyChanged("Instances");
				}
			}
		}

		public void BeginLoadInstances()
		{
			Status = ConnectStatus.LoadingInstances;
			Session.Invoke<string[]>
			(
				() => { return ListenerFactory.EnumerateInstances(FHostName); }, // TODO: This will need to be changed to use the listener connectivity override overload
				(Exception AException) => 
				{ 
					Status = ConnectStatus.SelectHost;
					Exception = AException; 
				},
				(string[] AInstances) => 
				{
					Instances = AInstances; 
					Status = ConnectStatus.SelectInstance;
				}
			);
		}
		
		private string FInstanceName;
		
		public string InstanceName
		{
			get { return FInstanceName; }
			set
			{
				if (FInstanceName != value)
				{
					FInstanceName = value;
					NotifyPropertyChanged("InstanceName");
				}
			}
		}

		private string FUserName = "Admin";
		
		public string UserName
		{
			get { return FUserName; }
			set
			{
				if (value != FUserName)
				{
					FUserName = value;
					NotifyPropertyChanged("UserName");
				}
			}
		}

		private string FPassword;
		
		public string Password
		{
			get { return FPassword; }
			set
			{
				if (value != FPassword)
				{
					FPassword = value;
					NotifyPropertyChanged("Password");
				}
			}
		}
		
		public void BeginLogin()
		{
    		Status = ConnectStatus.Connecting;

    		Session.Invoke<DataView>
    		(
    			() =>
    			{
    				var LDataSession = 
    					new DataSession()
    					{
    						Alias = 
    							new ConnectionAlias()
    							{
    								HostName = FHostName,
    								InstanceName = FInstanceName
    							}
    					};
    				LDataSession.Alias.SessionInfo.UserID = FUserName;
    				LDataSession.Alias.SessionInfo.Password = FPassword == null ? "" : FPassword;
    				LDataSession.Alias.SessionInfo.Environment = "SilverlightClient";
    				LDataSession.Open();
    				try
    				{
    					return LDataSession.OpenReadOnlyDataView(".Frontend.Applications");
    				}
    				catch
    				{
    					LDataSession.Dispose();
    					throw;
    				}
    			}, 
    			(Exception AException) => 
    			{
    				Status = ConnectStatus.Login;
    				Exception = AException;
    			},
    			(DataView AApplications) =>
    			{
    				DataSession = AApplications.Session;
    				Applications = AApplications;
    				
    				// If there is only 1 application, skip to login
    				if (!AApplications.IsEmpty() && AApplications.IsLastRow)
    					Status = ConnectStatus.Login;
    				else
   						Status = ConnectStatus.SelectApplication;
    			}
    		);
		}

		private DataSession FDataSession;
		
		public DataSession DataSession
		{
			get { return FDataSession; }
			set
			{
				if (FDataSession != value)
				{
					FDataSession = value;
					NotifyPropertyChanged("DataSession");
				}
			}
		}
		
		private DataView FApplications;
		
		public DataView Applications
		{
			get { return FApplications; }
			set
			{
				if (FApplications != value)
				{
					FApplications = value;
					NotifyPropertyChanged("Applications");
				}
			}
		}
		
		private string FApplicationID;
		
		public string Application
		{
			get { return FApplicationID; }
			set
			{
				if (FApplicationID != value)
				{
					FApplicationID = value;
					NotifyPropertyChanged("ApplicationID");
				}
			}
		}
		
		public void BeginStartApplication()
		{
			Silverlight.Session.Invoke<Silverlight.Session>
			(
				() =>
				{
					var LSession = new Silverlight.Session(DataSession, true);
					var LStartingDocument = LSession.SetApplication(FApplicationID);
					LSession.Start(LStartingDocument, FContainer);
					return LSession;
				},
				(Exception AException) =>
				{
					Status = ConnectStatus.SelectApplication;
					Exception = AException;
				},
				(Silverlight.Session ASession) =>
				{
					Session = ASession;
					Status = ConnectStatus.Complete;
				}
			);
		}
		
		private Silverlight.Session FSession;
		
		public Silverlight.Session Session
		{
			get { return FSession; }
			set
			{
				if (FSession != value)
				{
					FSession = value;
					NotifyPropertyChanged("Session");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	
		public void NotifyPropertyChanged(string AName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(AName));
		}
	}
	
	public enum ConnectStatus 
	{ 
		SelectHost, 
		LoadingInstances, 
		SelectInstance, 
		Login, 
		Connecting, 
		SelectApplication, 
		StartingApplication, 
		Complete 
	};
}
