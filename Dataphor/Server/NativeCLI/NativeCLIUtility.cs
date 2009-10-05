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
		public static IsolationLevel NativeIsolationLevelToIsolationLevel(NativeIsolationLevel AIsolationLevel)
		{
			switch (AIsolationLevel)
			{
				case NativeIsolationLevel.Browse: return IsolationLevel.Browse;
				case NativeIsolationLevel.CursorStability: return IsolationLevel.CursorStability;
				case NativeIsolationLevel.Isolated: return IsolationLevel.Isolated;
				default: throw new ArgumentOutOfRangeException("AIsolationLevel");
			}
		}
		
		public static NativeIsolationLevel IsolationLevelToNativeIsolationLevel(IsolationLevel AIsolationLevel)
		{
			switch (AIsolationLevel)
			{
				case IsolationLevel.Isolated : return NativeIsolationLevel.Isolated;
				case IsolationLevel.CursorStability : return NativeIsolationLevel.CursorStability;
				case IsolationLevel.Browse : return NativeIsolationLevel.Browse;
				default: throw new ArgumentOutOfRangeException("AIsolationLevel");
			}
		}

		public static SessionInfo NativeSessionInfoToSessionInfo(NativeSessionInfo ANativeSessionInfo)
		{
			SessionInfo LSessionInfo = new SessionInfo();
			LSessionInfo.UserID = ANativeSessionInfo.UserID;
			LSessionInfo.Password = ANativeSessionInfo.Password;
			LSessionInfo.DefaultLibraryName = ANativeSessionInfo.DefaultLibraryName;
			LSessionInfo.HostName = ANativeSessionInfo.HostName;
			LSessionInfo.ClientType = "NativeCLI";
			LSessionInfo.DefaultUseDTC = ANativeSessionInfo.DefaultUseDTC;
			LSessionInfo.DefaultIsolationLevel = NativeIsolationLevelToIsolationLevel(ANativeSessionInfo.DefaultIsolationLevel);
			LSessionInfo.DefaultUseImplicitTransactions = ANativeSessionInfo.DefaultUseImplicitTransactions;
			LSessionInfo.DefaultMaxStackDepth = ANativeSessionInfo.DefaultMaxStackDepth;
			LSessionInfo.DefaultMaxCallDepth = ANativeSessionInfo.DefaultMaxCallDepth;
			LSessionInfo.UsePlanCache = ANativeSessionInfo.UsePlanCache;
			LSessionInfo.ShouldEmitIL = ANativeSessionInfo.ShouldEmitIL;
			return LSessionInfo;
		}

		public static NativeCLI.ErrorSeverity DataphorSeverityToNativeCLISeverity(Dataphor.ErrorSeverity ASeverity)
		{
			switch (ASeverity)
			{
				case Dataphor.ErrorSeverity.User : return NativeCLI.ErrorSeverity.User;
				case Dataphor.ErrorSeverity.Application : return NativeCLI.ErrorSeverity.Application;
				case Dataphor.ErrorSeverity.System : return NativeCLI.ErrorSeverity.System;
				case Dataphor.ErrorSeverity.Environment : return NativeCLI.ErrorSeverity.Environment;
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

		public static NativeModifier ModifierToNativeModifier(Modifier AModifier)
		{
			switch (AModifier)
			{
				case Modifier.In : return NativeModifier.In;
				case Modifier.Out : return NativeModifier.Out;
				case Modifier.Var : return NativeModifier.Var;
				default : return NativeModifier.In;
			}
		}
	}
}
