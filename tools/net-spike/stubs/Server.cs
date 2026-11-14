// SPIKE stubs: just enough surface to let the compiler reach real usage sites.
namespace System.Runtime.Remoting.Messaging { }
namespace System.Runtime.Remoting.Services {
	public interface ITrackingHandler { }
	public static class TrackingServices { public static void RegisterTrackingHandler(ITrackingHandler h) {} public static void UnregisterTrackingHandler(ITrackingHandler h) {} }
}
namespace System.Runtime.Remoting {
	public class ObjRef { } public static class RemotingServices { public static bool Disconnect(MarshalByRefObject o) { return true; } }
}
namespace System.Runtime.Remoting.Lifetime {
	public enum LeaseState { Null, Initial, Active } public static class LifetimeServices { public static TimeSpan LeaseManagerPollTime { get; set; } public static TimeSpan LeaseTime { get; set; } } public interface ILease { LeaseState CurrentState { get; } TimeSpan InitialLeaseTime { get; set; } TimeSpan RenewOnCallTime { get; set; } TimeSpan SponsorshipTimeout { get; set; } void Register(ISponsor s); void Unregister(ISponsor s); }
	public interface ISponsor { TimeSpan Renewal(ILease lease); }
}
namespace System.EnterpriseServices {
	public class ServicedComponent : MarshalByRefObject { protected virtual void Dispose(bool disposing) {} }
	public enum TransactionOption { Required, RequiresNew }
	public class TransactionAttribute : Attribute { public TransactionAttribute(TransactionOption o) {} }
	public static class ContextUtil { public static void SetComplete() {} public static void SetAbort() {} public static void DisableCommit() {} public static void EnableCommit() {} }
}
namespace System.ServiceModel.Web { }
namespace System.ServiceModel {
	public enum InstanceContextMode { Single, PerCall, PerSession }
	public enum ConcurrencyMode { Single, Multiple, Reentrant }
	public class ServiceBehaviorAttribute : Attribute { public InstanceContextMode InstanceContextMode { get; set; } public ConcurrencyMode ConcurrencyMode { get; set; } public bool UseSynchronizationContext { get; set; } public bool IncludeExceptionDetailInFaults { get; set; } }
	public class ServiceHost : IDisposable {
		public ServiceHost(object singleton, params Uri[] baseAddresses) {}
		public ServiceHost(Type t, params Uri[] baseAddresses) {}
		protected virtual void ApplyConfiguration() {} public object SingletonInstance { get { return null; } } public StubDescription Description { get { return null; } } public void AddServiceEndpoint(Type t, System.ServiceModel.Channels.Binding b, string uri) {} public void AddServiceEndpoint(Type t, System.ServiceModel.Channels.Binding b, Uri uri) {} public IAsyncResult BeginClose(AsyncCallback c, object s) { return null; } public void Open() {} public void Close() {} public void Abort() {} public void Dispose() {}
		public System.ServiceModel.CommunicationState State { get { return default(System.ServiceModel.CommunicationState); } }
	}
}

namespace System.ServiceModel { public class StubDescription { public string ConfigurationName { get; set; } public System.Collections.Generic.List<object> Behaviors = new System.Collections.Generic.List<object>(); } }
