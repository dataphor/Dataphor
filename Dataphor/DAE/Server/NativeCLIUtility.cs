using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Server
{
	public static class NativeCLIUtility
	{
		public static IsolationLevel SystemDataIsolationLevelToIsolationLevel(System.Data.IsolationLevel AIsolationLevel)
		{
			switch (AIsolationLevel)
			{
				case System.Data.IsolationLevel.ReadUncommitted: return IsolationLevel.Browse;
				case System.Data.IsolationLevel.ReadCommitted: return IsolationLevel.CursorStability;
				case System.Data.IsolationLevel.RepeatableRead: 
				case System.Data.IsolationLevel.Serializable: return IsolationLevel.Isolated;
				case System.Data.IsolationLevel.Chaos:
				case System.Data.IsolationLevel.Snapshot:
				case System.Data.IsolationLevel.Unspecified: 
				default: throw new ArgumentException("Chaos, snapshot, and unspecified isolation levels are not supported.");
			}
		}

		public static SessionInfo NativeSessionInfoToSessionInfo(NativeSessionInfo ANativeSessionInfo)
		{
			SessionInfo LSessionInfo = new SessionInfo();
			LSessionInfo.UserID = ANativeSessionInfo.UserID;
			LSessionInfo.Password = ANativeSessionInfo.Password;
			LSessionInfo.DefaultLibraryName = ANativeSessionInfo.DefaultLibraryName;
			LSessionInfo.HostName = ANativeSessionInfo.HostName;
			LSessionInfo.DefaultUseDTC = ANativeSessionInfo.DefaultUseDTC;
			LSessionInfo.DefaultIsolationLevel = NativeCLIUtility.SystemDataIsolationLevelToIsolationLevel(ANativeSessionInfo.DefaultIsolationLevel);
			LSessionInfo.DefaultUseImplicitTransactions = ANativeSessionInfo.DefaultUseImplicitTransactions;
			LSessionInfo.DefaultMaxStackDepth = ANativeSessionInfo.DefaultMaxStackDepth;
			LSessionInfo.DefaultMaxCallDepth = ANativeSessionInfo.DefaultMaxCallDepth;
			LSessionInfo.UsePlanCache = ANativeSessionInfo.UsePlanCache;
			LSessionInfo.ShouldEmitIL = ANativeSessionInfo.ShouldEmitIL;
			return LSessionInfo;
		}

		public static NativeCLI.ErrorSeverity DataphorSeverityToNativeCLISeverity(ErrorSeverity ASeverity)
		{
			switch (ASeverity)
			{
				case ErrorSeverity.User : return NativeCLI.ErrorSeverity.User;
				case ErrorSeverity.Application : return NativeCLI.ErrorSeverity.Application;
				case ErrorSeverity.System : return NativeCLI.ErrorSeverity.System;
				case ErrorSeverity.Environment : return NativeCLI.ErrorSeverity.Environment;
			}
			
			return NativeCLI.ErrorSeverity.Unspecified;
		}
		
		public static NativeCLIException WrapException(Exception AException)
		{
			DataphorException LDataphorException = AException as DataphorException;
			if (LDataphorException != null)
				return new NativeCLIException(LDataphorException.Message, LDataphorException.Code, DataphorSeverityToNativeCLISeverity(LDataphorException.Severity), LDataphorException.GetDetails(), LDataphorException.GetServerContext(), WrapException(LDataphorException.InnerException));
			
			if (AException != null)
				return new NativeCLIException(AException.Message, WrapException(AException.InnerException));
				
			return null;
		}
		
		public static Modifier NativeModifierToModifier(NativeModifier ANativeModifier)
		{
			switch (ANativeModifier)
			{
				case NativeModifier.In : return Modifier.In;
				case NativeModifier.Out : return Modifier.Out;
				case NativeModifier.Var : return Modifier.Var;
				default : return Modifier.In;
			}
		}
	}
}
