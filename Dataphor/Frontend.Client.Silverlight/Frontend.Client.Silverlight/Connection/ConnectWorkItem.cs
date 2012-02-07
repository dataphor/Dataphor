using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	using Alphora.Dataphor.DAE.Listener;
	using Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Runtime.Data;

	/// <summary> Manages the connection work-flow. </summary>
	public class ConnectWorkItem  : INotifyPropertyChanged
	{
		public ConnectWorkItem(ContentControl container)
		{
			_container = container;
		}
		
		private ContentControl _container;
		
		private ConnectStatus _status;

		public ConnectStatus Status
		{
			get { return _status; }
			set
			{
				if (_status != value)
				{
					_status = value;
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
				case ConnectStatus.SelectApplication : Status = ConnectStatus.Login; break;
				case ConnectStatus.StartingApplication: break;
				case ConnectStatus.Complete : Status = ConnectStatus.SelectApplication; break;
			}
		}
		
		private Exception _exception;
		
		public Exception Exception
		{
			get { return _exception; }
			set
			{
				if (value != _exception)
				{
					_exception = value;
					NotifyPropertyChanged("Exception");
				}
			}
		}
		
		private string _hostName;
		
		public string HostName
		{
			get { return _hostName; }
			set
			{
				if (_hostName != value)
				{
					_hostName = value;
					NotifyPropertyChanged("HostName");
				}
			}
		}
		
		private string[] _instances;
		
		public string[] Instances
		{
			get { return _instances; }
			set 
			{ 
				if (_instances != value)
				{
					_instances = value; 
					NotifyPropertyChanged("Instances");
				}
			}
		}

		public void BeginLoadInstances()
		{
			Status = ConnectStatus.LoadingInstances;
			Session.Invoke<string[]>
			(
				() => { return ListenerFactory.EnumerateInstances(_hostName); }, // TODO: This will need to be changed to use the listener connectivity override overload
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
		
		private string _instanceName;
		
		public string InstanceName
		{
			get { return _instanceName; }
			set
			{
				if (_instanceName != value)
				{
					_instanceName = value;
					NotifyPropertyChanged("InstanceName");
				}
			}
		}

		private string _userName = "Admin";
		
		public string UserName
		{
			get { return _userName; }
			set
			{
				if (value != _userName)
				{
					_userName = value;
					NotifyPropertyChanged("UserName");
				}
			}
		}

		private string _password;
		
		public string Password
		{
			get { return _password; }
			set
			{
				if (value != _password)
				{
					_password = value;
					NotifyPropertyChanged("Password");
				}
			}
		}
		
		private struct LoginResult
		{
			public List<object[]> Applications;
			public DataSession DataSession;
		}
		
		public void BeginLogin()
		{
    		Status = ConnectStatus.Connecting;

    		Session.Invoke<LoginResult>
    		(
    			() =>
    			{
    				var dataSession = 
    					new DataSession()
    					{
    						Alias = 
    							new ConnectionAlias()
    							{
    								HostName = _hostName,
    								InstanceName = _instanceName
    							}
    					};
    				dataSession.Alias.SessionInfo.UserID = _userName;
    				dataSession.Alias.SessionInfo.Password = _password == null ? "" : _password;
    				dataSession.Alias.SessionInfo.Environment = "SilverlightClient";
    				dataSession.Open();
    				try
    				{
    					var applicationRows = new List<object[]>();
    					using (var applications = dataSession.OpenReadOnlyDataView(".Frontend.Applications"))
    					{
    						foreach (DAE.Runtime.Data.Row row in applications)
    							applicationRows.Add((object[])((NativeRow)row.AsNative).Values.Clone());
    					}
    					return new LoginResult() { Applications = applicationRows, DataSession = dataSession };
    				}
    				catch
    				{
    					dataSession.Dispose();
    					throw;
    				}
    			}, 
    			(Exception AException) => 
    			{
    				Status = ConnectStatus.Login;
    				Exception = AException;
    			},
    			(LoginResult AResult) =>
    			{
    				DataSession = AResult.DataSession;
    				Applications = AResult.Applications;
    				
    				// If there is only 1 application, skip to login
					if (Applications.Count == 1)
					{
						ApplicationID = (string)Applications[0][0];
					    BeginStartApplication();
					}
					else
   						Status = ConnectStatus.SelectApplication;
    			}
    		);
		}

		private DataSession _dataSession;
		
		public DataSession DataSession
		{
			get { return _dataSession; }
			set
			{
				if (_dataSession != value)
				{
					_dataSession = value;
					NotifyPropertyChanged("DataSession");
				}
			}
		}
		
		private List<object[]> _applications;
		
		public List<object[]> Applications
		{
			get { return _applications; }
			set
			{
				if (_applications != value)
				{
					_applications = value;
					NotifyPropertyChanged("Applications");
				}
			}
		}
		
		private string _applicationID;
		
		public string ApplicationID
		{
			get { return _applicationID; }
			set
			{
				if (_applicationID != value)
				{
					_applicationID = value;
					NotifyPropertyChanged("ApplicationID");
				}
			}
		}
		
		public void BeginStartApplication()
		{
			Status = ConnectStatus.StartingApplication;
			Silverlight.Session.Invoke<Silverlight.Session>
			(
				() =>
				{
					var session = new Silverlight.Session(DataSession, true);
					var startingDocument = session.SetApplication(_applicationID);
					session.Start(startingDocument, _container);
					return session;
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
		
		private Silverlight.Session _session;
		
		public Silverlight.Session Session
		{
			get { return _session; }
			set
			{
				if (_session != value)
				{
					_session = value;
					NotifyPropertyChanged("Session");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	
		public void NotifyPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
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
