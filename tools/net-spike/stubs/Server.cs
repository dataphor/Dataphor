// SPIKE stubs: just enough surface to let the compiler reach real usage sites.
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
