/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public static class NativeCLIUtility
	{
		public static IsolationLevel NativeIsolationLevelToIsolationLevel(NativeIsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case NativeIsolationLevel.Browse: return IsolationLevel.Browse;
				case NativeIsolationLevel.CursorStability: return IsolationLevel.CursorStability;
				case NativeIsolationLevel.Isolated: return IsolationLevel.Isolated;
				default: throw new ArgumentOutOfRangeException("AIsolationLevel");
			}
		}
		
		public static NativeIsolationLevel IsolationLevelToNativeIsolationLevel(IsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case IsolationLevel.Isolated : return NativeIsolationLevel.Isolated;
				case IsolationLevel.CursorStability : return NativeIsolationLevel.CursorStability;
				case IsolationLevel.Browse : return NativeIsolationLevel.Browse;
				default: throw new ArgumentOutOfRangeException("AIsolationLevel");
			}
		}

		public static SessionInfo NativeSessionInfoToSessionInfo(NativeSessionInfo nativeSessionInfo)
		{
			SessionInfo sessionInfo = new SessionInfo();
			sessionInfo.UserID = nativeSessionInfo.UserID;
			sessionInfo.Password = nativeSessionInfo.Password;
			sessionInfo.DefaultLibraryName = nativeSessionInfo.DefaultLibraryName;
			sessionInfo.HostName = nativeSessionInfo.HostName;
			sessionInfo.Environment = "NativeCLI";
			sessionInfo.DefaultUseDTC = nativeSessionInfo.DefaultUseDTC;
			sessionInfo.DefaultIsolationLevel = NativeIsolationLevelToIsolationLevel(nativeSessionInfo.DefaultIsolationLevel);
			sessionInfo.DefaultUseImplicitTransactions = nativeSessionInfo.DefaultUseImplicitTransactions;
			sessionInfo.DefaultMaxStackDepth = nativeSessionInfo.DefaultMaxStackDepth;
			sessionInfo.DefaultMaxCallDepth = nativeSessionInfo.DefaultMaxCallDepth;
			sessionInfo.UsePlanCache = nativeSessionInfo.UsePlanCache;
			sessionInfo.ShouldEmitIL = nativeSessionInfo.ShouldEmitIL;
			return sessionInfo;
		}

		public static NativeCLI.ErrorSeverity DataphorSeverityToNativeCLISeverity(Dataphor.ErrorSeverity severity)
		{
			switch (severity)
			{
				case Dataphor.ErrorSeverity.User : return NativeCLI.ErrorSeverity.User;
				case Dataphor.ErrorSeverity.Application : return NativeCLI.ErrorSeverity.Application;
				case Dataphor.ErrorSeverity.System : return NativeCLI.ErrorSeverity.System;
				case Dataphor.ErrorSeverity.Environment : return NativeCLI.ErrorSeverity.Environment;
			}
			
			return NativeCLI.ErrorSeverity.Unspecified;
		}
		
		public static NativeCLIException WrapException(Exception exception)
		{
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
				return new NativeCLIException(dataphorException.Message, dataphorException.Code, DataphorSeverityToNativeCLISeverity(dataphorException.Severity), dataphorException.GetDetails(), dataphorException.GetServerContext(), WrapException(dataphorException.InnerException));
			
			if (exception != null)
				return new NativeCLIException(exception.Message, WrapException(exception.InnerException));
				
			return null;
		}
		
		public static Modifier NativeModifierToModifier(NativeModifier nativeModifier)
		{
			switch (nativeModifier)
			{
				case NativeModifier.In : return Modifier.In;
				case NativeModifier.Out : return Modifier.Out;
				case NativeModifier.Var : return Modifier.Var;
				default : return Modifier.In;
			}
		}

		public static NativeModifier ModifierToNativeModifier(Modifier modifier)
		{
			switch (modifier)
			{
				case Modifier.In : return NativeModifier.In;
				case Modifier.Out : return NativeModifier.Out;
				case Modifier.Var : return NativeModifier.Var;
				default : return NativeModifier.In;
			}
		}
	}
}
