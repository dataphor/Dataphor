/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Web;
using System.Net;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[ListInDesigner(false)]
	public class Host : Node, IHost
	{
		/// <summary> Create a new host object. </summary>
		/// <param name="ASession"> The session which this host is part of. </param>
		public Host(Session ASession) : base()
		{
			FSession = ASession;
		}

		/// <remarks> Calls Close (<see cref="Close"/>) and dereferences ParentHost. </remarks>
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Close();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		// Session

		private Session FSession;
		public virtual Session Session
		{
			get { return FSession; }
		}

		// Pipe

		public virtual Pipe Pipe
		{
			get { return FSession.Pipe; }
		}

		// Document

		private string FDocument = String.Empty;
		public string Document
		{
			get { return FDocument; }
			set 
			{ 
				if (value != FDocument)
				{
					FDocument = ( value == null ? String.Empty : value ); 
					if (OnDocumentChanged != null)
						OnDocumentChanged(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler OnDocumentChanged;

		// NextRequest

		private Request FNextRequest;
		public Request NextRequest
		{
			get { return FNextRequest; }
			set { FNextRequest = value; }
		}

		/// <summary> Ensures that this host does not have a parent. </summary>
		protected void CheckRootNode()
		{
			if (Parent != null)
				throw new ClientException(ClientException.Codes.NotRootNode);
		}

		// Active

		private bool FActive;
		/// <remarks> When true the host node and all it's children are active. </remarks>
		public override bool Active
		{
			get
			{
				if (Parent != null)
					return base.Active;
				else
					return FActive & !Transitional;
			}
		}

		public void Open()
		{
			Open(false);
		}

		public void Open(bool ADeferAfterActivate)
		{
			if (!Active)
			{
				CheckRootNode();
				ActivateAll();
				FActive = true;
				if (!ADeferAfterActivate)
					AfterActivate();
			}
		}

		/// <summary> AfterOpen is to be called only if the host was opened with ADeferAfterActivate = true. </summary>
		public void AfterOpen()
		{
			AfterActivate();
		}
		
		public void Close()
		{
			if (Active)
			{
				CheckRootNode();
				try
				{
					BeforeDeactivate();
				}
				finally
				{
					FActive = false;
					DeactivateAll();
				}
			}
		}

		public INode Load(string ADocument, object AInstance)
		{
			string LDocumentValue = EvaluateDocument(ADocument);
			Children.Clear();
 
			Deserializer LDeserializer;

			LDeserializer = Session.CreateDeserializer();

			INode LNode;
			try
			{
				LNode = (INode)LDeserializer.Deserialize(LDocumentValue, AInstance);
				try
				{
					LNode.Owner = this;
				}
				catch
				{
					LNode.Dispose();
					throw;
				}
			}
			catch (Exception E)
			{
				throw new ClientException(ClientException.Codes.DocumentDeserializationError, E, LDocumentValue);
			}
			if (LDeserializer.Errors.Count > 0)
				HandleDeserializationErrors(LDeserializer.Errors);
			Children.Add(LNode);
			Document = ADocument;
			return LNode;
		}

		private string EvaluateDocument(string ADocument)
		{
			// Optimization: check to see if the document expression is merely a string literal before making a trip to the server
			var LExpression = new DAE.Language.D4.Parser().ParseExpression(ADocument);
			var LStringValue = LExpression as DAE.Language.ValueExpression;
			if (LStringValue != null && LStringValue.Token == DAE.Language.TokenType.String)
				return (string)LStringValue.Value;
			else
				using (DAE.Runtime.Data.Scalar LScalar = Pipe.RequestDocument(ADocument))
				{
					return LScalar.AsString;
				}
		}

		public INode LoadNext(object AInstance)
		{
			Request LRequest = NextRequest;
			NextRequest = null;
			if (LRequest != null)
				return Load(LRequest.Document, AInstance);
			else
				return null;
		}

		/// <summary> Determines a unique name for the specified node. </summary>
		public void GetUniqueName(INode ANode)
		{
            string LBaseName;
			int LCount = 1;

			if (ANode.Name == String.Empty)
				LBaseName = ANode.GetType().Name;
			else
				LBaseName = ANode.Name;

			if (GetNode(LBaseName, ANode) == null)	// will the unaffected name do?
				ANode.Name = LBaseName;
			else
			{
				// Strip any trailing number from the name
				int LNumIndex = LBaseName.Length - 1;
				while ((LNumIndex >= 0) && Char.IsNumber(LBaseName, LNumIndex))
					LNumIndex--;
				if (LNumIndex < (LBaseName.Length - 1))
				{
					LCount = Int32.Parse(LBaseName.Substring(LNumIndex + 1));
					LBaseName = LBaseName.Substring(0, LNumIndex + 1);
				}

				// Iterate through numbers until a unique number is found
				string LName;
				do
				{
					LName = LBaseName + LCount.ToString();
					LCount++;
				} while (GetNode(LName, ANode) != null);
				ANode.Name = LName;
			}
		}

		/// <remarks> Allows any type of child. </remarks>
		public override bool IsValidChild(Type AChildType)
		{
			return true;
		}

		public event DeserializationErrorsHandler OnDeserializationErrors;

		public void HandleDeserializationErrors(ErrorList AErrors)
		{
			if (OnDeserializationErrors != null)
				OnDeserializationErrors(this, AErrors);
		}
	}

	public delegate void DeserializationErrorsHandler(Host AHost, ErrorList AErrors);
}