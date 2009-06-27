/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	public class NativeSession
	{
		public NativeSession(NativeSessionInfo ANativeSessionInfo)
		{
			FID = Guid.NewGuid();
			FNativeSessionInfo = ANativeSessionInfo;
		}
		
		private Guid FID;
		public Guid ID { get { return FID; } }
		
		private NativeSessionInfo FNativeSessionInfo;
		public NativeSessionInfo NativeSessionInfo { get { return FNativeSessionInfo; } }
		
		private SessionInfo FSessionInfo;
		public SessionInfo SessionInfo
		{
			get
			{
				if (FSessionInfo == null)
					FSessionInfo = NativeCLIUtility.NativeSessionInfoToSessionInfo(FNativeSessionInfo);
				return FSessionInfo;
			}
		}
		
		private IServerSession FSession;
		public IServerSession Session 
		{ 
			get { return FSession; } 
			set { FSession = value; }
		}
		
		private IServerProcess FProcess;
		public IServerProcess Process 
		{ 
			get { return FProcess; } 
			set { FProcess = value; }
		}
		
		public NativeResult Execute(string AStatement, NativeParam[] AParams)
		{
			IServerScript LScript = FProcess.PrepareScript(AStatement);
			try
			{
				if (LScript.Batches.Count != 1)
					throw new ArgumentException("Execution statement must contain one, and only one, batch.");
					
				IServerBatch LBatch = LScript.Batches[0];
				DataParams LDataParams = NativeMarshal.NativeParamsToDataParams(FProcess, AParams);
				try
				{
					NativeResult LResult = new NativeResult();
					LResult.Params = AParams;

					if (LBatch.IsExpression())
					{
						IServerExpressionPlan LExpressionPlan = LBatch.PrepareExpression(LDataParams);
						try
						{
							if (LExpressionPlan.DataType is Schema.TableType)
							{
								IServerCursor LCursor = LExpressionPlan.Open(LDataParams);
								try
								{
									LResult.Value = NativeMarshal.ServerCursorToNativeValue(FProcess, LCursor);
								}
								finally
								{
									LExpressionPlan.Close(LCursor);
								}
							}
							else
							{
								using (DataValue LValue = LExpressionPlan.Evaluate(LDataParams))
								{
									LResult.Value = NativeMarshal.DataValueToNativeValue(FProcess, LValue);
								}
							}
						}
						finally
						{
							LBatch.UnprepareExpression(LExpressionPlan);
						}
					}
					else
					{
						IServerStatementPlan LStatementPlan = LBatch.PrepareStatement(LDataParams);
						try
						{
							LStatementPlan.Execute(LDataParams);
						}
						finally
						{
							LBatch.UnprepareStatement(LStatementPlan);
						}
					}

					NativeMarshal.SetNativeOutputParams(FProcess, LResult.Params, LDataParams);
					return LResult;
				}
				finally
				{
					for (int LIndex = 0; LIndex < LDataParams.Count; LIndex++)
						if (LDataParams[LIndex].Value != null)
						{
							LDataParams[LIndex].Value.Dispose();
							LDataParams[LIndex].Value = null;
						}
				}
			}
			finally
			{
				FProcess.UnprepareScript(LScript);
			}
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] AOperations)
		{
			NativeResult[] LResults = new NativeResult[AOperations.Length];
			for (int LIndex = 0; LIndex < AOperations.Length; LIndex++)
				LResults[LIndex] = Execute(AOperations[LIndex].Statement, AOperations[LIndex].Params);
				
			return LResults;
		}
	}
}
